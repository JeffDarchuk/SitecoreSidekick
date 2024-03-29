﻿using System;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Sidekick.Core.Commands
{
	public class OpenScs : Command
	{
		public override void Execute(CommandContext context)
		{
			var builder = new UriBuilder(HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Host, HttpContext.Current.Request.Url.Port);
			Assert.ArgumentNotNull(context, "context");
			try
			{
				typeof(Sitecore.Shell.Framework.Windows).GetMethod("RunUri", new [] {typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) }).Invoke(null, new object[]
				{
					$"/scs/platform/scs.scs?desktop=true", "office/32x32/sword.png",
					"Sitecore Sidekick", "1000px", "700px"
				});
			}
			catch (Exception)
			{
				SheerResponse.Eval($"var scs = window.top.document.getElementById(\"scs\");scs.innerHTML = \" <iframe id = 'scs-iframe' style = 'width:100%;height:100%;background-color: transparent;' src = '/scs/platform/scs.scs' /> \";scs.style.display = \"block\";scs.style.position = \"absolute\";");
			}
		}
	}
}
