using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Data.Properties.Collectors
{
	public class UserInputCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }

		public string AngularMarkup =>
	@"
<div>
	<h4>
		{{property.Name}}
	</h4>
	<a ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<input ng-model=""property.Value""/>
	<div ng-if=""property.showdescription"">{{property.Description}}</div>
</div>";
		

		public bool Validate(string value)
		{
			return true;
		}
		public string Processor { get; set; } = "UserInput";
	}
}
