using System.Web.Mvc;
using SitecoreSidekick.Core;

namespace TargetNamespace
{
	class ScsAppNameController : ScsController
	{
		//The action name should match what the angular factory is calling, note that case sensitivity isn't an issue.
		[ActionName("AppCodeContentDemo.scsvc")]
		public ActionResult GetDemoContent(string content)
		{
			return Content(content + "SOME STUFF I ADDED SERVER SIDE");
		}
	}
}
