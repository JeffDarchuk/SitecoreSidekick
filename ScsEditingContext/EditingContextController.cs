using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.SecurityModel;
using SitecoreSidekick;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Core;
using SitecoreSidekick.Pipelines.HttpRequestBegin;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;
using System.Web.Configuration;
using System.Configuration;
using System.Text.RegularExpressions;
using Sitecore.Diagnostics;

namespace ScsEditingContext
{
	public class EditingContextController: ScsController
	{
		private readonly IScsRegistrationService _registration;
		private readonly ISitecoreDataAccessService _sitecore;
        private readonly SessionStateSection SessionSettings = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");

        public EditingContextController()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_sitecore = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		}

		protected EditingContextController(IScsRegistrationService registration)
		{
			_registration = registration;
		}
		[ScsLoggedIn]
		[ActionName("getcommonlocations.json")]
		public ActionResult CommonLocations()
		{
			return ScsJson(GetCommonLocations());
		}

		[ScsLoggedIn]
		[ActionName("getrelated.json")]
		public ActionResult RelatedItems()
		{
			return ScsJson(GetReferences());
		}

		[ScsLoggedIn]
		[ActionName("getreferrers.json")]
		public ActionResult ReferrerItems()
		{
			return ScsJson(GetReferrers());
		}

		private object GetReferrers()
		{
			string key = Request.Cookies[SessionSettings.CookieName]?.Value ?? "";
			if (EditingContextRegistration.Referrers.ContainsKey(key))
				return EditingContextRegistration.Referrers[key];
			return new List<TypeContentTreeNode>();
		}

		private object GetReferences()
		{
			string key = Request.Cookies[SessionSettings.CookieName]?.Value ?? "";
			if (EditingContextRegistration.Related.ContainsKey(key))
				return EditingContextRegistration.Related[key];
			return new List<TypeContentTreeNode>();
		}


		private dynamic GetCommonLocations()
		{
			EditingContextRegistration ec = _registration.GetScsRegistration<EditingContextRegistration>();
			dynamic ret = new ExpandoObject();
			ret.editor = ec.EditorLocations;
			if (IsAdmin.CurrentUserAdmin())
			{
				ret.core = ec.CoreLocations;
				ret.master = ec.MasterLocations;

			}
			return ret;
		}
	}
}
