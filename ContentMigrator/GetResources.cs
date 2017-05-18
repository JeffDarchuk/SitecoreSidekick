using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MicroCHAP;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using ScsContentMigrator.Data;
using Sitecore.Diagnostics;
using SitecoreSidekick;

namespace ScsContentMigrator
{
	public class GetResources
	{
		internal static SignatureService ss;
		public static IItemData GetRemoteItemData(RemoteContentPullArgs args, string itemId)
		{
			try
			{
				string url = $"{args.server}/scs/cmcontenttreegetitem.scsvc";
				string parameters = args.GetSerializedData(itemId, false);
				string nonce = Guid.NewGuid().ToString();
				var sig = ss.CreateSignature(nonce, url,
					new[] { new SignatureFactor("payload", parameters), });
				WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
				wc.Headers["X-MC-MAC"] = sig.SignatureHash;
				wc.Headers["X-MC-Nonce"] = nonce;
				string yamlList = wc.UploadString(url, "POST",
					parameters);
				string yaml = JsonNetWrapper.DeserializeObject<List<string>>(yamlList).FirstOrDefault();
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
				string url = $"{args.server}/scs/cmcontenttree.scsvc";
				string parameters = $@"{{ ""id"": ""{itemId}"", ""database"": ""{args.database}""}}";
				string nonce = Guid.NewGuid().ToString();
				var sig = ss.CreateSignature(nonce, url,
					new[] {new SignatureFactor("payload", parameters),});
				WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
				wc.Headers["X-MC-MAC"] = sig.SignatureHash;
				wc.Headers["X-MC-Nonce"] = nonce;
				string response = wc.UploadString($"{args.server}/scs/cmcontenttree.scsvc", "POST",
					$@"{{ ""id"": ""{itemId}"", ""database"": ""{args.database}""}}");
				var node = JsonNetWrapper.DeserializeObject<CompareContentTreeNode>(response);
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
