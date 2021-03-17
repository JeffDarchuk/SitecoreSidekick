using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;

namespace Sidekick.Core
{
	public interface IScsRegistration
	{
		string Directive { get;}
		NameValueCollection DirectiveAttributes { get;}
		string ResourcesPath { get;}
		Type Controller { get;}
		string Icon { get; }
		string Name { get; }
		string CssStyle { get; }
		bool AdminOnly { get; }
		List<string> Roles { get; }
		List<string> Users { get; } 
		string CompileEmbeddedResource(string fileExtension);
		bool ApplicableSidekick();
		void RegisterRoutes();
	}
}
