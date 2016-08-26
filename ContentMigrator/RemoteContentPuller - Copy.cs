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
using Sitecore.Data.Query;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Rainbow;

namespace ScsContentMigrator
{
	public class RemoteContentPuller
	{
		DefaultDeserializer deserializer = new DefaultDeserializer(new DefaultLogger(), new DefaultFieldFilter());
		SitecoreDataStore scDatastore;
		private static ConcurrentDictionary<string, OperationStatus> _operation = new ConcurrentDictionary<string, OperationStatus>();

		public RemoteContentPuller()
		{
			scDatastore = new SitecoreDataStore(deserializer);
		}

		public dynamic PullContentItem(RemoteContentPullArgs args)
		{
			string operationId = RegisterEvent();
			Task.Run(() =>
			{
				Stopwatch sw = Stopwatch.StartNew();
				dynamic ret = new ExpandoObject();
				ret.Items = 0;
				IEnumerable<IItemData> idataList = GetRemoteItemData(args);
				Database db = Factory.GetDatabase(args.database);

				string id = args.id;
				Item root = null;
				HashSet<Guid> allowedItems = new HashSet<Guid>();
				using (new BulkUpdateContext())
				{
					using (new SecurityDisabler())
					{
						using (new EventDisabler())
						{
							if (args.mirror)
							{
								Stack<Item> items = new Stack<Item>();
								items.Push(db.GetItem(new ID(idataList.First().Id)));
								while (items.Any())
								{
									var curItem = items.Pop();
									allowedItems.Add(curItem.ID.Guid);
									foreach (Item child in curItem.Children)
										items.Push(child);
								}
							}
							ProcessItems(args, idataList, ret, operationId, root, db, allowedItems, id);
							if (args.mirror)
								foreach (Guid guid in allowedItems)
								{
									Item item = db.GetItem(new ID(guid));
									if (item != null)
									{
										RecordEvent(ret, item.Name, item.ID.ToString(), item.Paths.FullPath, "Recycle", operationId);
										item.Recycle();
									}
								}
						}
					}
				}
				ret.Time = sw.ElapsedMilliseconds / 1000;
				_operationLines[operationId].Add(ret);
				Task.Run(() =>
				{
					Thread.Sleep(60000);
					List<dynamic> outStuff;
					_operationLines.TryRemove(operationId, out outStuff);
				});
			});
			return operationId;
		}

		private void ProcessItems(RemoteContentPullArgs args, IEnumerable<IItemData> idataList, dynamic ret, string operationId, Item root,
			Database db, HashSet<Guid> allowedItems, string id)
		{
			foreach (IItemData idata in idataList)
			{
				try
				{
					if (idata is ErrorItemData)
					{
						RecordEvent(ret, idata, "Error", operationId);
						continue;
					}
					if (root == null)
					{
						root = db.GetItem(new ID(idata.Id));
						Item parent = db.GetItem(new ID(idata.ParentId));
						IItemData tmpData = idata;
						if (args.pullParent && parent == null)
						{
							Stack<IItemData> path = new Stack<IItemData>();
							args.children = false;
							while (parent == null)
							{
								parent = db.GetItem(new ID(tmpData.ParentId));
								path.Push(tmpData);
								args.id = tmpData.ParentId.ToString();
								tmpData = GetRemoteItemData(args).First();
							}
							while (path.Any())
							{
								var cur = path.Pop();
								RecordEvent(ret, cur, "Insert", operationId);
								InstallItemData(cur, args, db);
								if (args.mirror)
									allowedItems.Remove(cur.Id);
							}
							args.id = id;
						}
					}
					InstallItem(args, ret, operationId, db, allowedItems, idata);
				}
				catch (Exception e)
				{
					Log.Error("problem deserializing sitecore data", e, this);
					RecordEvent(ret, idata, "Error", operationId);
					if (args.mirror)
					{
						Item parent = db.GetItem(new ID(idata.ParentId));
						Stack<Item> delrem = new Stack<Item>(parent.Children);

						while (delrem.Any())
						{
							Item cur = delrem.Pop();
							allowedItems.Remove(cur.ID.Guid);
							foreach (Item child in cur.Children)
								delrem.Push(child);
						}
					}
				}
			}
		}

		private void InstallItem(RemoteContentPullArgs args, dynamic ret, string operationId, Database db, HashSet<Guid> allowedItems,
			IItemData idata)
		{
			bool exists = db.GetItem(new ID(idata.Id)) != null;
			if (!exists)
			{
				RecordEvent(ret, idata, "Insert", operationId);
				InstallItemData(idata, args, db);
				if (args.mirror)
					allowedItems.Remove(idata.Id);
			}
			else if (args.overwrite)
			{
				RecordEvent(ret, idata, "Update", operationId);
				InstallItemData(idata, args, db);
				if (args.mirror)
					allowedItems.Remove(idata.Id);
			}
		}

		internal static string RegisterEvent()
		{
			var ret = Guid.NewGuid().ToString();
			_operationLines[ret] = new List<dynamic>();
			return ret;
		}

		private IEnumerable<IItemData> GetRemoteItemData(RemoteContentPullArgs args)
		{
			List<IItemData> ret = new List<IItemData>();
			bool children = args.children;
			args.children = false;
			Queue<string> pullableItems = new Queue<string>();
			WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
			pullableItems.Enqueue(args.id);
			while (pullableItems.Any())
			{
				string cur = pullableItems.Dequeue();
				args.id = cur;
				string yamlList = null;
				try
				{
					yamlList = wc.UploadString($"{args.server}/scs/cmcontenttreegetitem.scsvc", "POST",
						args.GetSerializedData());
				}
				catch (Exception e)
				{
					Log.Error("Problem reading remote server item data", e, this);
				}
				if (string.IsNullOrWhiteSpace(yamlList))
				{
					yield return new ErrorItemData() {Id = Guid.Parse(args.id), Path = "Problem reading remote server item data" };
					continue;
				}
				List<string> remoteItems = JsonConvert.DeserializeObject<List<string>>(yamlList);
				var formatter = new YamlSerializationFormatter(null, null);
				foreach (string yaml in remoteItems)
				{
					using (var ms = new MemoryStream())
					{
						IItemData itemData = null;
						try
						{
							var bytes = Encoding.UTF8.GetBytes(yaml);
							ms.Write(bytes, 0, bytes.Length);

							ms.Seek(0, SeekOrigin.Begin);
							itemData = formatter.ReadSerializedItem(ms, args.id);
							itemData.DatabaseName = args.database;
						}
						catch (Exception e)
						{
							Log.Error("Problem reading yaml from remote server", e, this);
						}
						if (itemData == null)
						{
							yield return new ErrorItemData() { Id = Guid.Parse(args.id), Path = "Problem reading yaml from remote server" };
							continue;
						}
						yield return itemData;
					}
				}
				if (children)
				{
					ContentTreeNode node = null;
					try
					{
						node =	JsonConvert.DeserializeObject<ContentTreeNode>(wc.UploadString($"{args.server}/scs/cmcontenttree.scsvc", "POST",
								$@"{{ ""id"": ""{cur}"", ""database"": ""{args.database}"", ""server"": ""{args.server}"" }}"));
						foreach (ContentTreeNode child in node.Nodes)
						{
							pullableItems.Enqueue(child.Id);
						}
					}
					catch (Exception e)
					{
						Log.Error("Problem getting children of node " + cur, e, this);
					}
					if (node == null)
						yield return new ErrorItemData() {Id = Guid.Parse(cur), Path = "Problem getting children of node" };
				}
			}
		}

		private void RecordEvent(dynamic ret, IItemData data, string operation, string operationId)
		{
			RecordEvent(ret, data.Name, data.Id.ToString(), data.Path, operation, operationId);
		}

		private void RecordEvent(dynamic ret, string name, string id, string path, string operation, string operationId)
		{
			dynamic cur = new ExpandoObject();
			cur.Name = name;
			cur.Id = id;
			cur.Path = path;
			cur.Operation = operation;
			ret.Items++;
			_operationLines[operationId].Add(cur);
		}
		public ContentTreeNode GetRemoteContentTreeNode(RemoteContentTreeArgs args)
		{
			WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
			return JsonConvert.DeserializeObject<ContentTreeNode>(wc.UploadString($"{args.server}/scs/cmcontenttree.scsvc", "POST", args.GetSerializedData()));
		}

		private void InstallItemData(IItemData data, RemoteContentPullArgs args, Database db)
		{
			scDatastore.Save(data);
		}

		public static IEnumerable<dynamic> OperationStatus(string operationId, int lineNumber)
		{
			if (_operationLines.ContainsKey(operationId))
				for (int i = lineNumber; i < _operationLines[operationId].Count; i++)
					yield return _operationLines[operationId][i];
		}
	}
}
