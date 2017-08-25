using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Mvc.Extensions;
using Sitecore.Pipelines;
using SitecoreSidekick.Handlers;
using SitecoreSidekick.Pipelines.HttpRequestBegin;
using SitecoreSidekick.Services;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace SitecoreSidekick.Core
{

	public abstract class ScsRegistration : IScsRegistration
	{
		public virtual bool IsReusable => true;
		public bool AdminOnly { get; }
		public List<string> Roles { get; }
		public List<string> Users { get; }
		public IScsRegistrationService Registration { get; }
		protected ScsRegistration(string roles, string isAdmin, string users)
		{
			AdminOnly = isAdmin == "true";
			Roles = roles.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			Users = users.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			Registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
		}
		protected ScsRegistration(string roles, string isAdmin, string users, IScsRegistrationService registration)
		{
			AdminOnly = isAdmin == "true";
			Roles = roles.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			Users = users.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			Registration = registration;
		}
		public virtual void Process(PipelineArgs args)
		{
			Registration.RegisterSidekick(this);
			Registration.RegisterSidekick(Controller, this);
		}

		public bool ApplicableSidekick()
		{
			bool admin = IsAdmin.CurrentUserAdmin();
			if (admin)
				return true;
			if (AdminOnly)
				return false;
			if (Roles.Count == 0)
				return true;
			return IsAdmin.CurrentUserInRoleList(Roles);
		}

		public virtual void RegisterRoutes()
		{
			var routes = RouteTable.Routes;
			using (routes.GetWriteLock())
			{
				routes.MapRoute(Name + "services", "scs/" + Identifier + "/{action}", new { controller = $"{Controller.Namespace}.{Controller.Name}, {Controller.Assembly.GetName().Name}", action = "*" });
				routes.MapRoute(Name + "resources", "scs/" + Identifier + "/resources/{filename}", new { controller = $"{Controller.Namespace}.{Controller.Name}, {Controller.Assembly.GetName().Name}", action = "resources" });
			}
		}

		public string GetResource(string filename)
		{
			filename = filename.ToLowerInvariant();
			string result = "";
			using (var stream = GetType().Assembly.GetManifestResourceStream(ResourcesPath + "." + filename))
			{
				if (stream != null)
				{
					using (var reader = new StreamReader(stream))
					{
						result = reader.ReadToEnd();
					}
				}
			}
			return result;
		}
		public string CompileEmbeddedResource(string fileExtension)
		{
			StringBuilder sb = new StringBuilder();

			foreach (var resource in GetType().Assembly.GetManifestResourceNames().Where(x => x.EndsWith(fileExtension) && x.StartsWith(ResourcesPath)).Select(x => x.Substring(ResourcesPath.Length + 1)))
			{
				if (!resource.Equals("scsangular.js"))
				{
					sb.Append(GetResource(resource));
				}
			}

			return sb.ToString();

		}
		public abstract string Identifier { get; }
		public abstract string Directive { get; }
		public abstract Type Controller { get; }
		public abstract NameValueCollection DirectiveAttributes { get; set; }
		public abstract string Icon { get; }
		public abstract string Name { get; }
		public abstract string CssStyle { get; }
		public abstract string ResourcesPath { get; }
	}
}
