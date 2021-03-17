using System;
using System.Collections.Specialized;
using Sidekick.Core;

namespace Sidekick.XConnect
{
	class XConnectRegistration : ScsRegistration
	{
		public XConnectRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}

		public override string Identifier => "xc";
		public override string Directive => "xcmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "Sidekick.XConnect.Resources";
		public override Type Controller => typeof(XConnectController);
		public override string Icon => "/scs/xc/resources/xcicon.png";
		public override string Name => "XConnect Utility";
		public override string CssStyle => "min-width:600px;";
	}
}
