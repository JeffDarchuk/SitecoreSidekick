using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using MicroCHAP;
using Microsoft.CSharp.RuntimeBinder;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using ScsContentMigrator.Data;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator
{
	public class ContentMigrationController : ScsController
	{
		private readonly ISitecoreAccessService _sitecore;
		private readonly IScsRegistrationService _registration;
		private readonly IContentMigrationManagerService _migrationManager;
		private readonly IRemoteContentService _remoteContent;
		public static YamlSerializationFormatter Formatter = new YamlSerializationFormatter(null, null);

		public ContentMigrationController()
		{
			_sitecore = Bootstrap.Container.Resolve<ISitecoreAccessService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_migrationManager = Bootstrap.Container.Resolve<IContentMigrationManagerService>();
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
		}

		protected ContentMigrationController(ISitecoreAccessService sitecore, IScsRegistrationService registration, IContentMigrationManagerService migrationManager, IRemoteContentService remoteContent)
		{
			_sitecore = sitecore;
			_registration = registration;
			_migrationManager = migrationManager;
			_remoteContent = remoteContent;
		}

		[MchapOrLoggedIn]
		[ActionName("cmgetitemyaml.scsvc")]
		public ActionResult GetItemYaml(string id)
		{
			Assert.ArgumentNotNullOrEmpty(id, "id");
			using (var stream = new MemoryStream())
			{
				Formatter.WriteSerializedItem(_sitecore.GetItemData(Guid.Parse(id)), stream);
				stream.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(stream))
				{
					return Content(reader.ReadToEnd());
				}
			}
			
		}

		[MchapOrLoggedIn]
		[ActionName("cmgetitemyamlwithchildren.scsvc")]
		public ActionResult GetItemYamlWithChildren(string id)
		{
			Assert.ArgumentNotNullOrEmpty(id, "id");
			using (var stream = new MemoryStream())
			{
				using (new SecurityDisabler())
				{
					IItemData item = _sitecore.GetItemData(Guid.Parse(id));
					Formatter.WriteSerializedItem(item, stream);
					stream.Seek(0, SeekOrigin.Begin);

					using (var reader = new StreamReader(stream))
					{
						return ScsJson(new ChildrenItemDataModel
						{
							Item = reader.ReadToEnd(),
							Children = item.GetChildren().Select(x => x.Id).ToList()
						});
					}
				}
			}
		}

		[ScsLoggedIn]
		[ActionName("cmstartoperation.scsvc")]
		public ActionResult StartOperation(PullItemModel data)
		{
			return Content(_migrationManager.StartContentMigration(data));
		}

		[MchapOrLoggedIn]
		[ActionName("cmcontenttree.scsvc")]
		public ActionResult ContentTree(ContentTreeModel data)
		{
			return ScsJson(GetContentTree(data));
		}

		[ScsLoggedIn]
		[ActionName("cmserverlist.scsvc")]
		public ActionResult GetServerList()
		{
			return ScsJson(_registration.GetScsRegistration<ContentMigrationRegistration>().ServerList);
		}
		[MchapOrLoggedIn]
		[ActionName("cmcontenttreegetitem.scsvc")]
		public ActionResult ItemYaml(ContentTreeModel data)
		{
			return ScsJson(GetItemYaml(data));
		}

		[MchapOrLoggedIn]
		[ActionName("cmitemdatachildren.scsvc")]
		public ActionResult GetItemDataWithChildren(string id)
		{
			return ScsJson(ItemDataWithChildren(id));
		}

		[ScsLoggedIn]
		[ActionName("cmopeartionstatus.scsvc")]
		public ActionResult Status(OperationStatusRequestModel data)
		{
			return ScsJson(_migrationManager.GetItemLogEntries(data.OperationId, data.LineNumber));
		}

		[ScsLoggedIn]
		[ActionName("cmopeartionlog.scsvc")]
		public ActionResult LogStatus(OperationStatusRequestModel data)
		{
			return ScsJson(_migrationManager.GetAuditLogEntries(data.OperationId, data.LineNumber));
		}

		[ScsLoggedIn]
		[ActionName("cmoperationlist.scsvc")]
		public ActionResult OperationList()
		{
			return ScsJson(GetOperationList());
		}

		[ScsLoggedIn]
		[ActionName("cmstopoperation.scsvc")]
		public ActionResult Stop(string operationId)
		{
			return ScsJson(_migrationManager.CancelContentMigration(operationId));
		}

		[ScsLoggedIn]
		[ActionName("cmapprovepreview.scsvc")]
		public ActionResult ApprovePreview(string operationId)
		{
			return ScsJson(StartPreviewAsPull(operationId));
		}

		[ScsLoggedIn]
		[ActionName("cmqueuelength.scsvc")]
		public ActionResult GetOperationQueueLength(string operationId)
		{
			return ScsJson(OperationQueueLength(operationId));
		}

		[ActionName("cmchecksum.scsvc")]
		public ActionResult ItemChecksum()
		{
			return ScsJson(ContentMigrationRegistration.GetChecksum(Request.QueryString["id"]));
		}

		[ActionName("cmbuilddiff.scsvc")]
		public ActionResult BuildDiff(DiffRequestModel model)
		{
			using (new SecurityDisabler())
			{
				CompareContentTreeNode ret = new CompareContentTreeNode(Factory.GetDatabase("master").GetItem(model.Id), false);
				ret.BuildDiff(model.Server);
				return ScsJson(ret);
			}
		}

		private object ItemDataWithChildren(string id)
		{
			var ret = new ChildrenItemDataModel();
			Guid guid = Guid.Parse(id);
			ret.Item = _sitecore.GetItem(guid).GetYaml();
			ret.Children = _sitecore.GetChildrenIds(guid);
			return ret;
		}

		private object OperationQueueLength(string operationId)
		{
			return _migrationManager.GetContentMigration(operationId)?.ItemsInQueueToInstall;
		}

		private object StartPreviewAsPull(string operationId)
		{
			_migrationManager.GetContentMigration(operationId).StartOperationFromPreview();
			return true;
		}

		private object GetOperationList()
		{
			return _migrationManager.GetAllContentMigrations().Select(x => x.Status).OrderBy(x => x.StartedTime).ToList();
		}

		private List<string> GetItemYaml(ContentTreeModel data)
		{
			Request.InputStream.Seek(0, SeekOrigin.Begin);
			string payload = new StreamReader(Request.InputStream).ReadToEnd();
			if (!_remoteContent.HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] { new SignatureFactor("payload", payload) }))
			{
				System.Web.HttpContext.Current.Response.StatusCode = 403;
				return null;
			}

			var db = Factory.GetDatabase(data.Database);
			using (new SecurityDisabler())
			{
				Item item = db.GetItem(data.Id ?? data.Ids.FirstOrDefault());

				return new List<string> { item.GetYaml() };
			}
		}

		/// <summary>
		/// returns the content tree level for the given id
		/// </summary>
		private CompareContentTreeNode GetContentTree(ContentTreeModel data)
		{
			try
			{
				if (data.Id == null)
				{
					data.Id = "";
				}
				var args = new RemoteContentTreeArgs(data.Id, data.Database, data.Server, data.Children);
				if (args.Server != null)
				{
					return _remoteContent.GetContentTreeNode(args);
				}
			}
			catch (RuntimeBinderException)
			{

			}
			Request.InputStream.Seek(0, SeekOrigin.Begin);
			string payload = new StreamReader(Request.InputStream).ReadToEnd();
			if (!_remoteContent.HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] { new SignatureFactor("payload", payload) }))
			{
				System.Web.HttpContext.Current.Response.StatusCode = 403;
				return null;
			}

			using (new SecurityDisabler())
			{
				if (data.Id == "")
					return ContentMigrationRegistration.Root;
				Database db = Factory.GetDatabase(data.Database);
				Item i = db.GetItem(new ID(data.Id));
				return new CompareContentTreeNode(i);
			}
		}
	}
}
