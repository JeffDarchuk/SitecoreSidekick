using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rainbow.Diff;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using Rainbow.Storage.Yaml;
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

		private RemoteContentPullArgs _args;
		private bool doneRemote = false;
		public List<dynamic> Lines => logger.Lines;
		public List<string> LoggerOutput { get; private set; }
		private CMThreadPool tp = null;
		private DefaultLogger logger;
		private DefaultDeserializer deserializer;
		private ConcurrentQueue<IItemData> _installerQueue = new ConcurrentQueue<IItemData>();
		private ItemComparer _comparer = new DefaultItemComparer();
		SitecoreDataStore scDatastore;
		private Item root = null;
		private Database db;
		HashSet<Guid> allowedItems = new HashSet<Guid>();
		internal List<ContentTreeNode> RootNodes = new List<ContentTreeNode>();
		internal bool Completed = false;
		private object finishLocker = new object();
		private int threadCount = 0;
		internal bool Cancelled = false;
		private Stopwatch sw = new Stopwatch();
		private HashSet<string> _ids;
		private ConcurrentHashSet<string> _currentTracker = new ConcurrentHashSet<string>();
		private DateTime? _startedTime = null;
		private DateTime? _finishedTime = null;
		public string StartedTime => _startedTime?.ToString("MMM dd h:mm tt") ?? "";
		public string FinishedTime => _finishedTime?.ToString("MMM dd h:mm tt") ?? "";
		private bool processTemplates = false;

		public bool IsPreview => _args.preview;

		public OperationStatus(RemoteContentPullArgs args, string operationId)
		{
			sw.Start();
			LoggerOutput = new List<string>();
			logger = new DefaultLogger();
			deserializer = new DefaultDeserializer(logger, new DefaultFieldFilter());
			_startedTime = DateTime.Now;
			_args = args;
			_ids = new HashSet<string>(_args.ids);
			scDatastore = new SitecoreDataStore(deserializer);
			this.OperationId = operationId;
			db = Factory.GetDatabase(args.database);
			tp = new CMThreadPool(this);
			Init();
			foreach (string id in _ids)
				tp.Queue(GetNextItem, id);
			for (int i = 0; i < (processTemplates ? 1 : ContentMigrationHandler.writerThreads); i++)
				RunDatabaseWriterProcess();
		}

		public void RunPreviewAsFullOperation()
		{
			if (!_args.preview) return;
			sw = new Stopwatch();
			sw.Start();
			Lines.Clear();
			_args.preview = false;
			Completed = false;
			doneRemote = false;
			root = null;
			tp = new CMThreadPool(this);
			Init(true);
			foreach (string id in _ids)
				tp.Queue(GetNextItem, id);
			for (int i = 0; i < (processTemplates ? 1 : ContentMigrationHandler.writerThreads); i++)
				RunDatabaseWriterProcess();
		}

		private void Init(bool fromPreview = false)
		{
			using (new SecurityDisabler())
			{
				foreach (string id in _args.ids)
				{
					IItemData idata = GetResources.GetRemoteItemData(_args, id);

					Item parent = db.GetItem(new ID(idata.ParentId));
					IItemData tmpData = idata;
					root = db.GetItem(new ID(idata.Id));
					if (_args.mirror && root != null)
					{
						Stack<Item> items = new Stack<Item>();
						items.Push(root);
						while (items.Any())
						{
							var curItem = items.Pop();
							allowedItems.Add(curItem.ID.Guid);
							foreach (Item child in curItem.Children)
								items.Push(child);
						}
					}

					if (_args.pullParent && parent == null)
					{

						Stack<IItemData> path = new Stack<IItemData>();
						var tmp = _args.children;
						_args.children = false;
						while (parent == null)
						{
							parent = db.GetItem(new ID(tmpData.ParentId));
							path.Push(tmpData);
							tmpData = GetResources.GetRemoteItemData(_args, tmpData.ParentId.ToString());
						}
						while (path.Any())
						{
							var cur = path.Pop();

							InstallItem(cur);
							_currentTracker.Add(cur.Id.ToString());
						}
						_args.children = tmp;

					}
					if (fromPreview) return;
					var RootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.Id)));
					if (parent.Paths.FullPath.StartsWith("/sitecore/templates"))
						processTemplates = true;
					if (RootNode.Icon == "")
					{
						RootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.TemplateId)))
						{
							Id = new ID(idata.Id).ToString(),
							DisplayName = idata.Name
						};
					}
					RootNode.Server = _args.server;
					RootNodes.Add(RootNode);
					
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
							foreach (Guid guid in allowedItems)
							{
								try
								{
									Item item = db.GetItem(new ID(guid));
									if (item != null)
									{
										logger.BeginEvent(item.Name, item.ID.ToString(), item.Paths.FullPath, "Recycle",
											logger.GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")), item.Database.Name, false);
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
						Completed = true;
						dynamic last = new ExpandoObject();
						last.Date = $"{DateTime.Now:F}";
						last.Time = sw.Elapsed.TotalSeconds;
						last.Items = logger.Lines.Count;
						last.Cancelled = Cancelled;
						logger.Lines.Add(last);
						_finishedTime = DateTime.Now;
						if (_args.bulkUpdate || _args.eventDisabler)
							Sitecore.Caching.CacheManager.ClearAllCaches();
						foreach (var processedNode in RootNodes)
							ContentMigrationHandler.GetChecksum(processedNode.Id, true);
					}
				}
			});
		}

		async private Task<bool> ProcessItemQueue()
		{
			IItemData tmp = null;
			lock (finishLocker)
			{
				threadCount++;
			}
			bool ret = false;
			while (!Completed && !Cancelled && (!doneRemote || _installerQueue.Count > 0))
			{
				try
				{
					if (_installerQueue.TryDequeue(out tmp))
					{
						using (new SecurityDisabler())
						{
							if (_currentTracker.Contains(tmp.ParentId.ToString()) || db.GetItem(new ID(tmp.ParentId)) != null)
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
								continue;
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
						logger.BeginEvent(tmp.Name, tmp.Id.ToString(), e.ToString(), "Error", "", tmp.DatabaseName, false);
						_currentTracker.Add(tmp.Id.ToString());
					}
					else
						logger.BeginEvent("unknown", "", e.ToString(), "Error", "", "", false);
					Log.Error("Problem installing item", e, this);
				}
			}
			lock (finishLocker)
			{
				threadCount--;
				if (threadCount == 0)
					ret = true;
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
			doneRemote = true;
		}
		public void EndOperation()
		{
			doneRemote = true;
		}
		public string OperationId { get; }

		private void GetNextItem(object o)
		{
			if (doneRemote)
				return;
			string item = o as string;
			if (item.StartsWith("-"))
			{
				var errItem = item.Substring(1);
				var err = errItem.Split('|');
				logger.BeginEvent(new ErrorItemData { Id = Guid.Parse(err[0]), Path = err[1] }, "Error", "", false);
			}
			else
			{
				TimeSpan tmp = sw.Elapsed;
				IItemData idata = GetResources.GetRemoteItemData(_args, item);
				_installerQueue.Enqueue(idata);
				if (_args.children)
				{
					QueueChildren(item);
				}
			}
		}

		private void QueueChildren(object item)
		{
			var list = GetResources.GetRemoteItemChildren(_args, item.ToString()).ToList();
			foreach (string id in list)
			{
				tp.Queue(GetNextItem, id);
			}
		}
		public bool InstallItem(object o)
		{
			IItemData idata = (IItemData)o;
			if (idata is ErrorItemData)
			{
				logger.BeginEvent(idata, "Error", "", false);
				return true;
			}
			using (new SecurityDisabler())
			{
				Item exists = db.GetItem(new ID(idata.Id));

				if (exists == null)
				{
					if (!_args.preview)
					{
						try
						{
							BulkUpdateContext bu = null;
							if (_args.bulkUpdate)
								bu = new BulkUpdateContext();
							EventDisabler ed = null;
							if (_args.eventDisabler)
								ed = new EventDisabler();


							scDatastore.Save(idata);

							bu?.Dispose();
							ed?.Dispose();

						}
						catch (ParentItemNotFoundException)
						{
							logger.BeginEvent(idata, "Skipped Error on parent", "", false);
							return true;
						}
					}
					else
					{
						logger.BeginEvent(idata, "Created", "", false);
					}
					if (_args.mirror)
						allowedItems.Remove(idata.Id);
				}
				else if (_args.overwrite)
				{
					if (!_args.preview)
					{
						var results = _comparer.FastCompare(idata, new Rainbow.Storage.Sc.ItemData(exists));
						if (results.AreEqual)
							logger.BeginEvent(idata, "Skipped", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						else
						{
							logger.BeginEvent(idata, "Changed", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), true);
							BulkUpdateContext bu = null;
							if (_args.bulkUpdate)
								bu = new BulkUpdateContext();
							EventDisabler ed = null;
							if (_args.eventDisabler)
								ed = new EventDisabler();

							using (new SecurityDisabler())
								scDatastore.Save(idata);
							logger.CompleteEvent(idata.Id.ToString());
							bu?.Dispose();
							ed?.Dispose();
						}
					}
					else
					{
						var results = _comparer.Compare(idata, new Rainbow.Storage.Sc.ItemData(exists));
						if (results.AreEqual)
							logger.BeginEvent(idata, "Skipped", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						//else if (results.IsBranchChanged)
						//	logger.RecordEvent(idata, "Branch Change", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
						else if (results.IsMoved)
							logger.BeginEvent(idata, "Moved", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						else if (results.IsRenamed)
							logger.BeginEvent(idata, "Renamed", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
						else if (results.IsTemplateChanged)
							logger.BeginEvent(idata, "Template Change", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")),
								false);
						else
							logger.BeginEvent(idata, "Changed", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")), false);
					}
					if (_args.mirror)
						allowedItems.Remove(idata.Id);
				}
			}
			return true;
		}
	}
}
