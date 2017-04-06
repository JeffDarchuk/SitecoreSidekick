using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rainbow.Diff;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using ScsContentMigrator.CMRainbow;
using ScsContentMigrator.Data;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using SitecoreSidekick.ContentTree;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Data.Items;
using System.Web;
using Rainbow.Diff.Fields;
using Sitecore.SecurityModel;

namespace ScsContentMigrator
{
	public class GetResources
	{

		public static IItemData GetRemoteItemData(RemoteContentPullArgs args, string itemId)
		{
			try
			{
				WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
				string yamlList = wc.UploadString($"{args.server}/scs/cmcontenttreegetitem.scsvc", "POST",
					args.GetSerializedData(itemId, false));
				string yaml = JsonConvert.DeserializeObject<List<string>>(yamlList).FirstOrDefault();
				var ret = DeserializeYaml(yaml, itemId);
				if (ret == null)
					return null;
				ret.DatabaseName = args.database;
				return ret;
			}
			catch (Exception e)
			{
				Log.Error("Error getting remote item data for " + itemId, e, args);
			}
			return null;
		}

		public static IItemData DeserializeYaml(string yaml, string itemId)
		{
			var formatter = new YamlSerializationFormatter(null, null);
			if (yaml != null)
			{
				using (var ms = new MemoryStream())
				{
					IItemData itemData = null;
					try
					{
						var bytes = Encoding.UTF8.GetBytes(yaml);
						ms.Write(bytes, 0, bytes.Length);

						ms.Seek(0, SeekOrigin.Begin);
						itemData = formatter.ReadSerializedItem(ms, itemId);
					}
					catch (Exception e)
					{
						Log.Error("Problem reading yaml from remote server", e);
					}
					if (itemData != null)
					{
						return itemData;
					}
				}
			}
			return null;
		}
		public static CompareContentTreeNode GetRemoteItem(RemoteContentTreeArgs args, string itemId, bool diff)
		{
			try
			{
				WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
				var node = JsonConvert.DeserializeObject<CompareContentTreeNode>(wc.UploadString($"{args.server}/scs/cmcontenttree.scsvc", "POST",
								$@"{{ ""id"": ""{itemId}"", ""database"": ""{args.database}""}}"));
				if (!diff)
					return node;
				else
				{
					node.BuildDiff(args.database, itemId);
					return node;
				}
			}
			catch (Exception e)
			{
				Log.Error("Problem getting children of node " + itemId, e, args);
			}
			return null;
		}

		public static IEnumerable<string> GetRemoteItemChildren(RemoteContentTreeArgs args, string itemId)
		{
			return GetRemoteItem(args, itemId, false).Nodes.Select(x => x.Id);
		}
	}
}
