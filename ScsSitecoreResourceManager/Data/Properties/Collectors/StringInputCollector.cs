using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Data.Properties.Collectors
{
	public class StringInputCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }

		public string AngularMarkup { get; set; }

		

		public bool Validate(string value)
		{
			return true;
		}
		public string Processor { get; set; } = "String";
	}
}
