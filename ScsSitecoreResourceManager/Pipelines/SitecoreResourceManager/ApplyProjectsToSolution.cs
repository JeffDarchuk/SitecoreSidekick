using ScsSitecoreResourceManager.Data.Properties;
using ScsSitecoreResourceManager.Data.Properties.Collectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class ApplyProjectsToSolution
	{
		private const string _solutionProjectTemplate = "\n"+@"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""
EndProject";
		private const string _solutionGlobalSectionTemplate = @"
		{0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{0}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{0}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{0}.Release|Any CPU.Build.0 = Release|Any CPU";

		//Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Humana.HumanaComTenant.Project.HumanaCom"", ""Humana.HumanaComTenant.Project.HumanaCom\Humana.HumanaComTenant.Project.HumanaCom.csproj"", ""{ABB9DB14-D83B-4A2A-BC4B-20A20A8C037D}""
		//EndProject";
		public void Process(SitecoreResourceManagerArgs args)
		{
			string solution = File.ReadAllText(args.SolutionPath);
			foreach(string proj in args.NewOverlayFiles.Where(x => x.EndsWith(".csproj")))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(proj);
				var projGuid = doc.GetElementsByTagName("ProjectGuid")[0].InnerText;
				var projTypeGuid = doc.GetElementsByTagName("ProjectTypeGuids")[0].InnerText.Split(';').Last();
				int index = solution.IndexOf("Project(\"") - 1;
				solution = solution.Insert(index,
					string.Format(
						_solutionProjectTemplate,
						projTypeGuid.ToUpper(),
						$"{args.Prefix}.{args.Layer}.{args.ProjectName}",
						proj.Split(new[] { Path.GetDirectoryName(args.SolutionPath)+'\\' }, StringSplitOptions.None).Last(),
						projGuid));
				index = solution.IndexOf("GlobalSection(ProjectConfigurationPlatforms) = postSolution") + 59;
				solution = solution.Insert(index,
					string.Format(_solutionGlobalSectionTemplate, projGuid));
				args.EventLog.Add($"Prepping new project {proj} to be added to the solution {args.SolutionPath}");
			}
			File.WriteAllText(args.SolutionPath, solution);
			args.EventLog.Add($"Adding new projects to solution path {args.SolutionPath}");
		}
	}
}
