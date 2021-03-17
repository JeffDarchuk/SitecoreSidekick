using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using Sidekick.Core.Handlers;
using Sidekick.Core.Models;
using Sidekick.Core.Services;
using Sidekick.Core.Services.Interface;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using Sitecore;
using System.Threading.Tasks;

namespace Sidekick.Core.Pipelines.Initialize
{

	public class InitializeSidekick
	{
		public const string DesktopMenuRight = "{10148DC7-DCA6-4ACA-AC90-46FBF59A1D1F}";
		public const string SidekickButton = "{3F324240-7645-4C70-A337-64A9D4A91549}";
		public const string ActionTemplate = "{F58958D2-555B-4F56-946C-589E8866880C}";
		private readonly IScsRegistrationService _registration;
		public string DisableItemGeneration { get; set; }
		public InitializeSidekick()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
		}

		public InitializeSidekick(IScsRegistrationService registration)
		{
			_registration = registration;
		}
		public void Process(PipelineArgs args)
		{

			Assert.ArgumentNotNull(args, "args");
			Task.Run(() =>
			{
				if (DisableItemGeneration?.ToLower() != "true")
				{
					EnsureDesktopButton();
				}
				if (Factory.GetDatabase("master", false) != null)
				{
					ScsMainRegistration maintmp = new ScsMainRegistration("", "", "");
					_registration.RegisterSidekick(maintmp);
					_registration.RegisterSidekick(maintmp.Controller, maintmp);
					var pipeline = CorePipelineFactory.GetPipeline("scsRegister", string.Empty);
					pipeline.Run(new PipelineArgs());
					RegisterRoutes("scs");
				}
			});
		}

		public static void EnsureDesktopButton()
		{
			using (new SecurityDisabler())
			{
				var master = Factory.GetDatabase("master", false);
				if (master == null)
					return;
				var core = Factory.GetDatabase("core", false);
				if (core == null)
					return;

				Item sk = core.GetItem(new ID(SidekickButton));
				if (sk != null && EnsureButtonItem(sk))
					return;
				Item right = core.GetItem(new ID(DesktopMenuRight));
				if (right == null)
					return;
				if (sk == null)
				{
					sk = ItemManager.CreateItem("Sitecore Sidekick", right, new ID(ActionTemplate), new ID(SidekickButton));
				}
				using (new EditContext(sk))
				{
					sk[FieldIDs.DisplayName] = "Sitecore Sidekick";
					if (IsGreaterThanSc7())
					{
						sk[FieldIDs.Icon] = "office/32x32/sword.png";
						sk["Icon"] = "office/32x32/sword.png";
					}
					else
					{
						sk[FieldIDs.Icon] = "Network/32x32/knight.png";
						sk["Icon"] = "Network/32x32/knight.png";
					}

					sk["Display name"] = "Sitecore Sidekick";
					sk["Message"] = "scs:open";
					sk["Tool tip"] = "Open the Sitecore Sidekick";
					if (sk[FieldIDs.Security].IsNullOrEmpty())
					{
						sk[FieldIDs.Security] = @"ar|sitecore\Sitecore Client Authoring|pd|+item:read|pe|+item:read|";
					}
				}
			}
		}

		public static bool EnsureButtonItem(Item sk)
		{
			return
				sk[FieldIDs.DisplayName] == "Sitecore Sidekick" &&
				(IsGreaterThanSc7()
					? sk[FieldIDs.Icon] == "office/32x32/sword.png"
					: sk[FieldIDs.Icon] == "Network/32x32/knight.png") &&
				(IsGreaterThanSc7()
					? sk["Icon"] == "office/32x32/sword.png"
					: sk["Icon"] == "Network/32x32/knight.png") &&
				sk["Display name"] == "Sitecore Sidekick" &&
				sk["Message"] == "scs:open" &&
				sk["Tool tip"] == "Open the Sitecore Sidekick" &&
				!sk[FieldIDs.Security].IsNullOrEmpty();
		}

		private static bool IsGreaterThanSc7()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(HttpRuntime.AppDomainAppPath + "/sitecore/shell/sitecore.version.xml");
			var selectSingleNode = doc.SelectSingleNode("/information/version/major");
			return selectSingleNode != null && (selectSingleNode.InnerText != "7");
		}

		public void RegisterRoutes(string route)
		{
			ScsModelBinder.Default = ModelBinders.Binders.DefaultBinder;
			ModelBinders.Binders.DefaultBinder = new ScsModelBinder();
			var routes = RouteTable.Routes;
			using (routes.GetWriteLock())
			{
				routes.MapRoute("scs", "scs/platform/{action}", new { controller = "Sidekick.Core.Handlers.ScsMainController, Sidekick.Core", action = "scs" });
				routes.MapRoute("scsresources", "scs/platform/{action}/{filename}", new { controller = $"Sidekick.Core.Handlers.ScsMainController, Sidekick.Core", action = "resources" });
			}
			foreach (var sidekick in _registration.GetAllSidekicks().Where(x => x.Name != "Sitecore Sidekick"))
			{
				sidekick.RegisterRoutes();
			}

		}
	}
}
