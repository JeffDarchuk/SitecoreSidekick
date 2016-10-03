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
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Rainbow;

namespace ScsContentMigrator
{
	public class OperationStatus
	{
		private readonly List<dynamic> _lines = new List<dynamic>();
		private RemoteContentPullArgs _args;
		private bool doneRemote = false;
		public List<dynamic> Lines => _lines;
		private object _listLocker = new object();
		private CMThreadPool tp = null;
		DefaultDeserializer deserializer = new DefaultDeserializer(new DefaultLogger(), new DefaultFieldFilter());
		ConcurrentQueue<IItemData> _installerQueue = new ConcurrentQueue<IItemData>();
		SitecoreDataStore scDatastore;
		private Item root = null;
		private Database db;
		HashSet<Guid> allowedItems = new HashSet<Guid>();
		internal ContentTreeNode RootNode;
		internal bool Completed = false;
		private object finishLocker = new object();
		internal bool Cancelled = false;
		private Stopwatch sw = new Stopwatch();

		public bool IsPreview => _args.preview;

		public OperationStatus(RemoteContentPullArgs args, string operationId)
		{
			sw.Start();
			_args = args;
			scDatastore = new SitecoreDataStore(deserializer);
			this.OperationId = operationId;
			db = Factory.GetDatabase(args.database);
			tp = new CMThreadPool(this);
			Init();
			tp.Queue(GetNextItem, _args.id);
			for (int i = 0; i < ContentMigrationHandler.writerThreads; i++)
				RunDatabaseWriterProcess();
		}

		public void RunPreviewAsFullOperation()
		{
			if (!_args.preview) return;
			sw = new Stopwatch();
			sw.Start();
			_lines.Clear();
			_args.preview = false;
			Completed = false;
			doneRemote = false;
			root = null;
			tp = new CMThreadPool(this);
			Init();
			tp.Queue(GetNextItem, _args.id);
			for (int i = 0; i < ContentMigrationHandler.writerThreads; i++)
				RunDatabaseWriterProcess();
		}

		private void Init()
		{
			using (new SecurityDisabler())
			{
				IItemData idata = GetResources.GetRemoteItemData(_args, _args.id);

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
						_args.id = tmpData.ParentId.ToString();
						tmpData = GetResources.GetRemoteItemData(_args, tmpData.ParentId.ToString());
					}
					while (path.Any())
					{
						var cur = path.Pop();

						InstallItem(cur);
					}
					_args.children = tmp;

				}

				RootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.Id)));
				if (RootNode.Icon == "")
				{
					RootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.TemplateId)))
					{
						Id = new ID(idata.Id).ToString(),
						DisplayName = idata.Name
					};
				}
				RootNode.Server = _args.server;
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
										RecordEvent(item.Name, item.ID.ToString(), item.Paths.FullPath, "Recycle",
											GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")), item.Database.Name);
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
						last.Items = _lines.Count;
						last.Cancelled = Cancelled;
						lock (_listLocker)
							Lines.Add(last);
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
							if (InstallItem(tmp))
								tmp = null;
						}
						else if (tmp != null)
						{
							Thread.Sleep(10);
							if (InstallItem(tmp))
								tmp = null;
						}
						else
						{
							Thread.Sleep(10);
						}
					}
					catch (Exception e)
					{
						if (tmp != null)
							RecordEvent(tmp.Name, tmp.Id.ToString(), e.ToString(), "Error", "", tmp.DatabaseName);
						else
							RecordEvent("unknown", "",e.ToString(), "Error", "", "");
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
				RecordEvent(new ErrorItemData { Id = Guid.Parse(err[0]), Path = err[1] }, "Error", "");
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
				RecordEvent(idata, "Error", "");
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
						RecordEvent(idata, "Insert",
							GetSrc(ThemeManager.GetIconImage(Factory.GetDatabase(idata.DatabaseName).GetItem(new ID(idata.Id)), 32, 32, "",
								"")));
					}
					catch (ParentItemNotFoundException)
					{
						return false;
					}
				}
				else
				{
					var item = Factory.GetDatabase(idata.DatabaseName).GetItem(new ID(idata.TemplateId));
					RecordEvent(idata, "Insert", item == null ? "" :
						GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")));
				}

				if (_args.mirror)
					allowedItems.Remove(idata.Id);
			}
			else if (_args.overwrite)
			{
				if (exists.ParentID == new ID(idata.ParentId))
					RecordEvent(idata, "Update", GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
				else
				{
					if (!_args.preview)
						exists.MoveTo(exists.Database.GetItem(new ID(idata.ParentId)));
					RecordEvent(idata, "Move", GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
				}
				if (!_args.preview)
					scDatastore.Save(idata);
				if (_args.mirror)
					allowedItems.Remove(idata.Id);
			}
			return true;
		}
		private void RecordEvent(IItemData data, string status, string icon)
		{
			RecordEvent(data.Name, data.Id.ToString(), data.Path, status, icon, data.DatabaseName);
		}

		private void RecordEvent(string name, string id, string path, string status, string icon, string database)
		{
			dynamic cur = new ExpandoObject();
			cur.Name = name;
			cur.Id = id;
			cur.Path = path;
			cur.Icon = icon;
			cur.Operation = status;
			cur.DatabaseName = database;
			lock (_listLocker)
				Lines.Add(cur);
		}

		private string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
	}
}
