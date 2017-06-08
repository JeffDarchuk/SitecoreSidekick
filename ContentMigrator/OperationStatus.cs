using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Rainbow.Diff;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using ScsContentMigrator.Args;
using ScsContentMigrator.CMRainbow;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;

namespace ScsContentMigrator
{
	public class OperationStatus
	{

		private readonly RemoteContentPullArgs _args;
		private bool _doneRemote = false;
		public List<dynamic> Lines => _logger.Lines;
		public List<string> LoggerOutput { get; }
		private CmThreadPool _tp = null;
		private readonly DefaultLogger _logger;
		private readonly ConcurrentQueue<IItemData> _installerQueue = new ConcurrentQueue<IItemData>();
		private readonly ItemComparer _comparer = new DefaultItemComparer();
		readonly SitecoreDataStore _scDatastore;
		private Item _root = null;
		private readonly Database _db;
		readonly HashSet<Guid> _allowedItems = new HashSet<Guid>();
		internal List<ContentTreeNode> RootNodes = new List<ContentTreeNode>();
		internal bool Completed = false;
		private readonly object _finishLocker = new object();
		private int _threadCount = 0;
		internal bool Cancelled = false;
		private Stopwatch _sw = new Stopwatch();
		private readonly HashSet<string> _ids;
		private readonly ConcurrentHashSet<string> _currentTracker = new ConcurrentHashSet<string>();
		private DateTime? _startedTime = null;
		private DateTime? _finishedTime = null;
		public string StartedTime => _startedTime?.ToString("MMM dd h:mm tt") ?? "";
		public string FinishedTime => _finishedTime?.ToString("MMM dd h:mm tt") ?? "";
		private bool _processTemplates = false;

		public bool IsPreview => _args.preview;

		public OperationStatus(RemoteContentPullArgs args, string operationId)
		{
			_sw.Start();

			LoggerOutput = new List<string>();
			_logger = new DefaultLogger();
			var deserializer = new DefaultDeserializer(_logger, new DefaultFieldFilter());
			_startedTime = DateTime.Now;
			_args = args;
			_ids = new HashSet<string>(_args.Ids);
			_scDatastore = new SitecoreDataStore(deserializer);
			OperationId = operationId;
			_db = Factory.GetDatabase(args.Database);
			_tp = new CmThreadPool(this);
			Init();

			foreach (string id in _ids)
			{
				_tp.Queue(GetNextItem, id);
			}

			for (int i = 0; i < (_processTemplates ? 1 : ContentMigrationRegistration.WriterThreads); i++)
			{
				RunDatabaseWriterProcess();
			}
		}

		public void RunPreviewAsFullOperation()
		{
			if (!_args.preview) return;
			_sw = new Stopwatch();
			_sw.Start();
			Lines.Clear();
			_args.preview = false;
			Completed = false;
			_doneRemote = false;
			_root = null;
			_tp = new CmThreadPool(this);
			Init(true);
			foreach (string id in _ids)
			{
				_tp.Queue(GetNextItem, id);
			}

			for (int i = 0; i < (_processTemplates ? 1 : ContentMigrationRegistration.WriterThreads); i++)
			{
				RunDatabaseWriterProcess();
			}
		}

		private void Init(bool fromPreview = false)
		{
			using (new SecurityDisabler())
			{
				foreach (string id in _args.Ids)
				{
					IItemData idata = RemoteContentService.GetRemoteItemData(_args);

					Item parent = _db.GetItem(new ID(idata.ParentId));
					IItemData tmpData = idata;
					_root = _db.GetItem(new ID(idata.Id));
					if (_args.mirror && _root != null)
					{
						Stack<Item> items = new Stack<Item>();
						items.Push(_root);
						while (items.Any())
						{
							var curItem = items.Pop();
							_allowedItems.Add(curItem.ID.Guid);
							foreach (Item child in curItem.Children)
							{
								items.Push(child);
							}
						}
					}

					if (_args.pullParent && parent == null)
					{

						Stack<IItemData> path = new Stack<IItemData>();
						var tmp = _args.Children;
						_args.Children = false;

						while (parent == null)
						{
							parent = _db.GetItem(new ID(tmpData.ParentId));
							path.Push(tmpData);
							tmpData = RemoteContentService.GetRemoteItemData(_args, tmpData.ParentId.ToString());
						}

						while (path.Any())
						{
							var cur = path.Pop();

							InstallItem(cur);
							_currentTracker.Add(cur.Id.ToString());
						}

						_args.Children = tmp;

					}

					if (fromPreview) return;

					var rootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.Id)));

					if (parent.Paths.FullPath.StartsWith("/sitecore/templates"))
					{
						_processTemplates = true;
					}

					if (rootNode.Icon == "")
					{
						rootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.TemplateId)))
						{
							Id = new ID(idata.Id).ToString(),
							DisplayName = idata.Name
						};
					}

					rootNode.Server = _args.Server;
					RootNodes.Add(rootNode);
					
				}
			}
		}
		public void RunDatabaseWriterProcess()
		{
			Task.Run(async () =>
			{
				using (new SecurityDisabler())
				{
					bool islast = await ProcessItemQueue();
					if (Completed) return;
					if (islast)
					{
						if (Completed) return;

						if (_args.mirror && !Cancelled)
						{
							foreach (Guid guid in _allowedItems)
							{
								try
								{
									Item item = _db.GetItem(new ID(guid));
									if (item != null)
									{
										_logger.BeginEvent(item.Name, item.ID.ToString(), item.Paths.FullPath, "Recycle",
											_logger.GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")), item.Database.Name, false);
										if (!_args.preview)
											using (new SecurityDisabler())
												item.Recycle();
									}
								}
								catch (Exception e)
								{
									Log.Error("Problem recycling item", e, this);
								}
							}
						}

						Completed = true;
						dynamic last = new ExpandoObject();
						last.Date = $"{DateTime.Now:F}";
						last.Time = _sw.Elapsed.TotalSeconds;
						last.Items = _logger.Lines.Count;
						last.Cancelled = Cancelled;
						_logger.Lines.Add(last);
						_finishedTime = DateTime.Now;

						if (_args.bulkUpdate || _args.eventDisabler)
						{
							Sitecore.Caching.CacheManager.ClearAllCaches();
						}

						foreach (var processedNode in RootNodes)
						{
							ContentMigrationRegistration.GetChecksum(processedNode.Id, true);
						}
					}
				}
			});
		}

		private async Task<bool> ProcessItemQueue()
		{
			IItemData tmp = null;

			lock (_finishLocker)
			{
				_threadCount++;
			}

			bool ret = false;
			while (!Completed && !Cancelled && (!_doneRemote || _installerQueue.Count > 0))
			{
				try
				{
					if (_installerQueue.TryDequeue(out tmp))
					{
						using (new SecurityDisabler())
						{
							if (_currentTracker.Contains(tmp.ParentId.ToString()) || _db.GetItem(new ID(tmp.ParentId)) != null)
							{
								if (InstallItem(tmp))
								{
									_currentTracker.Add(tmp.Id.ToString());
									tmp = null;
								}
							}
							else
							{
								_installerQueue.Enqueue(tmp);
							}
						}
					}
					else
					{
						await Task.Delay(10);
					}
				}
				catch (Exception e)
				{
					if (tmp != null)
					{
						_logger.BeginEvent(tmp.Name, tmp.Id.ToString(), e.ToString(), "Error", "", tmp.DatabaseName, false);
						_currentTracker.Add(tmp.Id.ToString());
					}
					else
						_logger.BeginEvent("unknown", "", e.ToString(), "Error", "", "", false);
					Log.Error("Problem installing item", e, this);
				}
			}

			lock (_finishLocker)
			{
				_threadCount--;
				if (_threadCount == 0)
				{
					ret = true;
				}
			}

			return ret;
		}

		public int QueuedItems()
		{
			return _installerQueue.Count;
		}

		public void CancelOperation()
		{
			Cancelled = true;
			_doneRemote = true;
		}

		public void EndOperation()
		{
			_doneRemote = true;
		}

		public string OperationId { get; }

		private void GetNextItem(object o)
		{
			if (_doneRemote) return;

			string item = o as string;

			if (item.StartsWith("-"))
			{
				var errItem = item.Substring(1);
				var err = errItem.Split('|');
				_logger.BeginEvent(new ErrorItemData { Id = Guid.Parse(err[0]), Path = err[1] }, "Error", "", false);
			}
			else
			{
				IItemData idata = RemoteContentService.GetRemoteItemData(_args, item);
				_installerQueue.Enqueue(idata);
				if (_args.Children)
				{
					QueueChildren(item);
				}
			}
		}

		private void QueueChildren(object item)
		{
			var list = RemoteContentService.GetRemoteItemChildren(_args, item.ToString()).ToList();
			foreach (string id in list)
			{
				_tp.Queue(GetNextItem, id);
			}
		}
		public bool InstallItem(object o)
		{
			IItemData idata = (IItemData)o;
			if (idata is ErrorItemData)
			{
				_logger.BeginEvent(idata, "Error", "", false);
				return true;
			}
			using (new SecurityDisabler())
			{
				Item exists = _db.GetItem(new ID(idata.Id));

				if (exists == null)
				{
					if (!_args.preview)
					{
						BulkUpdateContext bu = null;
						EventDisabler ed = null;
						try
						{

							if (_args.bulkUpdate)
							{
								bu = new BulkUpdateContext();
							}

							if (_args.eventDisabler)
							{
								ed = new EventDisabler();
							}

							_scDatastore.Save(idata);
						}
						catch (ParentItemNotFoundException)
						{
							_logger.BeginEvent(idata, "Skipped Error on parent", "", false);
							return true;
						}
						finally
						{
							bu?.Dispose();
							ed?.Dispose();
						}
					}
					else
					{
						_logger.BeginEvent(idata, "Created", "", false);
					}

					if (_args.mirror)
					{
						_allowedItems.Remove(idata.Id);
					}
				}
				else if (_args.overwrite)
				{
					if (!_args.preview)
					{
						var results = _comparer.FastCompare(idata, new Rainbow.Storage.Sc.ItemData(exists));
						if (results.AreEqual)
						{
							_logger.BeginEvent(idata, "Skipped", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						}
						else
						{
							_logger.BeginEvent(idata, "Changed", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), true);
							BulkUpdateContext bu = null;
							EventDisabler ed = null;

							try
							{
								if (_args.bulkUpdate)
								{
									bu = new BulkUpdateContext();
								}

								if (_args.eventDisabler)
								{
									ed = new EventDisabler();
								}

								using (new SecurityDisabler())
								{
									_scDatastore.Save(idata);
								}

								_logger.CompleteEvent(idata.Id.ToString());
							}
							finally
							{
								bu?.Dispose();
								ed?.Dispose();
							}
						}
					}
					else
					{
						var results = _comparer.Compare(idata, new Rainbow.Storage.Sc.ItemData(exists));
						if (results.AreEqual)
							_logger.BeginEvent(idata, "Skipped", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						//else if (results.IsBranchChanged)
						//	logger.RecordEvent(idata, "Branch Change", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
						else if (results.IsMoved)
							_logger.BeginEvent(idata, "Moved", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						else if (results.IsRenamed)
							_logger.BeginEvent(idata, "Renamed", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						else if (results.IsTemplateChanged)
							_logger.BeginEvent(idata, "Template Change", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")),
								false);
						else
							_logger.BeginEvent(idata, "Changed", _logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
					}
					if (_args.mirror)
						_allowedItems.Remove(idata.Id);
				}
			}
			return true;
		}
	}
}
