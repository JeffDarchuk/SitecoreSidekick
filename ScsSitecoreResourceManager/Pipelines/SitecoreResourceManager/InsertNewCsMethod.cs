using System;
using System.IO;
using System.Linq;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{

	public class InsertNewCsMethod
	{
		private readonly string _fileName;
		private readonly string _template;
		private readonly string _applicableTemplateZip;

		public InsertNewCsMethod(string fileName, string template, string applicableTemplateZip)
		{
			_fileName = fileName;
			_template = template;
			_applicableTemplateZip = applicableTemplateZip;
		}

		public void Process(SitecoreResourceManagerArgs args)
		{
			if (Path.GetFileName(args.Wrapper.TemplateZip.ToLower()) != _applicableTemplateZip.ToLower())
				return;
			var fileName = ReplaceAllTokens.ReplaceTokens(_fileName, args);
			var template = ReplaceAllTokens.ReplaceTokens(_template, args);
			
			var path = Directory.EnumerateFiles(args.OverlayTarget, $"*{fileName}*", SearchOption.AllDirectories).FirstOrDefault(x => (Path.GetFileName(x)?.ToLower() ?? string.Empty) == fileName.ToLower());
			if (path == null)
				return;
			var content = File.ReadAllText(path);
			int index = content.LastIndexOf('}');
			if (index > -1)
				index--;
			index = content.LastIndexOf('}', index);
			content = content.Insert(index, template);
			File.WriteAllText(path, content);
		}
	}
}
