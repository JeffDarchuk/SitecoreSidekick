using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;

namespace TargetNamespace
{
	class ScsAppNameRegistration : ScsRegistration
	{
		public ScsAppNameRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}

		public override string Identifier => "AppCode";
		public override string Directive => "AppCodemasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "TargetNamespace.Resources";
		public override Type Controller => typeof(ScsAppNameController);
		public override string Icon => "/scs/AppCode/resources/AppCodeicon.png";
		public override string Name => "HumanReadableName";
		public override string CssStyle => "min-width:600px;";
	}
}
