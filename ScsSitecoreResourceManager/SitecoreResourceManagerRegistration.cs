using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;
using Sitecore.Configuration;
using System.Web;
using Sidekick.SitecoreResourceManager.Data;
using System.IO;
using System.IO.Compression;
using Sidekick.SitecoreResourceManager.Data.Properties;

namespace Sidekick.SitecoreResourceManager
{
	public class SitecoreResourceManagerRegistration : ScsRegistration
	{
		private readonly IJsonSerializationService _json;
		public SitecoreResourceManagerRegistration(string roles, string isAdmin, string users, string replaceExistingTemplates) : base(roles, isAdmin, users)
		{
			bool replaceExisting = replaceExistingTemplates.ToLower() == "true";
			_json = Bootstrap.Container.Resolve<IJsonSerializationService>();
			var templatesDir = GetInitialTemplatesDir();
			if (templatesDir == null)
				return;
			if (replaceExisting)
			{
				foreach (var file in Directory.EnumerateFiles(templatesDir))
				{
					if (File.Exists(GetDataDirectory() + $"\\Templates\\{Path.GetFileName(file)}"))
					{
						File.Delete(GetDataDirectory() + $"\\Templates\\{Path.GetFileName(file)}");
					}
					File.Copy(file, GetDataDirectory() + $"\\Templates\\{Path.GetFileName(file)}");
				}
			}
			else
			{
				Dictionary<string, string> defaultTemplates = Directory.EnumerateFiles(templatesDir).ToDictionary(Path.GetFileName);
				HashSet<string> existingTemplates = new HashSet<string>(Directory.EnumerateFiles(GetDataDirectory() + "\\Templates").Select(Path.GetFileName));
				foreach (var key in defaultTemplates.Keys.Where(x => !existingTemplates.Contains(x)))
				{
					File.Copy(defaultTemplates[key], GetDataDirectory() + $"\\Templates\\{key}");
				}
			}
		}

		public override string Identifier => "hg";
		public override string Directive => "hgmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "Sidekick.SitecoreResourceManager.Resources";
		public override Type Controller => typeof(SitecoreResourceManagerController);
		public override string Icon => "/scs/hg/resources/hgicon.png";
		public override string Name => "Resource Manager";
		public override string CssStyle => "min-width:600px;";
		public string GetDataDirectory()
		{
			string filepath;
			if (System.Text.RegularExpressions.Regex.IsMatch(Settings.DataFolder, @"^(([a-zA-Z]:\\)|(//)).*")) //if we have an absolute path, rather than relative to the site root
				filepath = Settings.DataFolder +
						   @"\SitecoreResourceManager";
			else
				filepath = HttpRuntime.AppDomainAppPath + Settings.DataFolder.Substring(1) +
						   @"\SitecoreResourceManager";
			if (!System.IO.Directory.Exists(filepath))
				System.IO.Directory.CreateDirectory(filepath);
			if (!System.IO.Directory.Exists(filepath+"\\Templates"))
				System.IO.Directory.CreateDirectory(filepath + "\\Templates");
			return filepath;
		}

		public string GetInitialTemplatesDir()
		{
			string path = HttpRuntime.AppDomainAppPath + @"\SitecoreResourceManager";
			if (Directory.Exists(path))
				return path;
			return null;
		}
		public PropertiesWrapper GetPropertiesWrapper(string template)
		{
			string templateDirectory = GetDataDirectory() + "\\Templates";
			string templatePath = Directory.GetFiles(templateDirectory).FirstOrDefault(x => Path.GetFileName(x) == template);
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
