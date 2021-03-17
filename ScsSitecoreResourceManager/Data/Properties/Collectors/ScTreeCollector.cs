using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sidekick.SitecoreResourceManager.Data.Properties.Collectors
{
	public class ScTreeCollector : IPropertyCollector
	{
		public string Id {get;set;}
		public string Name {get;set;}
		public string Description {get;set;}
		public string Value { get; set; }
		public string Values {get;set;}

		public string AngularMarkup { get
			{
				return @"
<div>
	<h4>
		{{property.Name}}
	</h4>
	<a class=""hgquestion"" ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<div class=""hgcollectordescription"" ng-if=""property.showdescription"">{{property.Description}}</div>
	<input ng-model=""property.Value"" />
	<hgcontenttree events=""vm.treeEvents"" parent=""'" + Values+@"'"" property=""property"" selected=""property.Value"" />
</div>";
			} }

		public string Processor { get; set; } = "ScTree";

		public bool Validate(string value)
		{
			return true;
		}
	}
}
