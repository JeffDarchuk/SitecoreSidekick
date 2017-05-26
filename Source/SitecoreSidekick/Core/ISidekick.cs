using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;

namespace SitecoreSidekick.Core
{
	public interface ISidekick
	{
		string Directive { get; set; }
		NameValueCollection DirectiveAttributes { get; set; }
		string ResourcesPath { get; set; }
		string Icon { get; }
		string Name { get; }
		string CssStyle { get; }
		bool AdminOnly { get; }
		List<string> Roles { get; }
		List<string> Users { get; } 
		string CompileEmbeddedResource(string fileExtension);
		ActionResult ProcessRequest(HttpContextBase context, string filename, dynamic data);
		void ProcessResourceRequest(HttpContextBase context, string filename, dynamic data);
		bool ApplicableSidekick();
		void RegisterRoutes();
	}
}
