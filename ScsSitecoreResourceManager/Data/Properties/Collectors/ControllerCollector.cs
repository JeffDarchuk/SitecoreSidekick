using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Data.Properties.Collectors
{
	public class ControllerCollector : IPropertyCollector
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public string Values { get; set; }

		public string AngularMarkup =>
			@"
<div ng-init=""vm.getProjects(property); vm.initArray(property); "">
	<h4>
		{{property.Name}}
	</h4>
	<a class=""hgquestion"" ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<div class=""hgcollectordescription"" ng-if=""property.showdescription"">{{property.Description}}</div>
	<br/>
	<strong>Choose project to insert into</strong>
	<select ng-init=""vm.getControllers(property)"" ng-change=""vm.getControllers(property)"" ng-model=""property.Value[0]"" ng-if=""property.projects"" ng-options=""o as o for o in property.projects""></select>
	<strong>Choose controller to add to</strong>
	<select ng-model=""property.Value[1]"" ng-if=""property.controllers"" ng-options=""o as o for o in property.controllers""></select>
</div>";

		public bool Validate(string value)
		{
			return value.EndsWith(".cs");
		}

		public string Processor { get; set; } = "Controller";
	}
}
