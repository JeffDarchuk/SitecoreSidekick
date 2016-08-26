using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Common;
using Sitecore.Mvc.ExperienceEditor.Pipelines.RenderPageExtenders;

namespace SitecoreSidekick.Pipelines.ExperienceEditor
{
	class RegisterSidekickForEe : RenderPageExtendersProcessor
	{
		public override void Process(RenderPageExtendersArgs args)
		{
			Assert.ArgumentNotNull((object)args, "args");
			args.Disposables.Add((IDisposable)new GenericDisposable((Action)(() => args.Writer.Write(@"
<script src='/scs/scscommand.js'>

</script>"))));
		}

		protected override bool Render(TextWriter writer)
		{
			return false;
		}
	}
}
