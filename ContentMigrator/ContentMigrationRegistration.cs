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
using System.IO;
using System.Web.Mvc;
using MicroCHAP;
using ScsContentMigrator.Models;
using ScsContentMigrator.Security;
using SitecoreSidekick;
using SitecoreSidekick.Core;
using SitecoreSidekick.Models;

namespace ScsContentMigrator
{
	public class ContentMigrationRegistration : ScsRegistration
	{
		private static readonly ConcurrentDictionary<string, int> Checksum = new ConcurrentDictionary<string, int>();
		private static readonly RemoteContentPuller _puller = new RemoteContentPuller();
		public CompareContentTreeNode Root { get; } = new CompareContentTreeNode { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		internal static int RemoteThreads = 1;
		internal static int WriterThreads = 1;
		public List<string> ServerList { get; } = new List<string>();
		public RemoteContentPuller Puller => _puller;
		public ScsHmacServer HmacServer { get; }
		public override string Directive => "cmmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "ScsContentMigrator.Resources";
		public override string Identifier => "cm";
		public override Type Controller => typeof(ContentMigrationController);
		public override string Icon => "/scs/cm/resources/cm.png";
		public override string Name => "Content Migrator";
		public override string CssStyle => "width:100%;min-width:800px;";

		public ContentMigrationRegistration(string roles, string isAdmin, string users, string remotePullingThreads, string databaseWriterThreads, string authenticationSecret) : base(roles, isAdmin, users)
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
		public void BuildServerList(XmlNode node)
		{
			ServerList.Add(node.InnerText);
		}

		private async Task GenerateChecksum(List<CompareContentTreeNode> roots = null)
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
			_puller.PullContentItem(args);
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
		


	}
}
