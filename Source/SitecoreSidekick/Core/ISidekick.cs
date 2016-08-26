using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

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
		string CompileEmbeddedResource(string fileExtension);
		void ProcessRequest(HttpContextBase context);
		void ProcessResourceRequest(HttpContextBase context);

	}
}
