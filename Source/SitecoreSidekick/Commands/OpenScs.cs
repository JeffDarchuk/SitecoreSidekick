using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace SitecoreSidekick.Commands
{
	public class OpenScs : Command
	{
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull((object)context, "context");
			SheerResponse.Eval(
				"var scs = window.top.document.getElementById(\"scs\");scs.innerHTML = \" <iframe id = 'scs-iframe' style = 'width:100%;height:100%;background-color: transparent;' src = '/scs/scs.scs' /> \";scs.style.display = \"block\";scs.style.position = \"absolute\";");
		}
	}
}
