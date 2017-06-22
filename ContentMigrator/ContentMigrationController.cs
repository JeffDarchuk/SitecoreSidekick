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
using ScsContentMigrator.Args;
using ScsContentMigrator.Data;
using ScsContentMigrator.Models;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick;
using SitecoreSidekick.Core;

namespace ScsContentMigrator
{
	public class ContentMigrationController : ScsController
	{
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
			return ScsJson(GetScsRegistration<ContentMigrationRegistration>().ServerList);
		}
		[MchapOrLoggedIn]
		[ActionName("cmcontenttreegetitem.scsvc")]
		public ActionResult ItemYaml(ContentTreeModel data)
		{
			return ScsJson(GetItemYaml(data));
		}

		[ScsLoggedIn]
		[ActionName("cmcontenttreepullitem.scsvc")]
		public ActionResult PullItemYaml(PullItemModel data)
		{
			return ScsJson(PullItem(data));
		}

		[ScsLoggedIn]
		[ActionName("cmopeartionstatus.scsvc")]
		public ActionResult Status(OperationStatusRequestModel data)
		{
			return ScsJson(GetOperationStatus(data));
		}

		[ScsLoggedIn]
		[ActionName("cmopeartionlog.scsvc")]
		public ActionResult LogStatus(OperationStatusRequestModel data)
		{
			return ScsJson(GetOperationLog(data));
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
			return ScsJson(StopOperation(operationId));
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

		private object OperationQueueLength(string operationId)
		{
			var operation = RemoteContentPuller.GetOperation(operationId);
			if (operation != null)
				return operation.QueuedItems();
			return null;
		}

		private object StartPreviewAsPull(string operationId)
		{
			RemoteContentPuller.GetOperation(operationId).RunPreviewAsFullOperation();
			return true;
		}

		private object StopOperation(string operationId)
		{
			return RemoteContentPuller.StopOperation(operationId);
		}

		private object GetOperationList()
		{
			return RemoteContentPuller.GetRunningOperations().OrderBy(x =>
			{
				DateTime tmp;
				if (DateTime.TryParseExact(x.FinishedTime, "MMM d h:mm tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out tmp))
				{
					return tmp;
				}

				return DateTime.Now;
			}).ToList();
		}

		private object GetOperationStatus(OperationStatusRequestModel data)
		{
			return ((IEnumerable<object>)RemoteContentPuller.OperationStatus(data.OperationId, data.LineNumber)).ToList();
		}

		private object GetOperationLog(OperationStatusRequestModel data)
		{
			return ((IEnumerable<object>)RemoteContentPuller.OperationLog(data.OperationId, data.LineNumber)).ToList();
		}

		private object PullItem(PullItemModel data)
		{
			try
			{
				var args = new RemoteContentPullArgs(data);
				if (args.Server != null)
				{
					return GetScsRegistration<ContentMigrationRegistration>().Puller.PullContentItem(args);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Problem pulling item from remote server", ex, this);
				return new { Error = "Problem pulling item from remote server\n\n" + ex };
			}
			return true;
		}

		private List<string> GetItemYaml(ContentTreeModel data)
		{
			Request.InputStream.Seek(0, SeekOrigin.Begin);
			string payload = new StreamReader(Request.InputStream).ReadToEnd();
			if (!GetScsRegistration<ContentMigrationRegistration>().HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] { new SignatureFactor("payload", payload) }))
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
					return RemoteContentService.GetRemoteItem(args, args.Id, true);
				}
			}
			catch (RuntimeBinderException)
			{

			}
			Request.InputStream.Seek(0, SeekOrigin.Begin);
			string payload = new StreamReader(Request.InputStream).ReadToEnd();
			if (!GetScsRegistration<ContentMigrationRegistration>().HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] { new SignatureFactor("payload", payload) }))
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
