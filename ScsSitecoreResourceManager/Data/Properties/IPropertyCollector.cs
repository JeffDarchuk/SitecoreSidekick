using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Data.Properties
{
	public interface IPropertyCollector
	{
		string Id { get; set; }
		string Name { get; set; }
		string Description { get; set; }
		string Value { get; set; }
		string Values { get; set; }
		string AngularMarkup { get; }
		bool Validate(string value);
		string Processor { get; set; }
	}
}
