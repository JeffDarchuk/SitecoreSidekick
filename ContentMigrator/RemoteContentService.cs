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
	public class RemoteContentService
	{
		// TODO: this is ugly AF. Fix this. This class should be named something more descriptive. It shouldn't be static. And it shouldn't have this property on it.
		internal static SignatureService SignatureService;

		public static IItemData GetRemoteItemData(RemoteContentPullArgs args, string itemId)
		{
			try
			{
				string url = $"{args.server}/scs/cmcontenttreegetitem.scsvc";
				string parameters = args.GetSerializedData(itemId, false);

				string yamlList = MakeRequest(url, parameters);
				string yaml = JsonNetWrapper.DeserializeObject<List<string>>(yamlList).FirstOrDefault();

				var resultItem = DeserializeYaml(yaml, itemId);
				if (resultItem == null)
				{
					return null;
				}

				resultItem.DatabaseName = args.database;

				return resultItem;
			}
			catch (Exception e)
			{
				Log.Error("Error getting remote item data for " + itemId, e, args);
			}

			return null;
		}

		// TODO: this looks awful similar to the method in ItemExtensions.cs
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
						Log.Error("Problem reading yaml from remote server", e, typeof(RemoteContentService));
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
			string url = $"{args.server}/scs/cmcontenttree.scsvc";
			string parameters = $@"{{ ""id"": ""{itemId}"", ""database"": ""{args.database}""}}";

			string response = MakeRequest(url, parameters);

			var node = JsonNetWrapper.DeserializeObject<CompareContentTreeNode>(response);

			if (!diff)
			{
				return node;
			}

			node.BuildDiff(args.database, itemId);

			return node;
		}

		public static IEnumerable<string> GetRemoteItemChildren(RemoteContentTreeArgs args, string itemId)
		{
			return GetRemoteItem(args, itemId, false).Nodes.Select(x => x.Id);
		}

		private static string MakeRequest(string url, string parameters)
		{
			string nonce = Guid.NewGuid().ToString();

			WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
			var signature = SignatureService.CreateSignature(nonce, url, new[] { new SignatureFactor("payload", parameters) });

			wc.Headers["X-MC-MAC"] = signature.SignatureHash;
			wc.Headers["X-MC-Nonce"] = nonce;

			var currentPolicy = ServicePointManager.SecurityProtocol;

			try
			{
				// .NET < 4.6.1 uses (insecure) SSL3 by default and does not enable TLS 1.2 for WebClient.
				ServicePointManager.SecurityProtocol = SetSslCiphers();

				return wc.UploadString(url, "POST", parameters);
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.ProtocolError)
				{
					var response = ex.Response as HttpWebResponse;
					if (response?.StatusCode == HttpStatusCode.Forbidden)
					{
						throw new InvalidOperationException("Remote server returned Forbidden. Make sure your shared secrets match.");
					}

					throw;
				}

				throw;
			}
			finally
			{
				ServicePointManager.SecurityProtocol = currentPolicy;
			}
		}

		// TODO: in future, once this class isn't static any longer, this should be protected virtual
		private static SecurityProtocolType SetSslCiphers()
		{
			return SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
		}
	}
}
