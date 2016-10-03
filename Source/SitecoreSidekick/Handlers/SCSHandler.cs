using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Core;
using SitecoreSidekick.Pipelines.HttpRequestBegin;

namespace SitecoreSidekick.Handlers
{
	/// <summary>
	/// base handler for http requests to the SCS 
	/// </summary>
	public class ScsHandler : ScsHttpHandler
	{
		private static List<ISidekick> Sidekicks { get; set; } = new List<ISidekick>();
		private static StringBuilder js = new StringBuilder();
		private static StringBuilder css = new StringBuilder();

		public override string Directive { get; set; }
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "SitecoreSidekick.Resources";
		public override string Icon => "";
		public override string Name => "Sitecore Sidekick";
		public override string CssStyle => "600px";

		public ScsHandler(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
			js.Append(CompileEmbeddedResource(".js"));
			css.Append(CompileEmbeddedResource(".css"));
		}
		public static void RegisterSideKick(ISidekick sk)
		{
			Sidekicks.Add(sk);
			js.Append(sk.CompileEmbeddedResource(".js"));
			css.Append(sk.CompileEmbeddedResource(".css"));
		}

		/// <summary>
		/// base HTTP request
		/// </summary>
		/// <param name="context"></param>
		public override void ProcessRequest(HttpContextBase context)
		{
			try
			{
				context.Response.StatusCode = 404;
				string file = GetFile(context);
				foreach (ISidekick sk in Sidekicks)
				{
					if (context.Response.StatusCode != 404)
						return;
					sk.ProcessRequest(context);
					if (context.Response.StatusCode != 404)
						return;
					sk.ProcessResourceRequest(context);
				}
				ProcessResourceRequest(context);
				if (context.Response.StatusCode == 404)
				{
					if (file == "scs.scs")
					{
						var html = GetResource("scsindex.scs").Replace("[[sidekicks]]", GetAllSidekickDirectives());
						if (context.Request.QueryString["desktop"] == "true")
							html = html.Replace("</head>", $"<style>{GenerateDesktopStyle()}</style></head>");
						ReturnResponse(context, html, "text/html");
					}
					else if (file.Equals("scscommand.js"))
						ReturnResource(context, file, "application/javascript");
					else if (file.EndsWith(".js"))
						ReturnResponse(context, js.ToString(), "application/javascript");
					else if (file.EndsWith(".css"))
						ReturnResponse(context, css.ToString(), "text/css");
					else if (file == "contenttreeselectedrelated.scsvc")
						ReturnJson(context, GetContentSelectedRelated(context));
					else
						NotFound(context);
				}
			}
			catch (Exception e)
			{
				Log.Error("Sitecore sidekick failed to return the proper resource", e, this);
				Error(context, e);
			}
		}

		private object GetContentSelectedRelated(HttpContextBase context)
		{
			var data = GetPostData(context);
			try
			{
				if (data.server != null)
				{
					WebClient wc = new WebClient { Encoding = Encoding.UTF8 };
					var node = JsonConvert.DeserializeObject<bool>(wc.UploadString($"{data.server}/scs/contenttreeselectedrelated.scsvc", "POST",
									$@"{{ ""currentId"": ""{data.currentId}"", ""selectedId"": ""{data.selectedId}""}}"));
					return node;
				}
			}
			catch (RuntimeBinderException)
			{

			}
			var db = Factory.GetDatabase("master", false);
			if (db == null)
				return false;
			if (data.currentId == "")
				return true;
			Item current = db.GetItem(data.currentId);
			Item selected = db.GetItem(data.selectedId);
			if (current == null || selected == null)
				return false;
			var spath = selected.Paths.FullPath;
			var cpath = current.Paths.FullPath;
			if (spath == cpath)
				return false;
			return spath.StartsWith(cpath);
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
			var basicAngularIf = $"!vm.sidekick || ({"vm.sidekick != '" + string.Join("' && vm.sidekick != '", Sidekicks.Select(x=>x.Name).ToArray())}')";
			var sb = new StringBuilder($"<div ng-style=\"({basicAngularIf}) && {{'width':'{CssStyle}', 'background-color':'white'}}\"><h3 id=\"sidekickHeader\" ng-if=\"{basicAngularIf}\">{Name}<span class='close' onclick='window.top.document.getElementById(\"scs\").style.display=\"none\";'></span></h3>");
			foreach (var sk in Sidekicks.Where(x => x.ApplicableSidekick()))
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
