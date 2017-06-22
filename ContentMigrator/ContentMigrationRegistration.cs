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
using Sitecore.Pipelines;
using SitecoreSidekick;
using SitecoreSidekick.Core;
using SitecoreSidekick.Models;

namespace ScsContentMigrator
{
	public class ContentMigrationRegistration : ScsRegistration
	{
		private static ChecksumGenerator Checksum = new ChecksumGenerator();
		private static readonly RemoteContentPuller _puller = new RemoteContentPuller();
		public static CompareContentTreeNode Root { get; } = new CompareContentTreeNode { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		internal static int RemoteThreads = 1;
		internal static int WriterThreads = 1;
		public List<string> ServerList { get; } = new List<string>();
		public RemoteContentPuller Puller => _puller;
		public ScsHmacServer HmacServer { get; set; }
		public override string Directive => "cmmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "ScsContentMigrator.Resources";
		public override string Identifier => "cm";
		public override Type Controller => typeof(ContentMigrationController);
		public override string Icon => "/scs/cm/resources/cm.png";
		public override string Name => "Content Migrator";
		public override string CssStyle => "width:100%;min-width:800px;";
		public string AuthenticationSecret { get; set; }

		public ContentMigrationRegistration(string roles, string isAdmin, string users, string remotePullingThreads, string databaseWriterThreads) : base(roles, isAdmin, users)
		{
			if (RemoteThreads == 1)
			{
				int.TryParse(remotePullingThreads, out RemoteThreads);
			}

			if (WriterThreads == 1)
			{
				int.TryParse(databaseWriterThreads, out WriterThreads);
			}

			//Timer t = new Timer(60 * 1000);
			//t.Elapsed += async (sender, e) => await GenerateChecksum();
			//t.Start();
		}

		public override void Process(PipelineArgs args)
		{
#pragma warning disable 4014
			//async call to rebuild the checksum that won't block startup.
			//GenerateChecksum();
#pragma warning restore 4014
			if (string.IsNullOrWhiteSpace(AuthenticationSecret))
			{
				throw new InvalidOperationException("Sitecore Sidekick Content Migrator was initialized with an empty shared secret. Make a copy of zSCSContentMigrator.Local.config.example, rename it to .config, and set up a unique, long, randomly generated shared secret there.");
			}

			if (AuthenticationSecret.Length < 32)
			{
				throw new InvalidOperationException("Sitecore Sidekick Content Migrator was initialized with an insecure shared secret. Please use a shared secret of 32 or more characters.");
			}

			RemoteContentService.SignatureService = new SignatureService(AuthenticationSecret);
			HmacServer = new ScsHmacServer(RemoteContentService.SignatureService, new UniqueChallengeStore());
			base.Process(args);
		}

		public void BuildServerList(XmlNode node)
		{
			ServerList.Add(node.InnerText);
		}

		//public static async Task GenerateChecksum()
		//{
		//	Task ret = Task.Run(() =>
		//	{
		//		Checksum = new ChecksumGenerator().Generate(Root.Nodes.Select(x => new ID(x.Id)), "master");
		//	});

		//	await ret;
		//}

		public static int GetChecksum(string id)
		{
			return Checksum.Generate(id, "master");
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
			}
		}
	}
}
