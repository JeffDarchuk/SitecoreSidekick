using ScsHelixLayerGenerator.Data.Properties;
using ScsHelixLayerGenerator.Data.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Data
{
	public class PropertiesWrapper
	{

		public Dictionary<string, Dictionary<string,string>> Targets { get; set; }
		public Dictionary<string, ConfigurationNode> Properties { get; set; }
		public string TemplateZip { get; set; }
	}
}
