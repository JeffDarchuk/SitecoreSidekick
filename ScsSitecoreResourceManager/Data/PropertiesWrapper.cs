using ScsSitecoreResourceManager.Data.Properties;
using ScsSitecoreResourceManager.Data.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Data
{
	public class PropertiesWrapper
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public Dictionary<string, Dictionary<string,string>> Targets { get; set; }
		public Dictionary<string, ConfigurationNode> Properties { get; set; }
		public Dictionary<string, string> CompiledProperties { get; set; }
		public string TemplateZip { get; set; }
	}
}
