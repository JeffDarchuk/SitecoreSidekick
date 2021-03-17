using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sidekick.Core.ContentTree;
using Sidekick.Core;
using Sidekick.Core.Models;
using Sidekick.Core.Pipelines.HttpRequestBegin;
using Sidekick.Core.Services;
using Sidekick.Core.Services.Interface;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.Core.Handlers
{
	/// <summary>
	/// base handler for http requests to the SCS 
	/// </summary>
	public class ScsMainRegistration : ScsRegistration
	{
		public override string Directive => string.Empty;
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string Icon => "";
		public override string Name => "Sitecore Sidekick";
		public override string ResourcesPath => "Sidekick.Core.Resources";
		public override string CssStyle => "600px";
		public ScsMainRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}
		public override string Identifier => "scs";
		public override Type Controller => typeof(ScsMainController);
	}
}
