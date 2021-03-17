using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Data.Properties.Collectors
{
	public class UserInputSelectCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }

		public string AngularMarkup { get
			{
				var options = Values.Split(',').Aggregate(new StringBuilder(), (current, next) => current.Append($" <option value='{next}' >{next}</option> ")).ToString();
				return @"
<div>
	<h4>
		{{property.Name}}
	</h4>
	<a class=""hgquestion"" ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<div class=""hgcollectordescription"" ng-if=""property.showdescription"">{{property.Description}}</div>
	<select ng-model=""property.Value"">" + options+ @"</select>
</div>";
			} }

		

		public bool Validate(string value)
		{
			return true;
		}
		public string Processor { get; set; } = "UserInputSelect";
	}
}
