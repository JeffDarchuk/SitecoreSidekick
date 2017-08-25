using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;

namespace ScsContactSearch
{
	class ScsContactSearchRegistration : ScsRegistration
	{
		public ScsContactSearchRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}

		public ScsContactSearchRegistration(string roles, string isAdmin, string users, IScsRegistrationService registration) : base(roles, isAdmin, users, registration)
		{
		}

		public override string Identifier => "cs";
		public override string Directive => "csmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "ScsContactSearch.Resources";
		public override Type Controller => typeof(ScsContactSearchController);
		public override string Icon => "/scs/cs/resources/cs.png";
		public override string Name => "Contact Search";
		public override string CssStyle => "min-width:600px;";
	}
}
