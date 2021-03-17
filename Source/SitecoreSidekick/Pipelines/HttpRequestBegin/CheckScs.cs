using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Mvc.Extensions;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Security.Accounts;
using System.Web.Configuration;
using System.Configuration;
using Sitecore.Pipelines;

namespace Sidekick.Core.Pipelines.HttpRequestBegin
{
	public class CheckScs
	{
		public void Process(PipelineArgs args)
		{
			if (HttpContext.Current.Request.Url.AbsolutePath.Contains("/scs/"))
				args.AbortPipeline();
		}
	}
}
