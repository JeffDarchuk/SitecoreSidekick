using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Handlers;
using SitecoreSidekick.Pipelines.HttpRequestBegin;

namespace ScsEditingContext
{
	public class EditingContextHandler : ScsHandler
	{
		public List<dynamic> CoreLocations = new List<dynamic>();
		public List<dynamic> MasterLocations = new List<dynamic>();
		public List<dynamic> EditorLocations = new List<dynamic>();
		public override string Directive { get; set; } = "ecmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "ScsEditingContext.Resources";
		public override string Icon => "/scs/ec.png";
		public override string Name => "Editing Context";
		public override string CssStyle => "width:100%;min-width:600px";

		public static Database Core = Factory.GetDatabase("core");
		public static Database Master = Factory.GetDatabase("master");
		internal static ConcurrentDictionary<string, List<TypeContentTreeNode>> Related { get; }
		internal static ConcurrentDictionary<string, List<TypeContentTreeNode>> Referrers { get; }
		public override void RegisterRoutes()
		{
			var routes = RouteTable.Routes;
			using (routes.GetWriteLock())
			{
				routes.MapRoute(Name, "scs/ec/{action}", new { controller = $"{GetType().Namespace}.{GetType().Name}, {GetType().Assembly.GetName().Name}", action = "ec" });
				routes.MapRoute(Name + "resources", "scs/ec/{action}/{filename}", new {controller = $"{GetType().Namespace}.{GetType().Name}, {GetType().Assembly.GetName().Name}", action = "resources"});
			}
		}

		static EditingContextHandler()
		{
			Related = new ConcurrentDictionary<string, List<TypeContentTreeNode>>();
			Referrers = new ConcurrentDictionary<string, List<TypeContentTreeNode>>();
		}

		public EditingContextHandler()
		{
		}

		public EditingContextHandler(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}
		[ActionName("blaer.json")]
		public ActionResult Blaer()
		{
			return Content("FLERFLER");
		}
		public ActionResult Resources(string filename)
		{
			return Content("RESOURCE "+filename);
		}
		public override ActionResult ProcessRequest(HttpContextBase context, string filename, dynamic data)
		{
			if (filename == "ecgetcommonlocations.json")
				ReturnJson(context, GetCommonLocations());
			else if (filename == "ecgetitemhistory.json")
				ReturnJson(context, GetItemHistory(context));
			else if (filename == "ecgetrelated.json")
				ReturnJson(context, GetReferences(context));
			else if (filename == "ecgetreferrers.json")
				ReturnJson(context, GetReferrers(context));
			else 
				ProcessResourceRequest(context, filename, data);
			return null;
		}

		private object GetReferrers(HttpContextBase context)
		{
			string key = context.Request.Cookies["ASP.NET_SessionId"]?.Value ?? "";
			if (Referrers.ContainsKey(key))
				return Referrers[key];
			return new List<TypeContentTreeNode>();
		}

		private object GetReferences(HttpContextBase context)
		{
			string key = context.Request.Cookies["ASP.NET_SessionId"]?.Value ?? "";
			if (Related.ContainsKey(key))
				return Related[key];
			return new List<TypeContentTreeNode>();
		}

		private dynamic GetItemHistory(HttpContextBase context)
		{
			dynamic ret = new ExpandoObject();
			HttpCookie authCookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];
			if (authCookie != null)
			{
				FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
				if (ticket != null)
				{
					string u = ticket.Name;
					var httpCookie = context.Request.Cookies["scseditorcontext"+u];
					if (httpCookie?.Value != null)
						using (new SecurityDisabler())
						{
							var urlDecode = HttpUtility.UrlDecode(httpCookie.Value);
							if (urlDecode != null)
								ret.items = urlDecode.Split(',').Select(FindItem).Where(x=>x != null);
						}
				}
			}
			return ret;
		}

		private dynamic FindItem(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				return null;
			dynamic ret = new ExpandoObject();
			string[] parts = key.Split('|');
			if (parts.Length != 4)
				return null;
			ret.Icon = parts[3];
			ret.DisplayName = parts[2];
			ret.DatabaseName = parts[1];
			ret.Id = parts[0];
			return ret;
		}

		private dynamic GetCommonLocations()
		{
			dynamic ret = new ExpandoObject();
			ret.editor = EditorLocations;
			if (IsAdmin.CurrentUserAdmin())
			{
				ret.core = CoreLocations;
				ret.master = MasterLocations;
				
			}
			return ret;
		}
		public void AddCoreLocation(XmlNode arg)
		{
			CoreLocations.Add(GetLocationFromXml(arg, Core));
		}

		public void AddMasterLocation(XmlNode arg)
		{
			MasterLocations.Add(GetLocationFromXml(arg, Master));
		}

		public void AddEditorLocation(XmlNode arg)
		{
			EditorLocations.Add(GetLocationFromXml(arg, Master));
		}

		public dynamic GetLocationFromXml(XmlNode arg, Database db)
		{
			var node = new ContentTreeNode(db.DataManager.DataEngine.GetItem(new ID(arg.Attributes?["id"]?.InnerText), LanguageManager.DefaultLanguage, Version.Latest));
			dynamic location = new ExpandoObject();
			location.item = node;
			location.description = arg.Attributes?["description"]?.InnerText;
			return location;
		}
	}
}
