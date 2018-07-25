using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class AddToExistingController
	{
		public readonly string ActionFormat;

		public AddToExistingController(string actionFormat = @"    public ActionResult _CONTROLLERACTION_()
        {
            //Your code goes here!
            return Content(""Magic"");
        }
	")
		{
			ActionFormat = actionFormat;
		}
		public void Process(SitecoreResourceManagerArgs args)
		{
			if (args.TargetControllerPath.IsNullOrEmpty() || args.ControllerAction.IsNullOrEmpty())
			{
				Log.Debug("Unable to add to existing controller due to the lack of _TARGETCONTROLLERPATH_ and/or _CONTROLLERACTION_");
				return;
			}

			if (!args.TargetControllerPath.EndsWith(".cs"))
			{
				Log.Debug("Unable to add to existing controller due to _TARGETCONTROLLERPATH_ not ending in .cs");
				return;
			}

			string text = System.IO.File.ReadAllText(args.TargetControllerPath);
			int index = text.LastIndexOf('}');
			if (index > -1)
				index--;
			index = text.LastIndexOf('}', index);
			string action = ReplaceAllTokens.ReplaceTokens(args.ActionFormat ?? ActionFormat, args, "Controller Code");
			text = text.Insert(index-1, action);
			Log.Debug($"Adding controller action to controller at {args.TargetControllerPath}");
			System.IO.File.WriteAllText(args.TargetControllerPath, text);
			args.EventLog.Add($"Added code to the existing controller {args.TargetControllerPath}");
		}
	}
}
