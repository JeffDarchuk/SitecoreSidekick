using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using SitecoreSidekick;

namespace ScsContentMigrator
{
	public class MchapOrLoggedInAttribute : ScsLoggedInAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if (string.IsNullOrWhiteSpace(filterContext.RequestContext.HttpContext.Request.Headers["X-MC-MAC"]))
			{
				base.OnActionExecuting(filterContext);
			}
		}
	}
}
