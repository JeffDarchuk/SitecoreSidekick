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
		internal bool Cancelled = false;
		private Stopwatch sw = new Stopwatch();
		private HashSet<string> _ids;
		private ConcurrentHashSet<string> _currentTracker = new ConcurrentHashSet<string>();
		private DateTime? _startedTime = null;
		private DateTime? _finishedTime = null;
		public string StartedTime => _startedTime?.ToString("MMM dd h:mm tt") ?? "";
		public string FinishedTime => _finishedTime?.ToString("MMM dd h:mm tt") ?? "";

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
			foreach(string id in _ids)
				tp.Queue(GetNextItem, id);
			for (int i = 0; i < ContentMigrationHandler.writerThreads; i++)
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
			for (int i = 0; i < ContentMigrationHandler.writerThreads; i++)
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
							_ids.Remove(id);
							_ids.Add(tmpData.ParentId.ToString());
							tmpData = GetResources.GetRemoteItemData(_args, tmpData.ParentId.ToString());
						}
						while (path.Any())
						{
							var cur = path.Pop();

							InstallItem(cur);
						}
						_args.children = tmp;

					}
					if (fromPreview) return;
					var RootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.Id)));
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
			Task.Run(() =>
			{
				using (new SecurityDisabler())
				{
					BulkUpdateContext bu = null;
					if (_args.bulkUpdate)
						bu = new BulkUpdateContext();
					EventDisabler ed = null;
					if (_args.eventDisabler)
						ed = new EventDisabler();

					ProcessItemQueue();

					bu?.Dispose();
					ed?.Dispose();
					if (Completed) return;
					lock (finishLocker)
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
										logger.RecordEvent(item.Name, item.ID.ToString(), item.Paths.FullPath, "Recycle",
											logger.GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")), item.Database.Name);
										if (!_args.preview)
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
					}
				}
			});
		}

		private void ProcessItemQueue()
		{
			IItemData tmp = null;
			using (new SecurityDisabler())
				while (!Completed && !Cancelled && (!doneRemote || _installerQueue.Count > 0))
				{
					try
					{
						if (tmp == null && _installerQueue.TryDequeue(out tmp))
						{
							if (_currentTracker.Contains(tmp.ParentId.ToString()))
							{
								Thread.Sleep(10);
								continue;
							}
							_currentTracker.Add(tmp.Id.ToString());
							if (InstallItem(tmp))
							{
								_currentTracker.Remove(tmp.Id.ToString());
								tmp = null;
							}
						}
						else if (tmp != null)
						{
							Thread.Sleep(10);
							if (InstallItem(tmp))
							{
								_currentTracker.Remove(tmp.Id.ToString());
								tmp = null;
							}
						}
						else
						{
							Thread.Sleep(10);
						}
					}
					catch (Exception e)
					{
						if (tmp != null)
						{
							logger.RecordEvent(tmp.Name, tmp.Id.ToString(), e.ToString(), "Error", "", tmp.DatabaseName);
							_currentTracker.Remove(tmp.Id.ToString());
						}
						else
							logger.RecordEvent("unknown", "", e.ToString(), "Error", "", "");
						Log.Error("Problem installing item", e, this);
						tmp = null;
					}						
				}
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
				logger.RecordEvent(new ErrorItemData { Id = Guid.Parse(err[0]), Path = err[1] }, "Error", "");
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
				logger.RecordEvent(idata, "Error", "");
				return true;
			}
			Item exists = db.GetItem(new ID(idata.Id));
			
			if (exists == null)
			{
				if (!_args.preview)
				{
					try
					{
						scDatastore.Save(idata);
					}
					catch (ParentItemNotFoundException)
					{
						logger.RecordEvent(idata, "Skipped Error on parent", "");
						return true;
					}
				}
				else
				{
					logger.RecordEvent(idata, "Created", "");
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
						logger.RecordEvent(idata, "Skipped", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					else
						scDatastore.Save(idata);
				}
				else
				{
					var results = _comparer.Compare(idata, new Rainbow.Storage.Sc.ItemData(exists));
					if (results.AreEqual)
						logger.RecordEvent(idata, "Skipped", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					//else if (results.IsBranchChanged)
					//	logger.RecordEvent(idata, "Branch Change", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					else if (results.IsMoved)
						logger.RecordEvent(idata, "Moved", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					else if (results.IsRenamed)
						logger.RecordEvent(idata, "Renamed", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					else if (results.IsTemplateChanged)
						logger.RecordEvent(idata, "Template Change", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					else if (results.ChangedVersions.Any())
						logger.RecordEvent(idata, "New Version", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
					else
						logger.RecordEvent(idata, "Changed", logger.GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
				}
				if (_args.mirror)
					allowedItems.Remove(idata.Id);
			}
			return true;
		}
	}
}
