using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ScsSitecoreResourceManager.Data.Properties.Collectors
{
	public class IconCollector : IPropertyCollector
	{
		public static Dictionary<string, ZipArchiveEntry> Images = new Dictionary<string, ZipArchiveEntry>();
		static IconCollector()
		{
			string iconRoot = HttpContext.Current.Server.MapPath("~/") + "\\sitecore\\shell\\Themes\\Standard";
			foreach (string file in Directory.GetFiles(iconRoot, "*.zip"))
			{
				var zip = ZipFile.OpenRead(file);
				foreach (var entry in zip.Entries)
				{
					if (entry.FullName.EndsWith(".png") && entry.FullName.Contains("32x32"))
					{
						Images[entry.FullName] = entry;
					}

				}
			}
		}
		public string Id {get;set;}
		public string Name {get;set;}
		public string Description {get;set;}
		public string Value { get; set; }
		public string Values {get;set;}

		public string AngularMarkup { get
			{
				var options = Images.Keys.Aggregate(new StringBuilder(), (running, x) => running.Append($"'{x}',"));
				var arr = $"[{options.Remove(options.Length - 1, 1)}]";
				return @"
<div ng-init=""property.list="+arr+ @""">
	<h4 ng-init=""property.search = 'gear'"" >
		{{property.Name}}
	</h4>
	<a class=""hgquestion"" ng-mouseover=""property.showdescription = true"" ng-mouseleave=""property.showdescription = false"">?</a>
	<div class=""hgcollectordescription"" ng-if=""property.showdescription"">{{property.Description}}</div>
	<div><span>Search</span><input style=""width:100px"" ng-model=""property.search"" /></div>
	<div class=""hgicon""  ng-repeat=""icon in property.list | filter:property.search| limitTo:200"">
		<label>
			<input type=""radio"" class=""hgiconradio"" name=""hgicon"" ng-model=""property.Value"" value=""{{icon}}"">
			<img ng-class=""{'hgiconselected' : icon === property.Value}"" ng-src=""/scs/hg/hgicon.scsvc?icon={{icon}}"" />
		</label>
	</div> 
	<input ng-model=""property.Value"" />
</div>";
			} }

		public string Processor { get; set; } = "SitecoreIcon";

		public bool Validate(string value)
		{
			return true;
		}
	}
}
