using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using Sitecore.Diagnostics;
using SitecoreSidekick.ContentTree;

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
							itemData.DatabaseName = args.database;
						}
						catch (Exception e)
						{
							Log.Error("Problem reading yaml from remote server", e, args);
						}
						if (itemData != null)
						{
							return itemData;
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Error("Error getting remote item data for " + itemId, e, args);
			}
			return null;
		}
		public static ContentTreeNode GetRemoteItem(RemoteContentTreeArgs args, string itemId)
		{
			try
			{
				WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
				var node = JsonConvert.DeserializeObject<ContentTreeNode>(wc.UploadString($"{args.server}/scs/cmcontenttree.scsvc", "POST",
								$@"{{ ""id"": ""{itemId}"", ""database"": ""{args.database}""}}"));
				return node;
			}
			catch (Exception e)
			{
				Log.Error("Problem getting children of node " + itemId, e, args);
			}
			return null;
		}
		public static IEnumerable<string> GetRemoteItemChildren(RemoteContentTreeArgs args, string itemId)
		{
			return GetRemoteItem(args, itemId).Nodes.Select(x => x.Id);
		}
	}
}
