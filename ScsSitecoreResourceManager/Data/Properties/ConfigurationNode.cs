using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Data.Properties
{
	public class ConfigurationNode
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Processor { get; set; }
		public string PropertyType { get; set; }
		public bool Remember { get; set; }
		public string Default { get; set; }
		public string AngularMarkup { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }
	}
}
