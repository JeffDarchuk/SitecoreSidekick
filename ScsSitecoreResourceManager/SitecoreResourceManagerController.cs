using System.Web.Mvc;
using Sidekick.Core;
using Sitecore.Configuration;
using System.Web;
using System.IO;
using System.Linq;
using Sidekick.Core.Services.Interface;
using System.Collections.Generic;
using Sidekick.SitecoreResourceManager.Models;
using System.Text;
using Sidekick.SitecoreResourceManager.Services;
using Sidekick.SitecoreResourceManager.Data.Properties;
using Sidekick.SitecoreResourceManager.Data.Properties.Collectors;
using System;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Sitecore.Pipelines;
using Sidekick.SitecoreResourceManager.Pipelines.SitecoreResourceManager;
using Sidekick.Core;
using Sidekick.Core.ContentTree;

namespace Sidekick.SitecoreResourceManager
{
	class SitecoreResourceManagerController : ScsController
	{
		public const string SolutionProjectRegex = "\"([^\"]*.csproj)";
		public const string ControllerClassRegex = "\"(?i)([^\"]*controller.cs)";
		public IScsRegistrationService _registration;
		public IAssemblyScannerService _scanner;
		public ISavedPropertiesService _savedProps;
		public ISitecoreDataAccessService _sitecore;
		public SitecoreResourceManagerController()
		{
			_savedProps = Bootstrap.Container.Resolve<ISavedPropertiesService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_scanner = Bootstrap.Container.Resolve<IAssemblyScannerService>();
			_sitecore = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		}
		//The action name should match what the angular factory is calling, note that case sensitivity isn't an issue.
		[ActionName("hggettemplates.scsvc")]
		public ActionResult GetTemplates()
		{
			return ScsJson(Directory.EnumerateFiles(
				_registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetDataDirectory() + "\\Templates")
				.Select(Path.GetFileName)
				.Select(x =>
				{
					var wrapper = _registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetPropertiesWrapper(x);
					return new {wrapper.Name, wrapper.Description, Template = x};

				})
			);
		}
		[ActionName("hgdownloadtemplate.scsvc")]
		public ActionResult DownloadTemplate()
		{
			string file = Directory.EnumerateFiles(_registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetDataDirectory() + "\\Templates").FirstOrDefault(x => x.EndsWith(Request.QueryString["template"]));
			if (string.IsNullOrWhiteSpace(file)) return Content("Error, file not found");
			return File(System.IO.File.ReadAllBytes(file), "application/zip, application/octet-stream", Request.QueryString["template"]);
		}
		[ActionName("hgcontenttree.scsvc")]
		public ActionResult ContentTree(ContentTreeModel data)
		{
			return ScsJson(GetContentTree(data));
		}
		[ActionName("hguploadtemplate.scsvc")]
		public ActionResult UploadTemplate()
		{
			using (var fileStream = System.IO.File.Create(_registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetDataDirectory() + "\\Templates\\" + Request.Headers["X_FILENAME"]))
			{
				Request.InputStream.Seek(0, SeekOrigin.Begin);
				Request.InputStream.CopyTo(fileStream);
			}
			return Content("we're good");
		}
		[ActionName("hgremovetemplate.scsvc")]
		public ActionResult RemoveTemplate(string template)
		{
			if (template.Contains("\\") || template.Contains("/"))
				return Content("naughty!");
			System.IO.File.Delete(_registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetDataDirectory()+$"\\Templates\\{template}");
			return Content("we're good");
		}
		[ActionName("hggettargets.scsvc")]
		public ActionResult GetTargets(string template)
		{
			var targets = _registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetPropertiesWrapper(template).Targets;
			foreach(var target in targets.Keys)
			{
				foreach(var property in targets[target].Keys.ToArray())
				{
					if (targets[target][property] == "???")
					{
						targets[target][property + "-o"] = "???";
					}
					if (targets[target][property] == "???" && _savedProps[$"{target}.{property}", target] != null)
					{
						targets[target][property] = _savedProps[$"{target}.{property}", target];
					}
					if (targets[target][property] == "???" && _savedProps[$"{target}.{property}", template] != null)
					{
						targets[target][property] = _savedProps[$"{target}.{property}", template];
					}
				}
			}
			return ScsJson(targets);
		}

		[ActionName("hggetprojects.scsvc")]
		public ActionResult GetProjects(string solutionPath)
		{
			List<string> ret = new List<string>();
			if (solutionPath.EndsWith(".sln"))
			{
				string directory = Path.GetDirectoryName(solutionPath);
				var text = System.IO.File.ReadAllText(solutionPath);
				foreach (Match match in Regex.Matches(text, SolutionProjectRegex))
				{
					ret.Add($"{directory}\\{match.Groups[1].Value}");
				}
			}

			return ScsJson(ret);
		}
		[ActionName("hggetcontrollers.scsvc")]
		public ActionResult GetControllers(string projectPath)
		{
			List<string> ret = new List<string>();
			if (projectPath.EndsWith(".csproj"))
			{
				string directory = Path.GetDirectoryName(projectPath);
				var text = System.IO.File.ReadAllText(projectPath);
				foreach (Match match in Regex.Matches(text, ControllerClassRegex))
				{
					ret.Add($"{directory}\\{match.Groups[1].Value}");
				}
			}

			return ScsJson(ret);
		}
		[ActionName("hggetproperties.scsvc")]
		public ActionResult GetProperties(TargetModel model)
		{
			var collectors = _scanner.ScanForImplementsInterface<IPropertyCollector>().ToDictionary(x => x.Processor);
			var properties = _registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetPropertiesWrapper(model.Template);
			return ScsJson(properties.Properties.Select(x =>
			{
				IPropertyCollector ret;
				collectors.TryGetValue(x.Value.Processor, out ret);
				if (ret == null) return ret;
				ret.Description = x.Value.Description;
				ret.Name = x.Value.Name;
				ret.Processor = x.Value.Processor;
				ret.Id = x.Key;
				if (x.Value.Remember)
				{
					ret.Value = _savedProps[$"{model.Target}.{x.Key}", model.Template] ?? _savedProps[$"{model.Target}.{x.Key}", model.Target] ?? "";
					if (string.IsNullOrWhiteSpace(ret.Value))
					{
						ret.Value = x.Value.Default;
					}
				}
				else
				{
					ret.Value = x.Value.Default;
				}
				ret.Values = x.Value.Values;
				return ret;
			}).Where(x => x != null));
		}

		[ActionName("hgsubmittargetproperty.scsvc")]
		public ActionResult SubmitTargetProperty(SubmitTargetPropertyModel model)
		{
			_savedProps[$"{model.TargetId}.{model.PropertyId}", model.Template] = model.Value;
			_savedProps[$"{model.TargetId}.{model.PropertyId}", model.TargetId] = model.Value;
			return ScsJson(true);
		}
		[ActionName("hgexecute.scsvc")]
		public ActionResult Execute(ExecuteModel model)
		{
			
			var collectors = _scanner.ScanForImplementsInterface<IPropertyCollector>().ToDictionary(x => x.Processor);
			var propertiesWrapper = _registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetPropertiesWrapper(model.Template);
			var propertyDictionary = model.Properties.ToDictionary(x => x.Id);
			foreach (var prop in model.Properties)
			{
				if (propertyDictionary[prop.Id].Value == null)
					continue;
				if (!string.IsNullOrWhiteSpace(prop.Processor) && !collectors[prop.Processor].Validate(prop.Value))
				{
					return Content(prop.Name);
				}
				propertyDictionary[prop.Id].Value = ReplaceAllTokens.ReplaceTokens(propertyDictionary[prop.Id].Value, propertyDictionary.ContainsKey, x => propertyDictionary[x].Value);
				if (propertiesWrapper.Targets[model.Target].ContainsKey(prop.Id) && propertiesWrapper.Targets[model.Target][prop.Id] == "???")
				{
					_savedProps[$"{model.Target}.{prop.Id}", model.Template] = prop.Value;
				}
				if (propertiesWrapper.Properties.ContainsKey(prop.Id) && propertiesWrapper.Properties[prop.Id].Remember)
				{
					_savedProps[$"{model.Target}.{prop.Id}", model.Template] = prop.Value;
				}
			}
			var args = new SitecoreResourceManagerArgs(propertyDictionary, propertiesWrapper);
			var pipeline = CorePipelineFactory.GetPipeline("propertyProcessorPreCompiled", string.Empty);
			pipeline.Run(args);
			foreach (string compilePropertyKey in propertiesWrapper.CompiledProperties.Keys)
			{
				args[compilePropertyKey] = ReplaceAllTokens.ReplaceTokens(propertiesWrapper.CompiledProperties[compilePropertyKey], propertyDictionary.ContainsKey, x => propertyDictionary[x].Value);
			}
			pipeline = CorePipelineFactory.GetPipeline("propertyProcessorPostCompiled", string.Empty);
			pipeline.Run(args);
			pipeline = CorePipelineFactory.GetPipeline("SitecoreResourceManager", string.Empty);
			pipeline.Run(args);
			return ScsJson(args.Output);
		}
		[ActionName("hgicon.scsvc")]
		public ActionResult Icon()
		{
			string icon = Request.QueryString["icon"];
			if (IconCollector.Images.ContainsKey(icon))
			{
				return new FileStreamResult(IconCollector.Images[icon].Open(), "image/png");
			}
			return Content("nothing here");
		}
		private object GetContentTree(ContentTreeModel data)
		{
			if (string.IsNullOrWhiteSpace(data.Id)) return null;

			var item = _sitecore.GetItemData(data.Id, data.Database);
			return new ContentTreeNode(item);
		}
	}
}