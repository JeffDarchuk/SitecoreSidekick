using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Data.Properties.Collectors
{
	public class GuidCollector : IPropertyCollector
	{
		public string Id {get;set;}
		public string Name {get;set;}
		public string Description {get;set;}
		public string Value
		{
			get
			{
				return Id.EndsWith("LOWER_") ? Guid.NewGuid().ToString("D").ToLower() : Guid.NewGuid().ToString("B");
			}
			set
			{
			}
		}
		public string Values {get;set;}

		public string AngularMarkup => "";

		public string Processor { get; set; } = "Guid";

		public bool Validate(string value)
		{
			return true;
		}
	}
}
