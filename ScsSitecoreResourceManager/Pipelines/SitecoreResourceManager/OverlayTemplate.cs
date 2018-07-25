using SitecoreSidekick.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class OverlayTemplate
	{
		public void Process(SitecoreResourceManagerArgs args)
		{
			string root = args.OverlayTarget;
			if (File.Exists(args.OverlayTarget))
			{
				root = Path.GetDirectoryName(args.OverlayTarget);
			}
			HashSet<string> directories = new HashSet<string>();
			HashSet<string> files = new HashSet<string>();
			using (var archive = ZipFile.OpenRead(args.Wrapper.TemplateZip))
			{
				foreach (var s in archive.Entries)
				{
					if (s.FullName.ToLower() == "properties.json")
						continue;
					var parts = s.FullName.Split('/');
					for (int i = 1; i <= parts.Length; i++)
					{
						if (i == parts.Length && parts[i-1] != "")
						{
							files.Add($@"{root}\{string.Join("/", parts.Take(i)).Replace('/','\\')}".TrimEnd('\\'));
						}
						else
						{
							directories.Add($@"{root}\{string.Join("/", parts.Take(i)).Replace('/', '\\')}".TrimEnd('\\'));
						}
					}
				}
			}
			args.NewOverlayDirectories = new SortedSet<string>(directories, Comparer<string>.Create(Compare));
			args.NewOverlayFiles = new SortedSet<string>(files, Comparer<string>.Create(Compare));
			ZipFile.ExtractToDirectory(args.Wrapper.TemplateZip, root);
			File.Delete(root + "\\properties.json");
			args.EventLog.Add($"Overlaying template files at root {root}");
		}

		private int Compare(string s1, string s2)
		{
			int ret = s1.Split('\\').Length - s2.Split('\\').Length;
			if (ret == 0)
				return 1;
			return ret;
		}
	}
}
