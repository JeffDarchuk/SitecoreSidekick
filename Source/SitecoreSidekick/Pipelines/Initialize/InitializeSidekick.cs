using System.Web.Routing;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using SitecoreSidekick.Handlers;

namespace SitecoreSidekick.Pipelines.Initialize
{

	public class InitializeSidekick
	{
		public const string DesktopMenuRight = "{10148DC7-DCA6-4ACA-AC90-46FBF59A1D1F}";
		public const string SidekickButton = "{3F324240-7645-4C70-A337-64A9D4A91549}";
		public const string ActionTemplate = "{F58958D2-555B-4F56-946C-589E8866880C}";
		public void Process(PipelineArgs args)
		{

			Assert.ArgumentNotNull(args, "args");
			EnsureDesktopButton();
			if (Factory.GetDatabase("master", false) != null)
			{
				RegisterRoutes("scs");
				var pipeline = CorePipelineFactory.GetPipeline("scsRegister", string.Empty);
				pipeline.Run(new PipelineArgs());
			}
		}

		public static void EnsureDesktopButton()
		{
			var master = Factory.GetDatabase("master", false);
			if (master == null)
				return;
			var core = Factory.GetDatabase("core", false);
			if (core == null)
				return;

			Item sk = core.DataManager.DataEngine.GetItem(new ID(SidekickButton), Language.DefaultLanguage, Version.Latest);
			if (sk != null)
				return;
			Item right = core.DataManager.DataEngine.GetItem(new ID(DesktopMenuRight), Language.DefaultLanguage, Version.Latest);
			if (right == null)
				return;
			using (new SecurityDisabler())
			{
				sk = ItemManager.CreateItem("Sitecore Sidekick", right, new ID(ActionTemplate), new ID(SidekickButton));
				using (new EditContext(sk))
				{
					sk["Display name"] = "Sitecore Sidekick";
					sk["Icon"] = "office/32x32/sword.png";
					sk["Message"] = "scs:open";
					sk["Tool tip"] = "Open the Sitecore Sidekick";
				}
			}

		}

		public static void RegisterRoutes(string route)
		{
			var routes = RouteTable.Routes;
			var handler = new ScsHandler("", "", "");
			using (routes.GetWriteLock())
			{
				var filenameRoute = new Route(route + "/{filename}", handler)
				{
					Defaults = new RouteValueDictionary(new { controller = "SCSHandler", action = "ProcessRequest" }),
					Constraints = new RouteValueDictionary(new { controller = "SCSHandler", action = "ProcessRequest" })
				};
				routes.Add("SCSHandlerFilenameRoute", filenameRoute);
			}
		}
	}
}
