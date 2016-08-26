using System.Collections.Concurrent;
using System.Web;
using Sitecore;
using Sitecore.Pipelines.HttpRequest;

namespace SitecoreSidekick.Pipelines.HttpRequestBegin
{
	public class IsAdmin : HttpRequestProcessor
	{
		private static ConcurrentDictionary<string,bool> _isAdmin = new ConcurrentDictionary<string, bool>();

		public static bool CurrentUser()
		{
			HttpCookie myCookie = HttpContext.Current.Request.Cookies["ASP.NET_SessionId"];
			if (myCookie != null && (_isAdmin.ContainsKey(myCookie.Value) && _isAdmin[myCookie.Value]))
				return true;
			return false;
		}

		public override void Process(HttpRequestArgs args)
		{
			HttpCookie myCookie = args.Context.Request.Cookies["ASP.NET_SessionId"];
			if (myCookie != null) _isAdmin[myCookie.Value] = Context.User.IsAdministrator;
		}
	}
}
