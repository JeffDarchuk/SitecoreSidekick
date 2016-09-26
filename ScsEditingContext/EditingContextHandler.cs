using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Xml;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Handlers;
using SitecoreSidekick.Pipelines.HttpRequestBegin;

namespace ScsEditingContext
{
	public class EditingContextHandler : ScsHttpHandler
	{
		public List<dynamic> CoreLocations = new List<dynamic>();
		public List<dynamic> MasterLocations = new List<dynamic>();
		public List<dynamic> EditorLocations = new List<dynamic>();
		public override string Directive { get; set; } = "ecmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "ScsEditingContext.Resources";
		public override string Icon => "/scs/ec.png";
		public override string Name => "Editing Context";
		public override string CssStyle => "width:600px";

		public static Database Core = Factory.GetDatabase("core");
		public static Database Master = Factory.GetDatabase("master");
		public EditingContextHandler(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}
		public override void ProcessRequest(HttpContextBase context)
		{
			string file = GetFile(context);

			if (file == "ecgetcommonlocations.json")
				ReturnJson(context, GetCommonLocations());
			else if (file == "ecgetitemhistory.json")
				ReturnJson(context, GetItemHistory());
			else 
				ProcessResourceRequest(context);
		}

		private dynamic GetItemHistory()
		{
			dynamic ret = new ExpandoObject();
			HttpCookie authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
			if (authCookie != null)
			{
				FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
				if (ticket != null)
				{
					string u = ticket.Name;
					var httpCookie = HttpContext.Current.Request.Cookies["scseditorcontext"+u];
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
			var node = new ContentTreeNode(db.DataManager.DataEngine.GetItem(new ID(arg.Attributes?["id"]?.InnerText), LanguageManager.DefaultLanguage, Sitecore.Data.Version.Latest));
			dynamic location = new ExpandoObject();
			location.item = node;
			location.description = arg.Attributes?["description"]?.InnerText;
			return location;
		}
	}
}
