using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Data.Properties.Collectors
{
	public class UnicornConfigurationDependencyCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }

		public string AngularMarkup { get {
				var options = Unicorn.Configuration.UnicornConfigurationManager.Configurations.Aggregate(new StringBuilder(), (running, x) => running.Append($"<option value='{x.Name}'>{x.Name}</option>")).ToString();
				return @"
<div>
	<h4>
		{{property.Name}}
	</h4>
	<a class=""hgquestion"" ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<div class=""hgcollectordescription"" ng-if=""property.showdescription"">{{property.Description}}</div>
	<select multiple ng-model=""property.Value"">" + options+@"</select>
</div>";
			} }

		public string Processor { get; set; } = "UnicornConfigs";

		public bool Validate(string value)
		{
			return true;
		}
	}
}
