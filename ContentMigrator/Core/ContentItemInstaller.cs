using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Rainbow.Diff;
using Rainbow.Model;
using Rainbow.Storage;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using ScsContentMigrator.Args;
using ScsContentMigrator.CMRainbow;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services.Interface;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Shared.IoC;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace ScsContentMigrator.Core
{
	class ContentItemInstaller : IContentItemInstaller
	{
		private readonly DefaultLogger _logger = new DefaultLogger();
		private readonly IDataStore _scDatastore;
		private readonly ItemComparer _comparer = new DefaultItemComparer();
		private readonly ISitecoreAccessService _sitecore;
		private ConcurrentHashSet<Guid> _allowedItems = new ConcurrentHashSet<Guid>();
		private ConcurrentHashSet<Guid> _errors = new ConcurrentHashSet<Guid>();
		private ConcurrentHashSet<Guid> _currentlyProcessing = new ConcurrentHashSet<Guid>();
		private readonly object _locker = new object();
		public ContentMigrationOperationStatus Status { get; } = new ContentMigrationOperationStatus();


		public ContentItemInstaller()
		{
			var deserializer = new DefaultDeserializer(_logger, new DefaultFieldFilter());
			_scDatastore = new SitecoreDataStore(deserializer);			
			_sitecore = Bootstrap.Container.Resolve<ISitecoreAccessService>();
		}
		public ContentItemInstaller(ISitecoreAccessService sitecore, IDataStore dataStore)
		{
			_scDatastore = dataStore;
			_sitecore = sitecore;
		}

		public IEnumerable<dynamic> GetItemLogEntries(int lineToStartFrom)
		{
			for (int i = lineToStartFrom; i < _logger.Lines.Count; i++)
				yield return _logger.Lines[i];
		}

		public IEnumerable<string> GetAuditLogEntries(int lineToStartFrom)
		{
			for (int i = lineToStartFrom; i < _logger.LoggerOutput.Count; i++)
				yield return _logger.LoggerOutput[i];
		}

		public void CleanUnwantedLocalItems()
		{
			foreach (Guid id in _allowedItems)
			{
				try
				{
					_sitecore.RecycleItem(id);
					var data = _sitecore.GetItemData(id);
					_logger.BeginEvent(data, "Recycle", _sitecore.GetItemIconSrc(data), false);
				}
				catch (Exception e)
				{
					_logger.BeginEvent(new ErrorItemData() { Name = id.ToString("B"), Path = e.ToString() }, "Error", "", false);
				}
			}

		}

		public void SetupTrackerForUnwantedLocalItems(IEnumerable<Guid> rootIds)
		{
			_allowedItems = _sitecore.GetSubtreeOfGuids(rootIds);
		}

		public bool Completed { get; private set; }

		public void StartInstallingItems(PullItemModel args, BlockingCollection<IItemData> itemsToInstall, int threads, CancellationTokenSource cancellation)
		{
			Status.StartedTime = DateTime.Now;
			Status.RootNodes = args.Ids.Select(x => new ContentTreeNode(x));
			Status.IsPreview = args.Preview;
			Status.Server = args.Server;
			int items = 0;
			for (int i = 0; i < threads; i++)
			{
				Task.Run(async () =>
				{
					Thread.CurrentThread.Priority = ThreadPriority.Lowest;
					BulkUpdateContext bu = null;
					EventDisabler ed = null;
					try
					{

						if (args.BulkUpdate)
						{
							bu = new BulkUpdateContext();
						}
						if (args.EventDisabler)
						{
							ed = new EventDisabler();
						}
						using (new SecurityDisabler())
						{
							while (!Completed)
							{
								IItemData remoteData;
								if (!itemsToInstall.TryTake(out remoteData, int.MaxValue, cancellation.Token))
								{
									lock (_locker)
									{
										if (!Completed && !_currentlyProcessing.Any())
										{
											Finalize(items, args);
										}
									}
									break;
								}
								_currentlyProcessing.Add(remoteData.Id);
								Item localItem = _sitecore.GetItem(remoteData.Id);
								IItemData localData = localItem == null ? null : new Rainbow.Storage.Sc.ItemData(localItem);
								await ProcessItem(args, localData, remoteData, localItem);
								lock (_locker)
								{
									items++;
									_currentlyProcessing.Remove(remoteData.Id);
									if (_currentlyProcessing.Any() || !itemsToInstall.IsAddingCompleted || itemsToInstall.Count != 0)
									{
										continue;
									}

									if (!Completed)
									{
										Finalize(items, args);
									}
								}
							}
						}
					}
					catch (OperationCanceledException e)
					{
						Log.Warn("Content migration operation was cancelled", e, this);
						Status.Cancelled = true;
						lock (_locker)
						{
							if (!Completed)
							{
								Finalize(items, args);
							}
						}
					}
					catch (Exception e)
					{
						Log.Error("Catastrophic error when installing items", e, this);
					}
					finally
					{
						if (args.BulkUpdate)
						{
							bu?.Dispose();
						}
						if (args.EventDisabler)
						{
							ed?.Dispose();
						}
					}
				});
			}
		}

		private void Finalize(int items, PullItemModel args)
		{
			Completed = true;
			Status.FinishedTime = DateTime.Now;
			Status.Completed = true;
			_logger.Lines.Add(new
			{
				Items = items,
				Time = Status.FinishedTime.Subtract(Status.StartedTime).TotalSeconds,
				Date = Status.FinishedTime.ToString("F"),
				Status.Cancelled
			});
			_logger.LoggerOutput.Add(JsonNetWrapper.SerializeObject(_logger.Lines.Last()));
			if (args.RemoveLocalNotInRemote)
				CleanUnwantedLocalItems();
		}

		private async Task ProcessItem(PullItemModel args, IItemData localData, IItemData remoteData, Item localItem)
		{
			_allowedItems.Remove(remoteData.Id);
			if (args.Preview)
			{
				if (localData != null)
				{
					var results = _comparer.Compare(remoteData, localData);
					if (results.AreEqual)
					{
						_logger.BeginEvent(remoteData, "Skipped", _sitecore.GetItemIconSrc(localData), false);
					}
					else if (results.IsMoved)
						_logger.BeginEvent(remoteData, "Moved", _sitecore.GetItemIconSrc(localData), false);
					else if (results.IsRenamed)
						_logger.BeginEvent(remoteData, "Renamed", _sitecore.GetItemIconSrc(localData), false);
					else if (results.IsTemplateChanged)
						_logger.BeginEvent(remoteData, "Template Change", _sitecore.GetItemIconSrc(localData), false);
					else if (args.Overwrite)
						_logger.BeginEvent(remoteData, "Changed", _sitecore.GetItemIconSrc(localData), false);
					else
						_logger.BeginEvent(remoteData, "Skipped", _sitecore.GetItemIconSrc(localData), false);
				}
				else
					_logger.BeginEvent(remoteData, "Created", "", false);
			}
			else
			{
				bool skip = false;
				if (!args.Overwrite && localData != null)
				{
					_logger.BeginEvent(remoteData, "Skipped", _sitecore.GetItemIconSrc(localData), false);
					skip = true;
				}
				if (!skip && localData != null)
				{
					var results = _comparer.Compare(remoteData, localData);
					if (results.AreEqual)
					{
						_logger.BeginEvent(remoteData, "Skipped", _sitecore.GetItemIconSrc(localData), false);
						skip = true;
					}
				}
				else if (!skip)
				{
					while (_currentlyProcessing.Contains(remoteData.ParentId))
					{
						if (_errors.Contains(remoteData.ParentId))
						{
							_errors.Add(remoteData.Id);
							skip = true;
							break;
						}
						await Task.Delay(50);
					}
				}
				if (!skip)
				{
					try
					{
						
						if (localData != null)
						{
							_logger.BeginEvent(remoteData, "Changed", _logger.GetSrc(ThemeManager.GetIconImage(localItem, 32, 32, "", "")), true);
						}
						_scDatastore.Save(remoteData);
					}
					catch (TemplateMissingFieldException tm)
					{
						_logger.BeginEvent(new ErrorItemData() { Name = remoteData.Name, Path = tm.ToString() }, "Warning", "", false);
					}
					catch (ParentItemNotFoundException)
					{
						_logger.BeginEvent(remoteData, "Skipped parent error", "", false);
						_errors.Add(remoteData.Id);
					}
					catch (Exception e)
					{
						_errors.Add(remoteData.Id);
						_logger.BeginEvent(new ErrorItemData() { Name = remoteData?.Name ?? "Unknown item", Path = e.ToString() }, "Error", "", false);
					}
					if (localData != null)
					{
						if (_logger.LinesSupport[localData.Id.ToString()].Events.Count != 0)
						{
							_logger.CompleteEvent(localData.Id.ToString());
						}
						else
						{
							_logger.BeginEvent(localData, "Skipped", _logger.GetSrc(ThemeManager.GetIconImage(localItem, 32, 32, "", "")), false);
						}
					}
				}
			}
		}
	}
}
