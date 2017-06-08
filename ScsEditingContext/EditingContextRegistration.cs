using System;
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
using SitecoreSidekick;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Core;
using SitecoreSidekick.Handlers;
using SitecoreSidekick.Pipelines.HttpRequestBegin;

namespace ScsEditingContext
{
	public class EditingContextRegistration : ScsRegistration
	{
		public List<dynamic> CoreLocations { get; } = new List<dynamic>();
		public List<dynamic> MasterLocations { get; } = new List<dynamic>();
		public List<dynamic> EditorLocations { get; } = new List<dynamic>();
		public override string Directive => "ecmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "ScsEditingContext.Resources";
		public override Type Controller => typeof(EditingContextController);
		public override string Icon => "/scs/ec/resources/ec.png";
		public override string Name => "Editing Context";
		public override string CssStyle => "width:100%;min-width:600px";
		public override string Identifier => "ec";

		public Database Core{ get; } = Factory.GetDatabase("core");
		public Database Master { get; } = Factory.GetDatabase("master");
		internal static ConcurrentDictionary<string, List<TypeContentTreeNode>> Related { get; }
		internal static ConcurrentDictionary<string, List<TypeContentTreeNode>> Referrers { get; }


		static EditingContextRegistration()
		{
			Related = new ConcurrentDictionary<string, List<TypeContentTreeNode>>();
			Referrers = new ConcurrentDictionary<string, List<TypeContentTreeNode>>();
		}

		public EditingContextRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
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
