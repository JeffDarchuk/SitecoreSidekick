using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using ScsContactSearch.Services;
using SitecoreSidekick.Core;
using SitecoreSidekick.Shared.IoC;

namespace ScsContactSearch
{
	class ScsContactSearchController : ScsController
	{
		
		private IContactAccessService _contact;
		public ScsContactSearchController()
		{
			_contact = Bootstrap.Container.Resolve<IContactAccessService>();
		}
		[ActionName("csquery.scsvc")]
		public ActionResult QueryMongoContacts(string query)
		{
			Response.ContentType = "application/json";
			return Content(_contact.QueryContacts(query));
		}
	}
}
