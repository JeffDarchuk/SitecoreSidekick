using MicroCHAP;
using Microsoft.CSharp.RuntimeBinder;
using Rainbow.Model;
using Sidekick.ContentMigrator.Args;
using Sidekick.ContentMigrator.Core;
using Sidekick.ContentMigrator.Core.Interface;
using Sidekick.ContentMigrator.Data;
using Sidekick.ContentMigrator.Models;
using Sidekick.ContentMigrator.Services.Interface;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Engines;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Install.Files;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Guid = System.Guid;

namespace Sidekick.ContentMigrator
{
	public class ContentMigrationController : ScsController
	{
		private readonly ISitecoreDataAccessService _sitecore;
		private readonly IScsRegistrationService _registration;
		private readonly IContentMigrationManagerService _migrationManager;
		private readonly IRemoteContentService _remoteContent;
		private readonly IYamlSerializationService _yamlSerializationService;
		private readonly IChecksumManager _checksumManager;

		public ContentMigrationController()
		{
			_sitecore = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_migrationManager = Bootstrap.Container.Resolve<IContentMigrationManagerService>();
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
			_yamlSerializationService = Bootstrap.Container.Resolve<IYamlSerializationService>();
			_checksumManager = Bootstrap.Container.Resolve<IChecksumManager>();
		}

		[MchapOrLoggedIn]
		[ActionName("cmgetitemyaml.scsvc")]
		public ActionResult GetItemYaml(string id)
		{
			Assert.ArgumentNotNullOrEmpty(id, "id");
			using (var stream = new MemoryStream())
			{
				_yamlSerializationService.WriteSerializedItem(_sitecore.GetItemData(Guid.Parse(id)), stream);
				stream.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(stream))
				{
					return Content(reader.ReadToEnd());
				}
			}

		}

		[MchapOrLoggedIn]
		[ActionName("cmgetitemyamlwithchildren.scsvc")]
		public ActionResult GetItemYamlWithChildren(RevisionModel data)
		{
			var guid = Guid.Parse(data.Id);

			using (new SecurityDisabler())
			{

				IItemData item = _sitecore.GetItemData(guid);
				var localRev = _sitecore.GetItemAndChildrenRevision(guid);
				List<Guid> GrandChildren = new List<Guid>();
				var items = new List<KeyValuePair<Guid, string>>();
				if (data.Rev == null || !data.Rev.ContainsKey(item.Id) || data.Rev[item.Id] != localRev[item.Id])
				{
					using (var stream = new MemoryStream())
					{
						_yamlSerializationService.WriteSerializedItem(item, stream);
						stream.Seek(0, SeekOrigin.Begin);

						using (var reader = new StreamReader(stream))
						{
							items.Add(new KeyValuePair<Guid, string>(item.Id, reader.ReadToEnd()));
						}
					}
				}
				else
				{
					items.Add(new KeyValuePair<Guid, string>(item.Id, null));
				}
				if (item.Path.StartsWith("/sitecore/media library/"))
				{
					GrandChildren.AddRange(_sitecore.GetChildren(item).Select(x => x.Id));
				}
				else
				{
					items.AddRange(_sitecore.GetChildren(item).Select(x =>
					{
						GrandChildren.AddRange(_sitecore.GetChildren(x).Select(c => c.Id));
						if (data.Rev != null && data.Rev.ContainsKey(x.Id) && localRev.ContainsKey(x.Id) && data.Rev[x.Id] == localRev[x.Id])
						{
							return new KeyValuePair<Guid, string>(x.Id, null);
						}
						using (var stream = new MemoryStream())
						{
							_yamlSerializationService.WriteSerializedItem(x, stream);
							stream.Seek(0, SeekOrigin.Begin);

							using (var reader = new StreamReader(stream))
							{
								return new KeyValuePair<Guid, string>(x.Id, reader.ReadToEnd());
							}
						}

					}));
				}
				return ScsJson(new ChildrenItemDataModel
				{
					Items = items,
					GrandChildren = GrandChildren
				});
			}
		}

		[MchapOrLoggedIn]
		[ActionName("cmstartoperation.scsvc")]
		public ActionResult StartOperation(PullItemModel data)
		{
			if (!string.IsNullOrWhiteSpace(Request.Headers["X-MC-MAC"]))
			{
				Request.InputStream.Seek(0, SeekOrigin.Begin);
				string payload = new StreamReader(Request.InputStream).ReadToEnd();
				if (!_remoteContent.HmacServer.ValidateRequest(new HttpRequestWrapper(System.Web.HttpContext.Current.Request), x => new[] { new SignatureFactor("payload", payload) }))
				{
					System.Web.HttpContext.Current.Response.StatusCode = 403;
					return null;
				}
			}
			return ScsJson(_migrationManager.StartContentMigration(data).Status);
		}

		[MchapOrLoggedIn]
		[ActionName("cmcontenttree.scsvc")]
		public ActionResult ContentTree(ContentTreeModel data)
		{
			return ScsJson(GetContentTree(data));
		}

		[LoggedIn]
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
		[ActionName("cmopeartionstatus.scsvc")]
		public ActionResult Status(OperationStatusRequestModel data)
		{
			return ScsJson(_migrationManager.GetItemLogEntries(data.OperationId, data.LineNumber));
		}

		[MchapOrLoggedIn]
		[ActionName("cmopeartionlog.scsvc")]
		public ActionResult LogStatus(OperationStatusRequestModel data)
		{
			return ScsJson(_migrationManager.GetAuditLogEntries(data.OperationId, data.LineNumber));
		}

		[MchapOrLoggedIn]
		[ActionName("cmoperationlist.scsvc")]
		public ActionResult OperationList()
		{
			return ScsJson(GetOperationList());
		}

		[MchapOrLoggedIn]
		[ActionName("cmstopoperation.scsvc")]
		public ActionResult Stop(string operationId)
		{
			return ScsJson(_migrationManager.CancelContentMigration(operationId));
		}

		[LoggedIn]
		[ActionName("cmapprovepreview.scsvc")]
		public ActionResult ApprovePreview(string operationId)
		{
			return ScsJson(StartPreviewAsPull(operationId));
		}

		[MchapOrLoggedIn]
		[ActionName("cmqueuelength.scsvc")]
		public ActionResult GetOperationQueueLength(string operationId)
		{
			return ScsJson(OperationQueueLength(operationId));
		}

		[ActionName("cmchecksum.scsvc")]
		public ActionResult ItemChecksum()
		{
			return ScsJson(_checksumManager.GetChecksum(Request.QueryString["id"]));
		}

		[ActionName("cmbuilddiff.scsvc")]
		public ActionResult BuildDiff(DiffRequestModel model)
		{
			using (new SecurityDisabler())
			{
				CompareContentTreeNode ret = new CompareContentTreeNode(_sitecore.GetItemData(model.Id), false);
				ret.BuildDiff(model.Server);
				return ScsJson(ret);
			}
		}
		[LoggedIn]
		[ActionName("cminstallpackage.scsvc")]
		public ActionResult InstallPackage()
		{
			StringBuilder ret = new StringBuilder();
			Stopwatch sw = new Stopwatch();
			sw.Start();
			try
			{
				using (new SecurityDisabler())
				using (new SyncOperationContext())
				{
					IProcessingContext context = new SimpleProcessingContext();
					IItemInstallerEvents events =
						new DefaultItemInstallerEvents(
							new BehaviourOptions(InstallMode.Overwrite, MergeMode.Undefined));
					context.AddAspect(events);
					IFileInstallerEvents events1 = new DefaultFileInstallerEvents(true);
					context.AddAspect(events1);

					Sitecore.Install.Installer installer = new Sitecore.Install.Installer();
					installer.InstallPackage(MainUtil.MapPath(@"C:\inetpub\wwwroot\demo1.local\App_Data\packages\bigtst.zip"), context);
				}
			}
			catch (Exception e)
			{
				ret.Append(e.ToString());
			}
			finally
			{
				ret.Append($"\n\n{sw.Elapsed.TotalSeconds}");
			}

			return Content(ret.ToString());
		}

		[LoggedIn]
		[ActionName("cmgetpresets.scsvc")]
		public ActionResult GetPresets(string server)
		{
			server = server.ToLower();
			return ScsJson(_registration.GetScsRegistration<ContentMigrationRegistration>().PresetList.Values.Where(
				x => !x.BlackList.Contains(server) &&
					(!x.WhiteList.Any() || x.WhiteList.Contains(server))));
		}

		[LoggedIn]
		[ActionName("cmrunpreset.scsvc")]
		public ActionResult RunPreset(PresetRunModel model)
		{
			var preset = _registration.GetScsRegistration<ContentMigrationRegistration>().PresetList[model.Name];
			preset.Server = model.Server;
			return StartOperation(preset);
		}

		[LoggedIn]
		[ActionName("cmdefaultoperationparameters.scsvc")]
		public ActionResult DefaultParameters(PresetRunModel model)
		{
			return ScsJson(new PullItemModel(_registration.GetScsRegistration<ContentMigrationRegistration>()));
		}

		[MchapOrLoggedIn]
		[ActionName("cmchecksumisgenerating.scsvc")]
		public ActionResult ChecksumIsGenerating(string server)
		{
			if (string.IsNullOrWhiteSpace(server))
				return ScsJson(new 
				{ 
					refreshing = ChecksumManager.ChecksumRefreshing,
					lastRefresh = string.Format("{0:0.00}", DateTime.Now.Subtract(ChecksumManager.LastTimeRan).TotalMinutes)
				});
			return ScsJson(_remoteContent.ChecksumIsGenerating(server));
		}

		[MchapOrLoggedIn]
		[ActionName("cmchecksumregenerate.scsvc")]
		public ActionResult ChecksumRegenerate(string server)
		{
			if (string.IsNullOrWhiteSpace(server))
			{
				_checksumManager.RegenerateChecksum();
				return ScsJson(true);
			}
				
			return ScsJson(_remoteContent.ChecksumRegenerate(server));
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

			return data.Id == "" ? ContentMigrationRegistration.Root : new CompareContentTreeNode(_sitecore.GetItemData(data.Id));
		}
	}
}
