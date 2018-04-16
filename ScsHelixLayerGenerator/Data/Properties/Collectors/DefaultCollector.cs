using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Data.Properties.Collectors
{
	public class DefaultCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name {get;set;} 
		public string Description {get;set;} 
		public string Value {get;set;} 
		public string Values {get;set;}

		public string AngularMarkup => "";

		public string Processor { get; set; } = "";

		public bool Validate(string value)
		{
			throw new NotImplementedException();
		}
	}
}
