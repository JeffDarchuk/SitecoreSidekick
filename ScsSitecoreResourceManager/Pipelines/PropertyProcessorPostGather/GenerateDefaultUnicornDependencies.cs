using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sidekick.SitecoreResourceManager.Pipelines.SitecoreResourceManager;

namespace Sidekick.SitecoreResourceManager.Pipelines.PropertyProcessorPostGather
{
	public class GenerateDefaultUnicornDependencies
	{
		public void Process(SitecoreResourceManagerArgs args)
		{
			args["_DEFAULTUNICORNDEPENDENCIES_"] = (args.Layer == "Feature" ? $"{args.Prefix}.Foundation.*" : (args.Layer == "Project") ? $"{args.Prefix}.Foundation.*,{args.Prefix}.Feature.*" : "");
		}
	}
}
