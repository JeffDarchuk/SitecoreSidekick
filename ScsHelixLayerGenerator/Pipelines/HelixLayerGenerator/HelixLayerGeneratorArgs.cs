using ScsHelixLayerGenerator.Data;
using ScsHelixLayerGenerator.Data.Properties;
using ScsHelixLayerGenerator.Data.Properties.Collectors;
using ScsHelixLayerGenerator.Data.Targets;
using Sitecore.Pipelines;
using SitecoreSidekick.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Pipelines.HelixLayerGenerator
{
	public class HelixLayerGeneratorArgs : PipelineArgs
	{
		public Dictionary<string, DefaultCollector> Properties { get; set; }
		public PropertiesWrapper Wrapper { get; set; }
		public List<string> NewLayerRoots { get; set; }
		public HelixLayerGeneratorArgs(Dictionary<string,DefaultCollector> properties, PropertiesWrapper wrapper)
		{
			Properties = properties;
			Wrapper = wrapper;
		}
	}
}
