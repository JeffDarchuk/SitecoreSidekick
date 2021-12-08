using MicroCHAP;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using Sidekick.ContentMigrator.Args;
using Sidekick.ContentMigrator.Data;
using Sidekick.ContentMigrator.Models;
using Sidekick.ContentMigrator.Security;
using Sidekick.ContentMigrator.Services.Interface;
using Sitecore.Diagnostics;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Sidekick.ContentMigrator.Services
{
	public class RemoteContentService : IRemoteContentService
	{
		private ISignatureService _ss;
		private readonly IScsRegistrationService _registration;
		private readonly IJsonSerializationService _jsonSerializationService;
		private HmacServer _hmacServer;
		public HmacServer HmacServer
		{
			get
			{
				if (_ss == null)
				{
					_ss = new SignatureService(_registration.GetScsRegistration<ContentMigrationRegistration>().AuthenticationSecret);

				}
				return _hmacServer ?? (_hmacServer = new HmacServer(_ss, new UniqueChallengeStore()));
			}
			private set { _hmacServer = value; }
		}
		public RemoteContentService(IScsRegistrationService registration)
		{
			_registration = registration;
			_ss = Bootstrap.Container.Resolve<ISignatureService>(registration.GetScsRegistration<ContentMigrationRegistration>().AuthenticationSecret);
			_jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();
		}

		public RemoteContentService(ISignatureService signature, IScsRegistrationService registration)
		{
			_ss = signature;
			_registration = registration;
		}
		public object ChecksumIsGenerating(string server)
		{
			string url = $"{server}/scs/cm/cmchecksumisgenerating.scsvc";
			string json = MakeRequest(url, "");
			return _jsonSerializationService.DeserializeObject<object>(json);
		}
		public bool ChecksumRegenerate(string server)
		{
			string url = $"{server}/scs/cm/cmchecksumregenerate.scsvc";
			string json = MakeRequest(url, "");
			return _jsonSerializationService.DeserializeObject<bool>(json);
		}

		public IItemData GetRemoteItemData(Guid id, string server)
		{
			string url = $"{server}/scs/cm/cmgetitemyaml.scsvc";
			string parameters = _jsonSerializationService.SerializeObject(id);
			string yaml = MakeRequest(url, parameters);
			return DeserializeYaml(yaml, id);
		}

		public ChildrenItemDataModel GetRemoteItemDataWithChildren(Guid id, string server, Dictionary<Guid, string> rev = null)
		{
			string url = $"{server}/scs/cm/cmgetitemyamlwithchildren.scsvc";
			string parameters = _jsonSerializationService.SerializeObject(new {id, rev});
			string json = MakeRequest(url, parameters);
			return _jsonSerializationService.DeserializeObject<ChildrenItemDataModel>(json);
		}
		private string MakeRequest(string url, string parameters)
		{
			string nonce = Guid.NewGuid().ToString();

			WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
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

		public IItemData DeserializeYaml(string yaml, Guid itemId)
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
						itemData = formatter.ReadSerializedItem(ms, itemId.ToString());
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

			var node = _jsonSerializationService.DeserializeObject<CompareContentTreeNode>(response);

			if (!string.IsNullOrWhiteSpace(args.Id))
				node.SimpleCompare(args.Database, args.Id);

			return node;
		}

	}
}

