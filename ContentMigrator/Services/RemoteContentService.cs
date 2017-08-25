using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MicroCHAP;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using ScsContentMigrator.Data;
using ScsContentMigrator.Models;
using ScsContentMigrator.Security;
using ScsContentMigrator.Services.Interface;
using Sitecore.Diagnostics;
using SitecoreSidekick;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator.Services
{
	public class RemoteContentService : IRemoteContentService
	{
		private ISignatureService _ss;
		private readonly IScsRegistrationService _registration;

		public RemoteContentService()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
		}

		public RemoteContentService(ISignatureService signature)
		{
			_ss = signature;
		}
		public IItemData GetRemoteItemData(Guid id, string server)
		{
			string url = $"{server}/scs/cm/cmgetitemyaml.scsvc";
			string parameters = JsonNetWrapper.SerializeObject(id);
			string yaml = MakeRequest(url, parameters);
			return DeserializeYaml(yaml, id.ToString());
		}

		public ChildrenItemDataModel GetRemoteItemDataWithChildren(Guid id, string server)
		{
			string url = $"{server}/scs/cm/cmgetitemyamlwithchildren.scsvc";
			string parameters = JsonNetWrapper.SerializeObject(id);
			string json = MakeRequest(url, parameters);
			return JsonNetWrapper.DeserializeObject<ChildrenItemDataModel>(json);
		}
		private string MakeRequest(string url, string parameters)
		{
			string nonce = Guid.NewGuid().ToString();

			WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
			if (_ss == null)
			{
				_ss = new SignatureService(_registration.GetScsRegistration<ContentMigrationRegistration>().AuthenticationSecret);
				HmacServer = new ScsHmacServer(_ss, new UniqueChallengeStore());
			}
			var signature = _ss.CreateSignature(nonce, url, new[] { new SignatureFactor("payload", parameters) });

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
						throw new AccessViolationException("Remote server returned Forbidden. Make sure your shared secrets match.");
					}
					throw new Exception("Remote server didn't return a response", ex);

				}
				throw new Exception("Remote server didn't return a response", ex);

			}
			finally
			{
				ServicePointManager.SecurityProtocol = currentPolicy;
			}
		}

		protected virtual SecurityProtocolType SetSslCiphers()
		{
			return SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
		}

		public IItemData DeserializeYaml(string yaml, string itemId)
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
						Log.Error("Problem reading yaml from remote server", e, this);
					}
					if (itemData != null)
					{
						return itemData;
					}
				}
			}
			return null;
		}

		public CompareContentTreeNode GetContentTreeNode(RemoteContentTreeArgs args)
		{
			string url = $"{args.Server}/scs/cm/cmcontenttree.scsvc";
			string parameters = $@"{{ ""id"": ""{args.Id}"", ""database"": ""{args.Database}""}}";

			string response = MakeRequest(url, parameters);

			var node = JsonNetWrapper.DeserializeObject<CompareContentTreeNode>(response);

			if (!string.IsNullOrWhiteSpace(args.Id))
				node.SimpleCompare(args.Database, args.Id);

			return node;
		}

		public ScsHmacServer HmacServer { get; private set; }
	}
}

