using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sidekick.Core
{
	public class LoggedInAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if (Sitecore.Context.User.IsAuthenticated) return;
			string ticket = Sitecore.Web.Authentication.TicketManager.GetCurrentTicketId();

			if (!string.IsNullOrWhiteSpace(ticket))
			{
				Sitecore.Web.Authentication.TicketManager.Relogin(ticket);
			}
			if (Sitecore.Context.User.IsAuthenticated) return;
			filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
			filterContext.Result = new HttpUnauthorizedResult("Access denied.");
		}
	}
}
