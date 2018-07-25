using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScsSitecoreResourceManager.Data.Properties.Collectors;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetAboutInformation;

namespace ScsSitecoreResourceManager.Data.Properties
{
	public class DefaultPropertiesArgs : PipelineArgs
	{
		private const string delimiterRegex = "\\|(.*)\\|";
		private Dictionary<string, DefaultCollector> _storage;
		public DefaultPropertiesArgs(Dictionary<string, DefaultCollector> properties)
		{
			_storage = new Dictionary<string, DefaultCollector>(properties);
		}

		public IEnumerable<KeyValuePair<string, string>> GetAllProperties()
		{

			return new HashSet<KeyValuePair<string,string>>(_storage.Keys.Select(x => new KeyValuePair<string, string>(x, _storage[x].Value)));
		}
		public string this[string key]
		{
			get => GetValue(key);
			set => _storage[key] = new DefaultCollector(){Id = key, Value = value};
		} 

		// AddToExistingController
		public string TargetControllerPath => GetValue("_TARGETCONTROLLERPATH_")?.Split(new []{"<||>"}, StringSplitOptions.None)[1];
		public string TargetCsProjPath => GetValue("_TARGETCSPROJ_") ?? GetValue("_TARGETCONTROLLERPATH_")?.Split(new[] { "<||>" }, StringSplitOptions.None)[0];
		public string ControllerAction => GetValue("_CONTROLLERACTION_");
		// ApplyProjectsToSolution
		public string SolutionPath => GetValue("_SOLUTIONPATH_");
		public string ProjectName => GetValue("_PROJECTNAME_");
		// ApplyToPlaceholderSettings
		public string PlaceholderSettings => GetValue("_PLACEHOLDERSETTINGS_");
		// CreateRendering
		public string RenderingFolderTemplateId => GetValue("_RENDERINGFOLDERTEMPLATEID_");
		public string RenderingPath => GetValue("_RENDERINGPATH_");
		public string RenderingName => GetValue("_RENDERINGNAME_");
		public string RenderingTemplateId => GetValue("_RENDERINGTEMPLATEID_");
		public string ControllerNamespace => GetValue("_CONTROLLERNAMESPACE_");
		public string CacheOptions => GetValue("_CACHEOPTIONS_");
		public string SitecoreIcon => GetValue("_SITECOREICON_");
		public string RenderingDatasourceLocation => GetValue("_RENDERINGDATASOURCELOCATION_");
		public string AssemblyName => GetValue("_ASSEMBLYNAME_");
		public string ActionFormat => GetValue("_ACTIONFORMAT_");

		public string ViewPath => GetValue("_VIEWPATH_");
		// CreateTemplate
		public string TemplateFolderTemplateId => GetValue("_TEMPLATEFOLDERTEMPLATEID_");
		public string TemplatePath => GetValue("_TEMPLATEPATH_");
		public string BaseTemplateId => GetValue("_BASETEMPLATEID_");
		public string TemplateName => GetValue("_TEMPLATENAME_");
		// OverlayTemplate
		public string OverlayTarget => GetValue("_OVERLAYTARGET_");
		public string Prefix => GetValue("_PREFIX_");
		public string Layer => GetValue("_LAYER_");

		public string GeneratedTemplateId => GetValue("_GENERATEDTEMPLATEID_");
		public string GeneratedRenderingId => GetValue("_GENERATEDRENDERINGID_");

		public bool ContainsKey(string key) {

			if (key.Contains('|'))
			{
				key = Regex.Replace(key, delimiterRegex, "");
			}

			return _storage.ContainsKey(key);
		}

		private string GetValue(string key)
		{
			if (key.Contains('|'))
			{
				string delimiter = "<||>";
				var match = Regex.Match(key, delimiterRegex);
				if (match.Success && match.Groups.Count > 1)
				{
					delimiter = match.Groups[1].Value;
					key = Regex.Replace(key, delimiterRegex, "");
				}

				return _storage.ContainsKey(key) ? string.Join(delimiter, _storage[key].Value.Split(new[] {"<||>"}, StringSplitOptions.RemoveEmptyEntries)) : null;
			}
			return _storage.ContainsKey(key) ? _storage[key].Value : null;
		}
	}
}
