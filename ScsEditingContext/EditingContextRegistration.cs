using Sidekick.Core;
using Sidekick.Core.ContentTree;
using Sidekick.EditingContext.Services.Interface;
using Sitecore.Configuration;
using Sitecore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Xml;

namespace Sidekick.EditingContext
{
	public class EditingContextRegistration : ScsRegistration
	{
		private readonly ISitecoreDataAccessService _sitecoreDataAccessService;

		public List<dynamic> CoreLocations { get; } = new List<dynamic>();
		public List<dynamic> MasterLocations { get; } = new List<dynamic>();
		public List<dynamic> EditorLocations { get; } = new List<dynamic>();
		public override string Directive => "ecmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "Sidekick.EditingContext.Resources";
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
			_sitecoreDataAccessService = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		}
		public void AddCoreLocation(XmlNode arg)
		{
			var item = GetLocationFromXml(arg, Core);
			if (item != null)
				CoreLocations.Add(item);
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
			var item = _sitecoreDataAccessService.GetLatestItemData(arg.Attributes?["id"]?.InnerText, db);
			if (item == null) return null;
			var node = new ContentTreeNode(item);
			dynamic location = new ExpandoObject();
			location.item = node;
			location.description = arg.Attributes?["description"]?.InnerText;
			return location;
		}

	}
}
