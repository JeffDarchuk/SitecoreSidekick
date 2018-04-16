using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class ApplyFilesToProject
	{
		public void Process(SitecoreResourceManagerArgs args)
		{
			if (args.TargetCsProjPath == null) return;
			var proj = File.ReadAllText(args.TargetCsProjPath);
			int index = proj.LastIndexOf("</ItemGroup>", StringComparison.Ordinal) + "</ItemGroup>".Length;

			StringBuilder newItems = new StringBuilder("\n\t<ItemGroup>");
			foreach (var newPath in args.NewOverlayFiles)
			{
				if (newPath.EndsWith(".cs"))
				{
					newItems.Append($"\n\t\t<Compile Include=\"{newPath.Substring(args.OverlayTarget.Length + 1)}\" />");
				}
				else
				{
					newItems.Append($"\n\t\t<Content Include=\"{newPath.Substring(args.OverlayTarget.Length + 1)}\" />");
				}
				args.EventLog.Add($"Prepping new file {newPath.Substring(args.OverlayTarget.Length + 1)} to be added to project {args.TargetCsProjPath}");
			}
			newItems.Append("\n\t</ItemGroup>");
			proj = proj.Insert(index, newItems.ToString());
			File.WriteAllText(args.TargetCsProjPath, proj);
			args.EventLog.Add($"Adding new files to {args.TargetCsProjPath}");
		}
	}
}
