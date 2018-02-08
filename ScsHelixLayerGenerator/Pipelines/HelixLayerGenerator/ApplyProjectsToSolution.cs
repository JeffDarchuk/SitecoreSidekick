using ScsHelixLayerGenerator.Data.Properties;
using ScsHelixLayerGenerator.Data.Properties.Collectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ScsHelixLayerGenerator.Pipelines.HelixLayerGenerator
{
	public class ApplyProjectsToSolution
	{
		private const string _solutionProjectTemplate = @"Project(""{0}"") = ""{1}"", ""{2}"", ""{3}""
EndProject";
		private const string _solutionGlobalSectionTemplate = @"
		{0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{0}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{0}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{0}.Release|Any CPU.Build.0 = Release|Any CPU";

		//Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Humana.HumanaComTenant.Project.HumanaCom"", ""Humana.HumanaComTenant.Project.HumanaCom\Humana.HumanaComTenant.Project.HumanaCom.csproj"", ""{ABB9DB14-D83B-4A2A-BC4B-20A20A8C037D}""
		//EndProject";
		public void Process(HelixLayerGeneratorArgs args)
		{
			string solution = File.ReadAllText(args.Properties["_SOLUTIONPATH_"].Value);
			foreach(string root in args.NewLayerRoots)
			{
				foreach (string proj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
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
							$"{args.Properties["_PREFIX_"].Value}.{args.Properties["_LAYER_"].Value}.{args.Properties["_PROJECTNAME_"].Value}",
							proj.Split(new[] { Path.GetDirectoryName(args.Properties["_SOLUTIONPATH_"].Value)+'\\' }, StringSplitOptions.None).Last(),
							projGuid));
					index = solution.IndexOf("GlobalSection(ProjectConfigurationPlatforms) = postSolution") + 59;
					solution = solution.Insert(index,
						string.Format(_solutionGlobalSectionTemplate, projGuid));
				}
			}
			File.WriteAllText(args.Properties["_SOLUTIONPATH_"].Value, solution);
		}
	}
}
