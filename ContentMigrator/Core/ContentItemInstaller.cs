using Rainbow.Diff;
using Rainbow.Model;
using Rainbow.Storage;
using Rainbow.Storage.Sc.Deserialization;
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScsContentMigrator.CMRainbow.Interface;
using SitecoreSidekick.Services.Interface;

namespace ScsContentMigrator.Core
{
	public class ContentItemInstaller : IContentItemInstaller
	{
		private readonly IDefaultLogger _logger;
		private readonly IDataStore _scDatastore;
		private readonly IItemComparer _comparer;
		private readonly ISitecoreDataAccessService _sitecore;
		private readonly IJsonSerializationService _jsonSerializationService;
		internal ConcurrentHashSet<Guid> AllowedItems = new ConcurrentHashSet<Guid>();
		internal ConcurrentHashSet<Guid> Errors = new ConcurrentHashSet<Guid>();
		internal ConcurrentHashSet<Guid> CurrentlyProcessing = new ConcurrentHashSet<Guid>();
		internal int WaitForParentDelay = 50;
		private readonly object _locker = new object();
		public ContentMigrationOperationStatus Status { get; } = new ContentMigrationOperationStatus();


		public ContentItemInstaller()
		{
			_logger = Bootstrap.Container.Resolve<IDefaultLogger>();
			_scDatastore = Bootstrap.Container.Resolve<IDataStore>(_logger);
			_sitecore = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
			_jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();
			_comparer = Bootstrap.Container.Resolve<IItemComparer>();
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
			foreach (Guid id in AllowedItems)
			{
				try
				{
					_sitecore.RecycleItem(id);
					var data = _sitecore.GetItemData(id);
					_logger.BeginEvent(data, LogStatus.Recycle, _sitecore.GetIconSrc(data), false);
				}
				catch (Exception e)
				{
					_logger.BeginEvent(new ErrorItemData() { Name = id.ToString("B"), Path = e.ToString() }, LogStatus.Error, "", false);
				}
			}

		}

		public void SetupTrackerForUnwantedLocalItems(IEnumerable<Guid> rootIds)
		{
			AllowedItems = new ConcurrentHashSet<Guid>(_sitecore.GetSubtreeOfGuids(rootIds));
		}

		public bool Completed { get; private set; }

		private int ItemsInstalled { get; set; } = 0;

		public void StartInstallingItems(PullItemModel args, BlockingCollection<IItemData> itemsToInstall, int threads, CancellationToken cancellationToken)
		{
			Status.StartedTime = DateTime.Now;
			Status.RootNodes = args.Ids.Select(x => new ContentTreeNode(x));
			Status.IsPreview = args.Preview;
			Status.Server = args.Server;			
			for (int i = 0; i < threads; i++)
			{
				Task.Run(async () =>
				{
					await ItemInstaller(args, itemsToInstall, cancellationToken).ConfigureAwait(false);
				});
			}
		}

		private async Task ItemInstaller(PullItemModel args, BlockingCollection<IItemData> itemsToInstall, CancellationToken cancellationToken)
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
						if (!itemsToInstall.TryTake(out remoteData, int.MaxValue, cancellationToken))
						{
							lock (_locker)
							{
								if (!Completed && !CurrentlyProcessing.Any())
								{
									Finalize(ItemsInstalled, args);
								}
							}
							break;
						}
						CurrentlyProcessing.Add(remoteData.Id);						
						IItemData localData = _sitecore.GetItemData(remoteData.Id);
						await ProcessItem(args, localData, remoteData).ConfigureAwait(false);
						lock (_locker)
						{
							ItemsInstalled++;
							CurrentlyProcessing.Remove(remoteData.Id);
							if (CurrentlyProcessing.Any() || !itemsToInstall.IsAddingCompleted || itemsToInstall.Count != 0)
							{
								continue;
							}

							if (!Completed)
							{
								Finalize(ItemsInstalled, args);
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
						Finalize(ItemsInstalled, args);
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
			_logger.LoggerOutput.Add(_jsonSerializationService.SerializeObject(_logger.Lines.Last()));
			if (args.RemoveLocalNotInRemote)
				CleanUnwantedLocalItems();
		}

		internal async Task ProcessItem(PullItemModel args, IItemData localData, IItemData remoteData)
		{
			AllowedItems.Remove(remoteData.Id);
			if (args.Preview)
			{
				if (localData != null)
				{
					var results = _comparer.Compare(remoteData, localData);
					if (results.AreEqual)
					{
						_logger.BeginEvent(remoteData, LogStatus.Skipped, GetSrc(_sitecore.GetIconSrc(localData)), false);
					}
					else if (results.IsMoved)
						_logger.BeginEvent(remoteData, LogStatus.Moved, GetSrc(_sitecore.GetIconSrc(localData)), false);
					else if (results.IsRenamed)
						_logger.BeginEvent(remoteData, LogStatus.Renamed, GetSrc(_sitecore.GetIconSrc(localData)), false);
					else if (results.IsTemplateChanged)
						_logger.BeginEvent(remoteData, LogStatus.TemplateChange, GetSrc(_sitecore.GetIconSrc(localData)), false);
					else if (args.Overwrite)
						_logger.BeginEvent(remoteData, LogStatus.Changed, GetSrc(_sitecore.GetIconSrc(localData)), false);
					else
						_logger.BeginEvent(remoteData, LogStatus.Skipped, GetSrc(_sitecore.GetIconSrc(localData)), false);
				}
				else
					_logger.BeginEvent(remoteData, LogStatus.Created, "", false);
			}
			else
			{
				bool skip = false;
				if (!args.Overwrite && localData != null)
				{
					_logger.BeginEvent(remoteData, LogStatus.Skipped, GetSrc(_sitecore.GetIconSrc(localData)), false);
					skip = true;
				}
				if (!skip && localData != null)
				{
					var results = _comparer.Compare(remoteData, localData);
					if (results.AreEqual)
					{
						_logger.BeginEvent(remoteData, LogStatus.Skipped, GetSrc(_sitecore.GetIconSrc(localData)), false);
						skip = true;
					}
				}
				else if (!skip)
				{
					while (CurrentlyProcessing.Contains(remoteData.ParentId))
					{
						if (Errors.Contains(remoteData.ParentId))
						{
							Errors.Add(remoteData.Id);
							skip = true;
							break;
						}
						
						await Task.Delay(WaitForParentDelay).ConfigureAwait(false);
					}
				}
				if (!skip)
				{
					try
					{
						if (localData != null)
						{							
							_logger.BeginEvent(remoteData, LogStatus.Changed, _logger.GetSrc(GetSrc(_sitecore.GetIconSrc(localData))), true);
						}
						_scDatastore.Save(remoteData);
					}
					catch (TemplateMissingFieldException tm)
					{
						_logger.BeginEvent(new ErrorItemData() { Name = remoteData.Name, Path = tm.ToString() }, LogStatus.Warning, "", false);
					}
					catch (ParentItemNotFoundException)
					{
						_logger.BeginEvent(remoteData, LogStatus.SkippedParentError, "", false);
						Errors.Add(remoteData.Id);
					}
					catch (Exception e)
					{
						Errors.Add(remoteData.Id);
						_logger.BeginEvent(new ErrorItemData() { Name = remoteData?.Name ?? "Unknown item", Path = e.ToString() }, LogStatus.Error, "", false);
					}
					if (localData != null)
					{
						if (!_logger.HasLinesSupportEvents(localData.Id.ToString()))
						{
							_logger.CompleteEvent(localData.Id.ToString());
						}
						else
						{
							_logger.BeginEvent(localData, LogStatus.Skipped, _logger.GetSrc(GetSrc(_sitecore.GetIconSrc(localData))), false);
						}
					}
				}
			}
		}

		private string GetSrc(string imgTag)
		{
			if (string.IsNullOrWhiteSpace(imgTag)) return imgTag;
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
	}
}
