using ScsContentMigrator.Data;
using ScsContentMigrator.Security;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using ScsContentMigrator.Core.Interface;
using Sitecore.Events;
using Sitecore.Web.Authentication;
using SitecoreSidekick.Services.Interface;

namespace ScsContentMigrator
{
	public class ContentMigrationRegistration : ScsRegistration
	{
		private readonly ISitecoreDataAccessService _sitecoreDataAccessService;
		private readonly IChecksumManager _checksumManager;
		public static CompareContentTreeNode Root { get; } = new CompareContentTreeNode { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		public int RemoteThreads { get; } = 1;
		public int WriterThreads { get; } = 1;
		public List<string> ServerList { get; } = new List<string>();
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
			_sitecoreDataAccessService = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
			_checksumManager = Bootstrap.Container.Resolve<IChecksumManager>();
			if (RemoteThreads == 1)
			{
				int.TryParse(remotePullingThreads, out var remoteTmp);
				RemoteThreads = remoteTmp;
			}

			if (WriterThreads == 1)
			{
				int.TryParse(databaseWriterThreads, out var writerTmp);
				WriterThreads = writerTmp;
			}
		}

		public override void Process(PipelineArgs args)
		{
			_checksumManager.RegenerateChecksum();
			_checksumManager.StartChecksumTimer();
			if (string.IsNullOrWhiteSpace(AuthenticationSecret))
			{
				throw new InvalidOperationException("Sitecore Sidekick Content Migrator was initialized with an empty shared secret. Make a copy of zSCSContentMigrator.Local.config.example, rename it to .config, and set up a unique, long, randomly generated shared secret there.");
			}

			if (AuthenticationSecret.Length < 32)
			{
				throw new InvalidOperationException("Sitecore Sidekick Content Migrator was initialized with an insecure shared secret. Please use a shared secret of 32 or more characters.");
			}
			base.Process(args);
		}

		public void BuildServerList(XmlNode node)
		{
			ServerList.Add(node.InnerText);
		}

		public void BuildRoot(XmlNode node)
		{
			string dbName = "master";
			if (!string.IsNullOrWhiteSpace(node.Attributes?["database"]?.Value))
			{
				dbName = node.Attributes["database"].Value;
			}

			var item = _sitecoreDataAccessService.GetItemData(node.InnerText, dbName);
			if (item != null)
			{
				Root.Nodes.Add(new CompareContentTreeNode(item, false));
			}
		}
	}
}
