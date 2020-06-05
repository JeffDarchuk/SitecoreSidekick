using Microsoft.CSharp.RuntimeBinder;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using SitecoreSidekick.Core;
using SitecoreSidekick.Models;
using SitecoreSidekick.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SitecoreSidekick.Handlers
{
	public class ScsMainController : ScsController
	{
		private readonly IScsRegistrationService _registration;
		private readonly IAuthenticationService _authenticationService;
		private readonly IJsonSerializationService _jsonSerializationService;
		private readonly IHttpClientService _httpClientService;
		private readonly ISitecoreDataAccessService _sitecoreDataAccessService;
		private readonly IIconService _iconService;

		public ScsMainController()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_authenticationService = Bootstrap.Container.Resolve<IAuthenticationService>();
			_jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();
			_httpClientService = Bootstrap.Container.Resolve<IHttpClientService>();
			_sitecoreDataAccessService = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
			_iconService = Bootstrap.Container.Resolve<IIconService>();
		}

		[ActionName("scsvalid.scsvc")]
		public ActionResult Valid()
		{
			string ticket = _authenticationService.GetCurrentTicketId();
			if (!string.IsNullOrWhiteSpace(ticket) && !_authenticationService.IsAuthenticated)
				_authenticationService.Relogin(ticket);
			if (!_authenticationService.IsAuthenticated)
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
		public async Task<ActionResult> SelectedRelated(ContentSelectedRelatedModel model)
		{
			return ScsJson(await GetContentSelectedRelated(model));
		}

		[ActionName("scscommand.js")]
		public ActionResult GetCommand()
		{
			return Resources("scscommand.js");
		}
		[ActionName("scsicon.scsvc")]
		public ActionResult Icon()
		{
			string id = Request.QueryString["id"];
			string icon = Request.QueryString["icon"];
			if (string.IsNullOrWhiteSpace(icon) && !string.IsNullOrWhiteSpace(id))
			{
				Guid guid = Guid.Empty;
				if (Guid.TryParse(id, out guid))
				{
					icon = _sitecoreDataAccessService.GetIcon(guid);
				}
			}
			if (_iconService.Images.ContainsKey(icon))
			{
				return new FileStreamResult(_iconService.Images[icon].Open(), "image/png");
			}
			return new FileStreamResult(_iconService.Images["Core/32x32/new_document.png"].Open(), "image/png");
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

		private async Task<Dictionary<string, string>> GetContentSelectedRelated(ContentSelectedRelatedModel data)
		{
			try
			{
				if (data.Server != null)
				{
					var node = _jsonSerializationService.DeserializeObject<Dictionary<string, string>>(
						await _httpClientService.Post($"{data.Server}/scs/platform/contenttreeselectedrelated.scsvc",
							$@"{{ ""selectedIds"": {_jsonSerializationService.SerializeObject(data.SelectedIds)}, ""server"": null}}").ConfigureAwait(false));
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
				ScsSitecoreItem i = _sitecoreDataAccessService.GetScsSitecoreItem(selectedId).Parent;					
				while (i != null)
				{
					if (!ret.ContainsKey(i.Id))
						ret.Add(i.Id, "1");
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
			IScsRegistration sidekick = _registration.GetScsRegistration<ScsMainRegistration>();
			var basicAngularIf = $"!vm.sidekick || ({"vm.sidekick != '" + string.Join("' && vm.sidekick != '", _registration.GetAllSidekicks().Select(x => x.Name).ToArray())}')";
			var sb = new StringBuilder($"<div ng-style=\"({basicAngularIf}) \"><h3 id=\"sidekickHeader\" ng-if=\"{basicAngularIf}\">{sidekick.Name}<span class='close' onclick='window.top.document.getElementById(\"scs\").style.display=\"none\";'></span></h3>");
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
