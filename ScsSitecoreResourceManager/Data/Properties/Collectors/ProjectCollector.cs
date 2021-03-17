using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Data.Properties.Collectors
{
	public class ProjectCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }

		public string AngularMarkup =>
			@"
<div ng-init=""vm.getProjects(property);"">
	<h4>
		{{property.Name}}
	</h4>
	
	<a class=""hgquestion"" ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<div class=""hgcollectordescription"" ng-if=""property.showdescription"">{{property.Description}}</div>
	<br/>
	<strong>Choose project to insert into</strong>
	<select ng-init=""vm.getControllers(property)"" ng-model=""property.Value"" ng-if=""property.projects"" ng-options=""o as o for o in property.projects""></select>
</div>";

		public bool Validate(string value)
		{
			return value.EndsWith(".csproj");
		}

		public string Processor { get; set; } = "Project";
	}
}
