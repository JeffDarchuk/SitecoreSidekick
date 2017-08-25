using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using SitecoreSidekick.Core;
using SitecoreSidekick.Models;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace SitecoreSidekick.Handlers
{
	public class ScsMainController : ScsController
	{
		private readonly IScsRegistrationService _registration;
		public ScsMainController()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
		}

		protected ScsMainController(IScsRegistrationService registration)
		{
			_registration = registration;
		}
		[ActionName("scsvalid.scsvc")]
		public ActionResult Valid()
		{
			string ticket = Sitecore.Web.Authentication.TicketManager.GetCurrentTicketId();
			if (!string.IsNullOrWhiteSpace(ticket))
				Sitecore.Web.Authentication.TicketManager.Relogin(ticket);
			var user = Sitecore.Context.User;
			if (!user.IsAuthenticated)
			{
				return ScsJson(false);
			}
			return ScsJson(true);
		}
		[ScsLoggedIn]
		[ActionName("scs.scs")]
		public ActionResult ScsMain()
		{
			var html = GetResource("scsindex.scs").Replace("[[sidekicks]]", GetAllSidekickDirectives());
			if (Request.QueryString["desktop"] == "true")
			{
				html = html.Replace("</head>", $"<style>{GenerateDesktopStyle()}</style></head>");
			}

			return Content(html, "text/html");
		}
		[ActionName("contenttreeselectedrelated.scsvc")]
		public ActionResult SelectedRelated(ContentSelectedRelatedModel model)
		{
			return ScsJson(GetContentSelectedRelated(model));
		}

		public override ActionResult Resources(string filename)
		{
			if (filename.Equals("scsangular.js") || filename.Equals("scscommand.js"))
				return Content(GetResource(filename), "application/javascript");
			if (filename.EndsWith(".js"))
				return Content(_registration.Js, "application/javascript");
			if (filename.EndsWith(".css"))
				return Content(_registration.Css, "text/css");
			return base.Resources(filename);
		}

		private Dictionary<string, string> GetContentSelectedRelated(ContentSelectedRelatedModel data)
		{
			try
			{
				if (data.Server != null)
				{
					WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
					var node = JsonNetWrapper.DeserializeObject<Dictionary<string, string>>(wc.UploadString($"{data.Server}/scs/platform/contenttreeselectedrelated.scsvc", "POST",
									$@"{{ ""selectedIds"": {JsonNetWrapper.SerializeObject(data.SelectedIds)}, ""server"": null}}"));

					return node;
				}
			}
			catch (RuntimeBinderException)
			{

			}
			Dictionary<string, string> ret = new Dictionary<string, string>();
			foreach (string selectedId in data.SelectedIds)
			{
				BuildRelatedTree(ret, selectedId);
			}
			return ret;
		}

		private void BuildRelatedTree(Dictionary<string, string> ret, string selectedId)
		{
			using (new SecurityDisabler())
			{
				var db = Factory.GetDatabase("master", false);
				if (db == null)
					return;
				Item i = db.GetItem(selectedId).Parent;
				while (i != null)
				{
					if (!ret.ContainsKey(i.ID.ToString()))
						ret.Add(i.ID.ToString(), "1");
					i = i.Parent;
				}
			}
		}

		private object GenerateDesktopStyle()
		{
			return @"
#sidekickHeader{
	display:none;
}
.scs-root-div{
	margin:0px;
	width:100% !important;
}
.scs-form{
	max-height: 100% !important;
}
.full-width{
	width:100%;
}
#overlay{
	display:none;
}
body{
	background-color: #999;
	margin:0px;
}
#desktopSidekickHeader {
	display: block;
    padding: 5px;
    background: url(""/sitecore/shell/client/Speak/Assets/img/Speak/Layouts/breadcrumb_red_bg.png"")
}
.back{
	font-weight:bold;
}
.subheader-logo{
    margin-left: 100px;
    color: white;
    font-size: 20px;
    font-weight: bold;
}
	";
		}

		private string GetAllSidekickDirectives()
		{
			ScsMainRegistration sidekick = _registration.GetScsRegistration<ScsMainRegistration>();
			var basicAngularIf = $"!vm.sidekick || ({"vm.sidekick != '" + string.Join("' && vm.sidekick != '", _registration.GetAllSidekicks().Select(x => x.Name).ToArray())}')";
			var sb = new StringBuilder($"<div ng-style=\"({basicAngularIf}) && {{'width':'{sidekick.CssStyle}', 'background-color':'white'}}\"><h3 id=\"sidekickHeader\" ng-if=\"{basicAngularIf}\">{sidekick.Name}<span class='close' onclick='window.top.document.getElementById(\"scs\").style.display=\"none\";'></span></h3>");
			foreach (var sk in _registration.GetAllSidekicks().Where(x => x.ApplicableSidekick() && x.Name != "Sitecore Sidekick"))
			{
				sb.Append(
					$"<div ng-if=\"{basicAngularIf}\" ng-click=\"vm.selectSidekick('{sk.Name}')\" class=\"btn scsbtn\"><img ng-src=\"{sk.Icon}\" width=\"32\" height=\"32\" class=\"scContentTreeNodeIcon\" border=\"0\"><div>{sk.Name}</div></div>");
				sb.Append($"<div id=\"{sk.Name.Replace(" ", string.Empty).ToLower()}\" ng-if=\"vm.sidekick == '{sk.Name}'\" targetWidth=\"{sk.CssStyle}\"><div id=\"desktopSidekickHeader\"><span class=\"back\" ng-click=\"vm.goHome()\"><svg class=\"icon icon-arrow-left\"><use xlink:href=\"#icon-arrow-left\"></use></svg> Return Home</span><span class=\"subheader-logo\">{sk.Name}</span><span class='close' onclick='window.top.document.getElementById(\"scs\").style.display=\"none\";'></span></div><h3 id=\"sidekickHeader\">{sk.Name}<span class=\"back\" ng-click=\"vm.goHome()\">Return Home</span><span class='close' onclick='window.top.document.getElementById(\"scs\").style.display=\"none\";'></span></h3><div class=\"scs-form\"><{sk.Directive} ");
				if (sk.DirectiveAttributes != null && sk.DirectiveAttributes.Count > 0)
					foreach (var key in sk.DirectiveAttributes.AllKeys)
					{
						sb.Append($"{key}=\"{sk.DirectiveAttributes[key]}\" ");
					}
				sb.Append($"></{sk.Directive}></div></div>");
			}
			sb.Append("</div>");
			return sb.ToString();
		}
	}
}
