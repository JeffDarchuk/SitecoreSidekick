using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;
using Sitecore.Configuration;
using System.Web;
using ScsHelixLayerGenerator.Data;
using System.IO;
using System.IO.Compression;
using ScsHelixLayerGenerator.Data.Properties;

namespace ScsHelixLayerGenerator
{
	public class ScsHelixLayerGeneratorRegistration : ScsRegistration
	{
		private IJsonSerializationService _json;
		public ScsHelixLayerGeneratorRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
			_json = Bootstrap.Container.Resolve<IJsonSerializationService>();
		}

		public override string Identifier => "hg";
		public override string Directive => "hgmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "ScsHelixLayerGenerator.Resources";
		public override Type Controller => typeof(ScsHelixLayerGeneratorController);
		public override string Icon => "/scs/hg/resources/hgicon.png";
		public override string Name => "Helix Layer Generator";
		public override string CssStyle => "min-width:600px;";
		public string GetDataDirectory()
		{
			string filepath;
			if (System.Text.RegularExpressions.Regex.IsMatch(Settings.DataFolder, @"^(([a-zA-Z]:\\)|(//)).*")) //if we have an absolute path, rather than relative to the site root
				filepath = Settings.DataFolder +
						   @"\HelixLayerGenerator";
			else
				filepath = HttpRuntime.AppDomainAppPath + Settings.DataFolder.Substring(1) +
						   @"\HelixLayerGenerator";
			if (!System.IO.Directory.Exists(filepath))
				System.IO.Directory.CreateDirectory(filepath);
			return filepath;
		}
		public PropertiesWrapper GetPropertiesWrapper(string template)
		{
			string templateDirectory = GetDataDirectory() + "\\Templates";
			string templatePath = Directory.GetFiles(templateDirectory).FirstOrDefault(x => x.EndsWith(template));
			PropertiesWrapper ret = new PropertiesWrapper();
			using (ZipArchive archive = ZipFile.OpenRead(templatePath))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					if (entry.FullName.EndsWith("Properties.json", StringComparison.OrdinalIgnoreCase))
					{
						ret = _json.DeserializeObject<PropertiesWrapper>(new StreamReader(entry.Open()).ReadToEnd());
						break;
					}
				}
			}
			ret.TemplateZip = templatePath;
			return ret;
		}
	}
}
