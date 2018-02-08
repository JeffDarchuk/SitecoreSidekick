using System.Web.Mvc;
using SitecoreSidekick.Core;
using Sitecore.Configuration;
using System.Web;
using System.IO;
using System.Linq;
using SitecoreSidekick.Services.Interface;
using System.Collections.Generic;
using ScsHelixLayerGenerator.Models;
using System.Text;
using ScsHelixLayerGenerator.Services;
using ScsHelixLayerGenerator.Data.Properties;
using ScsHelixLayerGenerator.Data.Properties.Collectors;
using System;
using System.IO.Compression;
using Sitecore.Pipelines;
using ScsHelixLayerGenerator.Pipelines.HelixLayerGenerator;

namespace ScsHelixLayerGenerator
{
	class ScsHelixLayerGeneratorController : ScsController
	{
		public IScsRegistrationService _registration;
		public IAssemblyScannerService _scanner;
		public ISavedPropertiesService _savedProps;
		public ScsHelixLayerGeneratorController()
		{
			_savedProps = Bootstrap.Container.Resolve<ISavedPropertiesService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_scanner = Bootstrap.Container.Resolve<IAssemblyScannerService>();
		}
		//The action name should match what the angular factory is calling, note that case sensitivity isn't an issue.
		[ActionName("hggettemplates.scsvc")]
		public ActionResult GetTemplates()
		{
			return ScsJson(Directory.EnumerateFiles(_registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetDataDirectory() + "\\Templates").Select(x => Path.GetFileName(x)));
		}
		[ActionName("hgdownloadtemplate.scsvc")]
		public ActionResult DownloadTemplate()
		{
			string file = Directory.EnumerateFiles(_registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetDataDirectory() + "\\Templates").FirstOrDefault(x => x.EndsWith(Request.QueryString["template"]));
			if (string.IsNullOrWhiteSpace(file)) return Content("Error, file not found");
			return File(System.IO.File.ReadAllBytes(file), "application/zip, application/octet-stream", Request.QueryString["template"]);
		}
		[ActionName("hguploadtemplate.scsvc")]
		public ActionResult UploadTemplate()
		{
			using (var fileStream = System.IO.File.Create(_registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetDataDirectory() + "\\Templates\\" + Request.Headers["X_FILENAME"]))
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
			System.IO.File.Delete(_registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetDataDirectory()+$"\\Templates\\{template}");
			return Content("we're good");
		}
		[ActionName("hggettargets.scsvc")]
		public ActionResult GetTargets(string template)
		{
			var targets = _registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetPropertiesWrapper(template).Targets;
			foreach(var target in targets.Keys)
			{
				foreach(var property in targets[target].Keys.ToArray())
				{
					if (targets[target][property] == "???" && _savedProps[$"{target}.{property}", template] != null)
					{
						targets[target][property] = _savedProps[$"{target}.{property}", template];
					}
				}
			}
			return ScsJson(targets);
		}
		[ActionName("hggetproperties.scsvc")]
		public ActionResult GetProperties(string template)
		{
			var collectors = _scanner.ScanForImplementsInterface<IPropertyCollector>().ToDictionary(x => x.Processor);
			var properties = _registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetPropertiesWrapper(template);
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
					ret.Value = _savedProps[x.Key, template] ?? "";
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
		[ActionName("hgexecute.scsvc")]
		public ActionResult Execute(ExecuteModel model)
		{
			var collectors = _scanner.ScanForImplementsInterface<IPropertyCollector>().ToDictionary(x => x.Processor);
			var propertiesWrapper = _registration.GetScsRegistration<ScsHelixLayerGeneratorRegistration>().GetPropertiesWrapper(model.Template);
			HashSet<string> tracker = new HashSet<string>();
			var propertyDictionary = model.Properties.ToDictionary(x => x.Id);
			foreach (var prop in model.Properties)
			{
				if (!string.IsNullOrWhiteSpace(prop.Processor) && !collectors[prop.Processor].Validate(prop.Value))
				{
					return Content(prop.Name);
				}
				propertyDictionary[prop.Id].Value = ReplaceAllTokens.ReplaceTokens(propertyDictionary[prop.Id].Value, propertyDictionary);
				if (propertiesWrapper.Targets[model.Target].ContainsKey(prop.Id) && propertiesWrapper.Targets[model.Target][prop.Id] == "???")
				{
					_savedProps[$"{model.Target}.{prop.Id}", model.Template] = prop.Value;
				}
				if (propertiesWrapper.Properties.ContainsKey(prop.Id) && propertiesWrapper.Properties[prop.Id].Remember)
				{
					_savedProps[prop.Id, model.Template] = prop.Value;
				}
			}

			
			var pipeline = CorePipelineFactory.GetPipeline("helixLayerGenerator", string.Empty);
			pipeline.Run(new HelixLayerGeneratorArgs(propertyDictionary, propertiesWrapper) );
			return Content("");
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
	}
}