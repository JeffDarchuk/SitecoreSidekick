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
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Rainbow;

namespace ScsContentMigrator
{
	public class OperationStatus
	{
		private readonly List<dynamic> _lines = new List<dynamic>();
		int counter = 0;
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

		private Stopwatch sw = new Stopwatch();
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
			Task.Run(() =>
			{
				using (new BulkUpdateContext())
				using (new EventDisabler())
				using (new SecurityDisabler())
				{
					while (!Completed && (!doneRemote || _installerQueue.Count > 0))
					{
						try
						{
							IItemData tmp = null;
							if (_installerQueue.TryDequeue(out tmp))
							{
								InstallItem(tmp);
							}
							else
							{
								Thread.Sleep(10);
							}
						}
						catch (Exception e)
						{
							Log.Error("Problem installing item", e, this);
						}
					}
				
				if (!Completed && _args.mirror)
					foreach (Guid guid in allowedItems)
					{
						try
						{
							Item item = db.GetItem(new ID(guid));
							if (item != null)
							{
								RecordEvent(item.Name, item.ID.ToString(), item.Paths.FullPath, "Recycle", GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")));
								item.Recycle();
							}
						}
						catch (Exception e)
						{
								Log.Error("Problem recycling item", e, this);
							}
					}
				}
				dynamic last = new ExpandoObject();
				last.Time = sw.Elapsed.TotalSeconds;
				last.Items = _lines.Count;
				lock (_listLocker)
					Lines.Add(last);
				Completed = true;
			});
		}

		public void CancelOperation()
		{
			Completed = true;
			doneRemote = true;
		}
		public void EndOperation()
		{
			doneRemote = true;
		}
		public string OperationId { get; }

		private void Init()
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
					RecordEvent(cur, "Insert", GetSrc(ThemeManager.GetIconImage(Factory.GetDatabase(cur.DatabaseName).GetItem(new ID(cur.Id)), 32, 32, "", "")));
				}
			}
			RootNode = new ContentTreeNode(parent.Database.GetItem(new ID(idata.Id)));
		}

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
		public void InstallItem(object o)
		{
			IItemData idata = (IItemData)o;
			if (idata is ErrorItemData)
			{
				RecordEvent(idata, "Error", "");
				return;
			}
			Item exists = db.GetItem(new ID(idata.Id));
			if (exists == null)
			{
				scDatastore.Save(idata);
				RecordEvent(idata, "Insert", GetSrc(ThemeManager.GetIconImage(Factory.GetDatabase(idata.DatabaseName).GetItem(new ID(idata.Id)), 32, 32, "", "")));

				if (_args.mirror)
					allowedItems.Remove(idata.Id);
			}
			else if (_args.overwrite)
			{
				RecordEvent(idata, "Update", GetSrc(ThemeManager.GetIconImage(exists, 32, 32, "", "")));
				scDatastore.Save(idata);
				if (_args.mirror)
					allowedItems.Remove(idata.Id);
			}
		}
		private void RecordEvent(IItemData data, string status, string icon)
		{
			RecordEvent(data.Name, data.Id.ToString(), data.Path, status, icon);
		}

		private void RecordEvent(string name, string id, string path, string status, string icon)
		{
			dynamic cur = new ExpandoObject();
			cur.Name = name;
			cur.Id = id;
			cur.Path = path;
			cur.Icon = icon;
			cur.Operation = status;
			lock (_listLocker)
				Lines.Add(cur);
		}

		private string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal)+5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
	}
}
