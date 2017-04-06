using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Rainbow.Diff;
using ScsContentMigrator.Args;
using ScsContentMigrator.CMRainbow;
using ScsContentMigrator.Data;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Handlers;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ScsContentMigrator
{
	public class ContentMigrationHandler : ScsHttpHandler
	{
		private static ConcurrentDictionary<string, int> _checksum = new ConcurrentDictionary<string, int>();
		private static readonly RemoteContentPuller Puller = new RemoteContentPuller();
		private static CompareContentTreeNode Root = new CompareContentTreeNode() { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		private static List<string> ServerList = new List<string>();
		internal static int remoteThreads = 1;
		internal static int writerThreads = 1;
		public override string Directive { get; set; } = "cmmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "ScsContentMigrator.Resources";
		public override string Icon => "/scs/cm.png";
		public override string Name => "Content Migrator";
		public override string CssStyle => "width:800px";
		public ContentMigrationHandler(string roles, string isAdmin, string users, string remotePullingThreads, string databaseWriterThreads) : base(roles, isAdmin, users)
		{
			if (remoteThreads == 1)
				int.TryParse(remotePullingThreads, out remoteThreads);
			if (writerThreads == 1)
				int.TryParse(databaseWriterThreads, out writerThreads);
			Timer t = new Timer(60 * 1000);
			t.Elapsed += async (sender, e) => await GenerateChecksum();
			t.Start();
		}
		private static async Task GenerateChecksum(List<CompareContentTreeNode> roots = null)
		{
			Task ret = Task.Run(() =>
			{
				var db = Factory.GetDatabase("master", false);
				if (db == null) return;
				foreach (CompareContentTreeNode node in roots ?? Root.Nodes.OfType<CompareContentTreeNode>())
				{
					GetChecksum(node.Id, true);
				}
			});
			await ret;
		}
		public static int GetChecksum(string id, bool force = false, bool childrenOnly = true)
		{
			if (!_checksum.ContainsKey(id) || force)
			{
				using (new SecurityDisabler())
				{
					Database db = Factory.GetDatabase("master", false);
					Stack<Item> processing = new Stack<Item>();
					Stack<Item> checksumGeneration = new Stack<Item>();
					processing.Push(db.GetItem(id));
					while (processing.Any())
					{
						Item child = processing.Pop();
						checksumGeneration.Push(child);
						foreach (Item subchild in child.Children)
							processing.Push(subchild);
					}
					while (checksumGeneration.Any())
					{
						Item cur = checksumGeneration.Pop();
						int checksum = 0;
						foreach (Item child in cur.Children.OrderBy(x => x.ID.ToString()))
						{
							checksum = (checksum + (_checksum.ContainsKey(child.ID.ToString()) ? _checksum[child.ID.ToString()].ToString() : "-1")).GetHashCode();
						}
						_checksum["children" + cur.ID.ToString()] = checksum;
						checksum = (checksum.ToString() + cur.Statistics.Revision).GetHashCode();
						_checksum[cur.ID.ToString()] = checksum;
					}
					
				}
			}
			if (childrenOnly)
			{
				if (!_checksum.ContainsKey("children" + id))
					return -1;
				return _checksum["children" + id];
			}
			if (!_checksum.ContainsKey(id))
				return -1;
			return _checksum[id];
		}
		public static void StartContentSync(RemoteContentPullArgs args)
		{
			Puller.PullContentItem(args);
		}
		public void BuildRoot(XmlNode node)
		{
			string dbName = "master";
			if (!string.IsNullOrWhiteSpace(node.Attributes?["database"]?.Value))
			{
				dbName = node.Attributes["database"].Value;
			}
			var db = Factory.GetDatabase(dbName, false);
			using (new SecurityDisabler())
			{
				var item = db.GetItem(node.InnerText);
				if (item != null)
					Root.Nodes.Add(new CompareContentTreeNode(item, false));
				GenerateChecksum(new List<CompareContentTreeNode>() { new CompareContentTreeNode(item) });
			}

		}

		public override void ProcessRequest(HttpContextBase context)
		{
			var file = GetFile(context);
			if (file == "cmcontenttree.scsvc")
				ReturnJson(context, GetContentTree(context));
			else if (file == "cmcontenttreegetitem.scsvc")
				ReturnJson(context, GetItemYaml(context));
			else if (file == "cmcontenttreepullitem.scsvc")
				ReturnJson(context, PullItem(context));
			else if (file == "cmserverlist.scsvc")
				ReturnJson(context, ServerList);
			else if (file == "cmopeartionstatus.scsvc")
				ReturnJson(context, GetOperationStatus(context));
			else if (file == "cmoperationlist.scsvc")
				ReturnJson(context, GetOperationList(context));
			else if (file == "cmstopoperation.scsvc")
				ReturnJson(context, StopOperation(context));
			else if (file == "cmapprovepreview.scsvc")
				ReturnJson(context, StartPreviewAsPull(context));
			else if (file == "cmqueuelength.scsvc")
				ReturnJson(context, OperationQueueLength(context));
			else if (file == "cmchecksum.scsvc")
				ReturnJson(context, GetChecksum(context));
			else
				ProcessResourceRequest(context);
		}

		private object GetChecksum(HttpContextBase context)
		{
			using (new SecurityDisabler())
			{
				return GetChecksum(HttpContext.Current.Request.QueryString["id"]);
			}
		}

		private object OperationQueueLength(HttpContextBase context)
		{
			var data = GetPostData(context);
			return RemoteContentPuller.GetOperation(data.operationId).QueuedItems();
		}

		private object StartPreviewAsPull(HttpContextBase context)
		{
			var data = GetPostData(context);
			RemoteContentPuller.GetOperation(data.operationId).RunPreviewAsFullOperation();
			return true;
		}

		private object StopOperation(HttpContextBase context)
		{
			var data = GetPostData(context);
			return RemoteContentPuller.StopOperation(data.operationId);
		}

		private object GetOperationList(HttpContextBase context)
		{
			return RemoteContentPuller.GetRunningOperations().OrderBy(x =>
			{
				DateTime tmp;
				if (DateTime.TryParseExact(x.FinishedTime, "MMM d h:mm tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out tmp))
				{
					return tmp;
				}
				return DateTime.Now;
			}).ToList();
		}

		private object GetOperationStatus(HttpContextBase context)
		{
			var data = GetPostData(context);
			return ((IEnumerable<object>)RemoteContentPuller.OperationStatus(data.operationId, (int)data.lineNumber)).ToList();
		}

		public void BuildServerList(XmlNode node)
		{
			ServerList.Add(node.InnerText);
		}

		private dynamic PullItem(HttpContextBase context)
		{
			var data = GetPostData(context);
			try
			{
				var args = new RemoteContentPullArgs(data);
				if (args.server != null)
				{
					return Puller.PullContentItem(args);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Problem pulling item from remote server", ex, this);
				data.Error = "Problem pulling item from remote server\n\n" + ex;
				return data;
			}
			return true;
		}

		private List<string> GetItemYaml(HttpContextBase context)
		{
			var data = GetPostData(context);
			try
			{
				var server = data.server;
				if (server != null)
				{
					WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
					data.server = null;
					return
						wc.UploadString($"{server}/scs/cmcontenttreegetitem.scsvc", "POST",
							JsonConvert.DeserializeObject<ExpandoObject>(data));
				}
			}
			catch (RuntimeBinderException)
			{

			}
			var db = Factory.GetDatabase(data.database);
			using (new SecurityDisabler())
			{
				Item item = db.GetItem(data.id);
				if (data.children)
					return item.GetYamlTree().ToList();
				return new List<string>() { item.GetYaml() };
			}
		}

		/// <summary>
		/// returns the content tree level for the given id
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private static object GetContentTree(HttpContextBase context)
		{
			var data = GetPostData(context);
			try
			{
				var args = new RemoteContentTreeArgs(data);
				if (args.server != null)
				{
					return GetResources.GetRemoteItem(args, args.id, true);
				}
			}
			catch (RuntimeBinderException)
			{

			}
			using (new SecurityDisabler())
				return string.IsNullOrWhiteSpace(data.id.ToString()) ? Root : new CompareContentTreeNode(Factory.GetDatabase(data.database.ToString()).GetItem(new ID(data.id)));
		}
	}
}
