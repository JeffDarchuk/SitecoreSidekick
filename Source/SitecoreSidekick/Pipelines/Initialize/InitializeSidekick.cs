using System.Web.Routing;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using SitecoreSidekick.Handlers;

namespace SitecoreSidekick.Pipelines.Initialize
{

	public class InitializeSidekick
	{

		public void Process(PipelineArgs args)
		{

			Assert.ArgumentNotNull(args, "args");
			if (Factory.GetDatabase("master", false) != null)
			{
				RegisterRoutes("scs");
				var pipeline = CorePipelineFactory.GetPipeline("scsRegister", string.Empty);
				pipeline.Run(new PipelineArgs());
			}
		}


		public static void RegisterRoutes(string route)
		{
			var routes = RouteTable.Routes;
			var handler = new ScsHandler();
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
