using Sitecore.Mvc.Extensions;
using Sitecore.Pipelines;
using SitecoreSidekick.Pipelines.HttpRequestBegin;
using SitecoreSidekick.Services.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace SitecoreSidekick.Core
{

	public abstract class ScsRegistration : IScsRegistration
	{
		public virtual bool IsReusable => true;
		public bool AdminOnly { get; }
		public List<string> Roles { get; }
		public List<string> Users { get; }
		private readonly IScsRegistrationService _scsRegistrationService;
		private readonly IAuthorizationService _authorizationService;
		private readonly IMainfestResourceStreamService _manifestResourceStreamService;

		protected ScsRegistration(string roles, string isAdmin, string users)
		{
			AdminOnly = isAdmin == "true";
			Roles = roles.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			Users = users.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			_scsRegistrationService = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_authorizationService = Bootstrap.Container.Resolve<IAuthorizationService>();
			_manifestResourceStreamService = Bootstrap.Container.Resolve<IMainfestResourceStreamService>();
		}

		public virtual void Process(PipelineArgs args)
		{
			_scsRegistrationService.RegisterSidekick(this);
			_scsRegistrationService.RegisterSidekick(Controller, this);
		}

		public bool ApplicableSidekick()
		{			
			if (_authorizationService.IsCurrentUserAdmin)
				return true;
			if (AdminOnly)
				return false;
			if (Roles.Count == 0)
				return true;
			return _authorizationService.IsCurrentUserInRole(Roles);
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
			string result = _manifestResourceStreamService.GetManifestResourceText(GetType(), ResourcesPath + "." + filename, () => "");
			return result;
		}
		public string CompileEmbeddedResource(string fileExtension)
		{
			StringBuilder sb = new StringBuilder();

			foreach (var resource in _manifestResourceStreamService.GetManifestResourceNames(GetType()).Where(x => x.EndsWith(fileExtension) && x.StartsWith(ResourcesPath)).Select(x => x.Substring(ResourcesPath.Length + 1)))
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
