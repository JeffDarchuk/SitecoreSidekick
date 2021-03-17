using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Sidekick.XConnect.Services;
using Sitecore.XConnect.Serialization;
using Sidekick.Core;

namespace Sidekick.XConnect
{
	public class XConnectController : ScsController
	{
		private readonly IModelClassResolver _modelResolver;
		public XConnectController()
		{
			_modelResolver = Bootstrap.Container.Resolve<IModelClassResolver>();
		}
		[ActionName("xcgetmodels.scsvc")]
		public ActionResult GetModels()
		{
			return ScsJson(_modelResolver.GetAllModels().Select(x => new {name=x.Name, version=x.Version.ToString(), fullName=x.DeclaredTypes.Select(f => f.FullName)} ));
		}
		[ActionName("xcdownloadmodel.scsvc")]
		public ActionResult DownloadModel()
		{
			var name = Request.QueryString["name"];
			var model = _modelResolver.GetModelByName(name);
			if (model == null) throw new Exception($"Not able to find facet model with name {name}");
			return File(GenerateStreamFromString(XdbModelWriter.Serialize(model)), "application/json", $"{name}, {model.Version}.json");
		}
		[ActionName("xcdownloadconfig.scsvc")]
		public ActionResult DownloadConfig()
		{
			var name = Request.QueryString["name"];
			var model = _modelResolver.GetModelByName(name);
			if (model == null) throw new Exception($"Not able to find facet model with name {name}");
			var fullNamespace = string.Join(",", _modelResolver.GetModelType(name)?.AssemblyQualifiedName?.Split(',').Take(2) ?? throw new InvalidOperationException($"Unable to resolve the {name} model's fully qualified domain name"));
			return File(GenerateStreamFromString(string.Format(Constants.ConfigFormat, name, fullNamespace)), "text/xml", $"{name}, {model.Version}.config");
		}
		private static Stream GenerateStreamFromString(string s)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}
	}
}
