using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.CSharp.RuntimeBinder;
using ScsContentMigrator.Args;
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
using System.Web.Mvc;
using MicroCHAP;
using ScsContentMigrator.Security;
using SitecoreSidekick;

namespace ScsContentMigrator
{
	public class ContentMigrationHandler : ScsHandler
	{
		private static readonly ConcurrentDictionary<string, int> Checksum = new ConcurrentDictionary<string, int>();
		private static readonly RemoteContentPuller Puller = new RemoteContentPuller();
		private static readonly CompareContentTreeNode Root = new CompareContentTreeNode { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		private static readonly List<string> ServerList = new List<string>();
		internal static int RemoteThreads = 1;
		internal static int WriterThreads = 1;

		// TODO: this is ugly. Should not be static; should be passed as a parameter to the classes that need it.
		internal static ScsHmacServer HmacServer;
		public override string Directive { get; set; } = "cmmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "ScsContentMigrator.Resources";
		public override string Icon => "/scs/cm.png";
		public override string Name => "Content Migrator";
		public override string CssStyle => "width:100%;min-width:800px;";

		public ContentMigrationHandler()
		{
		}

		public ContentMigrationHandler(string roles, string isAdmin, string users, string remotePullingThreads, string databaseWriterThreads, string authenticationSecret) : base(roles, isAdmin, users)
		{
			if (string.IsNullOrWhiteSpace(authenticationSecret))
			{
				throw new InvalidOperationException("Sitecore Sidekick Content Migrator was initialized with an empty shared secret. Make a copy of zSCSContentMigrator.Local.config.example, rename it to .config, and set up a unique, long, randomly generated shared secret there.");
			}

			if (authenticationSecret.Length < 32)
			{
				throw new InvalidOperationException("Sitecore Sidekick Content Migrator was initialized with an insecure shared secret. Please use a shared secret of 32 or more characters.");
			}

			RemoteContentService.SignatureService = new SignatureService(authenticationSecret);
			HmacServer = new ScsHmacServer(RemoteContentService.SignatureService, new UniqueChallengeStore());

			if (RemoteThreads == 1)
			{
				int.TryParse(remotePullingThreads, out RemoteThreads);
			}

			if (WriterThreads == 1)
			{
				int.TryParse(databaseWriterThreads, out WriterThreads);
			}

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
					// this caches the checksum value internally
					GetChecksum(node.Id, true);
				}
			});

			await ret;
		}

		public static int GetChecksum(string id, bool force = false, bool childrenOnly = true)
		{
			if (!Checksum.ContainsKey(id) || force)
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
							checksum = (checksum + (Checksum.ContainsKey(child.ID.ToString()) ? Checksum[child.ID.ToString()].ToString() : "-1")).GetHashCode();
						}
						Checksum["children" + cur.ID] = checksum;
						checksum = (checksum + cur.Statistics.Revision).GetHashCode();
						Checksum[cur.ID.ToString()] = checksum;
					}
					
				}
			}
			if (childrenOnly)
			{
				if (!Checksum.ContainsKey("children" + id))
					return -1;
				return Checksum["children" + id];
			}

			if (!Checksum.ContainsKey(id))
			{
				return -1;
			}

			return Checksum[id];
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
				{
					Root.Nodes.Add(new CompareContentTreeNode(item, false));
				}

#pragma warning disable 4014
				// async method intentionally not awaited to allow processing in the background and this method returning
				GenerateChecksum(new List<CompareContentTreeNode> { new CompareContentTreeNode(item) });
#pragma warning restore 4014
			}

		}


		public override ActionResult ProcessRequest(HttpContextBase context, string filename, dynamic data)
		{
			if (filename.Equals("cmcontenttree.scsvc", StringComparison.Ordinal))
				ReturnJson(context, GetContentTree(data));
			else if (filename.Equals("cmcontenttreegetitem.scsvc", StringComparison.Ordinal))
				ReturnJson(context, GetItemYaml(data));
			else if (filename.Equals("cmcontenttreepullitem.scsvc", StringComparison.Ordinal))
				ReturnJson(context, PullItem(data));
			else if (filename.Equals("cmserverlist.scsvc", StringComparison.Ordinal))
				ReturnJson(context, ServerList);
			else if (filename.Equals("cmopeartionstatus.scsvc", StringComparison.Ordinal))
				ReturnJson(context, GetOperationStatus(data));
			else if (filename.Equals("cmoperationlist.scsvc", StringComparison.Ordinal))
				ReturnJson(context, GetOperationList());
			else if (filename.Equals("cmstopoperation.scsvc", StringComparison.Ordinal))
				ReturnJson(context, StopOperation(data));
			else if (filename.Equals("cmapprovepreview.scsvc", StringComparison.Ordinal))
				ReturnJson(context, StartPreviewAsPull(data));
			else if (filename.Equals("cmqueuelength.scsvc", StringComparison.Ordinal))
				ReturnJson(context, OperationQueueLength(data));
			else if (filename.Equals("cmchecksum.scsvc", StringComparison.Ordinal))
				ReturnJson(context, GetChecksum(context.Request.QueryString["id"]));
			else
				ProcessResourceRequest(context, filename, data);

			return null;
		}

		private object OperationQueueLength(dynamic data)
		{
			return RemoteContentPuller.GetOperation(data.operationId).QueuedItems();
		}

		private object StartPreviewAsPull(dynamic data)
		{
			RemoteContentPuller.GetOperation(data.operationId).RunPreviewAsFullOperation();
			return true;
		}

		private object StopOperation(dynamic data)
		{
			return RemoteContentPuller.StopOperation(data.operationId);
		}

		private object GetOperationList()
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

		private object GetOperationStatus(dynamic data)
		{
			return ((IEnumerable<object>)RemoteContentPuller.OperationStatus(data.operationId, (int)data.lineNumber)).ToList();
		}

		public void BuildServerList(XmlNode node)
		{
			ServerList.Add(node.InnerText);
		}

		private dynamic PullItem(dynamic data)
		{
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

		private List<string> GetItemYaml(dynamic data)
		{
			string payload = data.payload;
			if (!HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] {new SignatureFactor("payload", payload)}))
			{
				System.Web.HttpContext.Current.Response.StatusCode = 403;
				return null;
			}

			var db = Factory.GetDatabase(data.database);
			using (new SecurityDisabler())
			{
				Item item = db.GetItem(data.id);
				if (data.children)
				{
					return item.GetYamlTree().ToList();
				}

				return new List<string> { item.GetYaml() };
			}
		}

		/// <summary>
		/// returns the content tree level for the given id
		/// </summary>
		private static object GetContentTree(dynamic data)
		{
			try
			{
				var args = new RemoteContentTreeArgs(data);
				if (args.server != null)
				{
					return RemoteContentService.GetRemoteItem(args, args.id, true);
				}
			}
			catch (RuntimeBinderException)
			{

			}

			string payload = data.payload;
			if (!HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] { new SignatureFactor("payload", payload) }))
			{
				System.Web.HttpContext.Current.Response.StatusCode = 403;
				return null;
			}

			using (new SecurityDisabler())
			{
				return string.IsNullOrWhiteSpace(data.id.ToString()) ? Root : new CompareContentTreeNode(Factory.GetDatabase(data.database.ToString()).GetItem(new ID(data.id)));
			}
		}
	}
}
