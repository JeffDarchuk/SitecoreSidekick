using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using SitecoreSidekick.Core;

namespace SitecoreSidekick.Handlers
{
	/// <summary>
	/// base handler for http requests to the SCS 
	/// </summary>
	public class ScsMainHandlerController : ScsHandler
	{
		internal static List<ISidekick> Sidekicks { get; set; } = new List<ISidekick>();
		private static StringBuilder js = new StringBuilder();
		private static StringBuilder css = new StringBuilder();
		private static bool addedSelf = false;
		private static object locker = new object();

		public override string Directive { get; set; }
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "SitecoreSidekick.Resources";
		public override string Icon => "";
		public override string Name => "Sitecore Sidekick";
		public override string CssStyle => "600px";

		public ScsMainHandlerController()
		{
			lock (Locker)
			{
				if (!_addedSelf)
				{
					_addedSelf = true;
					Js.Insert(0, CompileEmbeddedResource(".js"));
					Css.Insert(0, CompileEmbeddedResource(".css"));
				}
			}
		}
		public ScsMainHandlerController(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
			lock (Locker)
			{
				if (!_addedSelf)
				{
					_addedSelf = true;
					Js.Insert(0, CompileEmbeddedResource(".js"));
					Css.Insert(0, CompileEmbeddedResource(".css"));
				}
			}
		}
		public static void RegisterSideKick(ISidekick sk, bool addSidekick = true)
		{
			Sidekicks.Add(sk);
			js.Insert(0, sk.CompileEmbeddedResource(".js"));
			css.Insert(0, sk.CompileEmbeddedResource(".css"));
		}
		//[Route("scs/{filename}")]
		//[ActionName("scs")]
		//public ActionResult ScsEntry(string filename)
		//{
		//	string ticket = Sitecore.Web.Authentication.TicketManager.GetCurrentTicketId();
		//	if (!string.IsNullOrWhiteSpace(ticket))
		//		Sitecore.Web.Authentication.TicketManager.Relogin(ticket);
		//	var data = GetPostData(Request.InputStream);
		//	var result = ProcessRequest(Request.RequestContext.HttpContext, filename, data);
		//	if (result == null)
		//		return Content("", Response.ContentType);
		//	return result;
		//}


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
		/// <summary>
		/// base HTTP request
		/// </summary>
		/// <param name="context"></param>
		public override ActionResult ProcessRequest(HttpContextBase context, string filename, dynamic data)
		{
			try
			{
				context.Response.StatusCode = 404;

				ProcessResourceRequest(context, filename, data);

				if (context.Response.StatusCode == 404)
				{
					if (filename == "scs.scs")
					{
						var html = GetResource("scsindex.scs").Replace("[[sidekicks]]", GetAllSidekickDirectives());
						if (context.Request.QueryString["desktop"] == "true")
						{
							html = html.Replace("</head>", $"<style>{GenerateDesktopStyle()}</style></head>");
						}

						ReturnResponse(context, html, "text/html");
					}
					else if (filename.EndsWith(".js")) ReturnResponse(context, js.ToString(), "application/javascript");
					else if (filename.EndsWith(".css")) ReturnResponse(context, css.ToString(), "text/css");
					else if (filename == "contenttreeselectedrelated.scsvc") ReturnJson(context, GetContentSelectedRelated(data));
					else if (filename == "scsvalid.scsvc") ReturnJson(context, true);
					else if (Response.StatusCode == 404) NotFound(context, "Requested resource was not found.");
					else if (Response.StatusCode == 403) ReturnResponse(context, "Unauthorized to perform this action", "text/plain", HttpStatusCode.Forbidden);
				}
			}
			catch (Exception e)
			{
				Log.Error("Sitecore sidekick failed to return the proper resource", e, this);
				Error(context, e);
			}

			return null;
		}

		private bool GetContentSelectedRelated(dynamic data)
		{
			try
			{
				if (data.server != null)
				{
					WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
					bool node = JsonNetWrapper.DeserializeObject<bool>(wc.UploadString($"{data.server}/scs/contenttreeselectedrelated.scsvc", "POST",
									$@"{{ ""currentId"": ""{data.currentId}"", ""selectedId"": {JsonNetWrapper.SerializeObject(data.selectedId.Count == 1 ? data.selectedId.FirstOrDefault() : data.selectedId)}}}"));
				
					return node;
				}
			}
			catch (RuntimeBinderException)
			{

			}

			if (data.selectedId is string)
			{
				return ContentSelectedRelated(data.currentId, data.selectedId);
			}

			foreach (object selectedId in data.selectedId)
			{
				if (selectedId != null && ContentSelectedRelated(data.currentId, selectedId.ToString()))
				{
					return true;
				}
			}
			return false;
		}

		private static bool ContentSelectedRelated(string currentId, string selectedId)
		{
			var db = Factory.GetDatabase("master", false);
			if (db == null) return false;
			if (currentId == "") return true;

			Item current = db.GetItem(currentId);
			Item selected = db.GetItem(selectedId);

			if (current == null || selected == null) return false;

			var spath = selected.Paths.FullPath;
			var cpath = current.Paths.FullPath;

			if (spath == cpath) return false;

			return spath.StartsWith(cpath);
		}

		private string GenerateDesktopStyle()
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
			var basicAngularIf = $"!vm.sidekick || ({"vm.sidekick != '" + string.Join("' && vm.sidekick != '", Sidekicks.Select(x=>x.Name).ToArray())}')";
			var sb = new StringBuilder($"<div ng-style=\"({basicAngularIf}) && {{'width':'{CssStyle}', 'background-color':'white'}}\"><h3 id=\"sidekickHeader\" ng-if=\"{basicAngularIf}\">{Name}<span class='close' onclick='window.top.document.getElementById(\"scs\").style.display=\"none\";'></span></h3>");
			foreach (var sk in Sidekicks.Where(x => x.ApplicableSidekick()))
			{
				sb.Append($"<div ng-if=\"{basicAngularIf}\" ng-click=\"vm.selectSidekick('{sk.Name}')\" class=\"btn scsbtn\"><img ng-src=\"{sk.Icon}\" width=\"32\" height=\"32\" class=\"scContentTreeNodeIcon\" border=\"0\"><div>{sk.Name}</div></div>");
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
