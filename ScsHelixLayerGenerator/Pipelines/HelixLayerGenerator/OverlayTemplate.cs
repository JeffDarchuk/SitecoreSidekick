using SitecoreSidekick.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Pipelines.HelixLayerGenerator
{
	public class OverlayTemplate
	{
		public void Process(HelixLayerGeneratorArgs args)
		{
			string solutionPath = Path.GetDirectoryName(args.Properties["_SOLUTIONPATH_"].Value);
			var zip = ZipFile.OpenRead(args.Wrapper.TemplateZip);
			foreach (var entry in zip.Entries)
			{
				int index = entry.FullName.IndexOf('/');
				if (index > 0)
				{
					if (Directory.Exists($@"{solutionPath}\{entry.FullName.Substring(0, index)}"))
					{
						try
						{
							Directory.Delete($@"{solutionPath}\{entry.FullName.Substring(0, index)}", true);
						}
						catch (IOException e)
						{
							throw new IOException($@"Unable to delete existing project at { solutionPath }\{ entry.FullName}, something may be locking this folder.", e);
						}
					}
					if (Directory.Exists($@"{solutionPath}\{ReplaceAllTokens.ReplaceTokens(entry.FullName.Substring(0, index), args.Properties)}"))
					{
						try
						{
							Directory.Delete($@"{solutionPath}\{ReplaceAllTokens.ReplaceTokens(entry.FullName.Substring(0, index), args.Properties)}", true);
						}
						catch (IOException e)
						{
							throw new IOException($@"Unable to delete existing project at {solutionPath}\{ReplaceAllTokens.ReplaceTokens(entry.FullName.Substring(0, index), args.Properties)}, something may be locking this folder.", e);
						}
					}
				}
				else
				{
					if (File.Exists($@"{solutionPath}\{entry.FullName}"))
					{
						try
						{
							File.Delete($@"{solutionPath}\{entry.FullName}");
						}
						catch (IOException e)
						{
							throw new IOException($@"Unable to delete existing project at {solutionPath}\{entry.FullName}, something may be locking this file.", e);
						}
					}
					if (File.Exists($@"{solutionPath}\{ReplaceAllTokens.ReplaceTokens(entry.FullName, args.Properties)}"))
					{
						try
						{
							File.Delete($@"{solutionPath}\{ReplaceAllTokens.ReplaceTokens(entry.FullName, args.Properties)}");
						}
						catch (IOException e)
						{
							throw new IOException($@"Unable to delete existing project at {solutionPath}\{ReplaceAllTokens.ReplaceTokens(entry.FullName, args.Properties)}, something may be locking this file.", e);
						}
					}
				}
			}
			HashSet<string> tracker = new HashSet<string>(Directory.GetDirectories(solutionPath));
			ZipFile.ExtractToDirectory(args.Wrapper.TemplateZip, solutionPath);
			args.NewLayerRoots = new List<string>(Directory.GetDirectories(solutionPath).Where(x => !tracker.Contains(x)));
			File.Delete(solutionPath + "\\properties.json");
		}
	}
}
