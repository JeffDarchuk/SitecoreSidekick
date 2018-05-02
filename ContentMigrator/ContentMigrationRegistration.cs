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
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using SitecoreSidekick.Services.Interface;

namespace ScsContentMigrator
{
	public class ContentMigrationRegistration : ScsRegistration
	{
		private readonly ISitecoreDataAccessService _sitecoreDataAccessService;

		private static Checksum _checksum;
		public static CompareContentTreeNode Root { get; } = new CompareContentTreeNode { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		public int RemoteThreads { get; } = 1;
		public int WriterThreads { get; } = 1;
		public Dictionary<string, string> ServerList { get; } = new Dictionary<string, string>();
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
			if (RemoteThreads == 1)
			{
				int remoteTmp;
				int.TryParse(remotePullingThreads, out remoteTmp);
				RemoteThreads = remoteTmp;
			}

			if (WriterThreads == 1)
			{
				int writerTmp;
				int.TryParse(databaseWriterThreads, out writerTmp);
				WriterThreads = writerTmp;
			}

			Timer t = new Timer(20 * 1000);
			t.Elapsed += (sender, e) =>
			{
				if (Root != null)
					_checksum = new ChecksumGenerator().Generate(Root.Nodes.Select(x => new ID(x.Id)).ToList(), "master");
			};
			t.Start();
		}

		public override void Process(PipelineArgs args)
		{
			Task.Run(() =>
			{
				_checksum = new ChecksumGenerator().Generate(Root.Nodes.Select(x => new ID(x.Id)).ToList(), "master");
			});

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
			string serverValue = node.InnerText;
	  		if(!string.IsNullOrWhiteSpace(node.Attributes?["desc"]?.Value)){
				serverValue = node.Attributes["desc"].Value;
			} 
			
			ServerList.Add(node.InnerText, serverValue);	
		}

		public static int GetChecksum(string id)
		{
			return _checksum?.GetChecksum(id) ?? -1;
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
