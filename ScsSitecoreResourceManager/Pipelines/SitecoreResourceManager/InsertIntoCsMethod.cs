using System;
using System.IO;
using System.Linq;

namespace Sidekick.SitecoreResourceManager.Pipelines.SitecoreResourceManager
{

	public class InsertIntoCsMethod
	{
		private readonly string _fileName;
		private readonly string _template;
		private readonly string _methodName;
		private readonly bool _insertAtEnd;
		private readonly string _applicableTemplateZip;

		public InsertIntoCsMethod(string fileName, string template, string methodName, string insertAtEnd, string applicableTemplateZip)
		{
			_fileName = fileName;
			_template = template;
			_methodName = methodName;
			_insertAtEnd = insertAtEnd.ToLower() == "true";
			_applicableTemplateZip = applicableTemplateZip;
		}

		public void Process(SitecoreResourceManagerArgs args)
		{
			if (Path.GetFileName(args.Wrapper.TemplateZip.ToLower()) != _applicableTemplateZip.ToLower())
				return;
			var fileName = ReplaceAllTokens.ReplaceTokens(_fileName, args);
			var template = ReplaceAllTokens.ReplaceTokens(_template, args);
			var methodName = ReplaceAllTokens.ReplaceTokens(_methodName, args);
			
			var path = Directory.EnumerateFiles(args.OverlayTarget, $"*{fileName}*", SearchOption.AllDirectories).FirstOrDefault(x => (Path.GetFileName(x)?.ToLower() ?? string.Empty) == fileName.ToLower());
			if (path == null)
				return;
			var content = File.ReadAllText(path);
			int index = content.IndexOf(methodName, StringComparison.Ordinal);
			if (index == -1)
				return;
			int brace = content.IndexOf("{", index, StringComparison.Ordinal);
			int semi = content.IndexOf(";", index, StringComparison.Ordinal);
			while (semi < brace)
			{
				index = content.IndexOf(methodName, index, StringComparison.Ordinal);
				if (index == -1)
					return;
				brace = content.IndexOf("{", index, StringComparison.Ordinal);
				semi = content.IndexOf(";", index, StringComparison.Ordinal);
			}

			if (_insertAtEnd)
			{
				int tracker = 0;
				for (;brace < content.Length; brace++)
				{
					if (content[brace] == '{')
					{
						tracker++;
					}else if (content[brace] == '}')
					{
						tracker--;
					}
					if (tracker == 0)
						break;
				}
				if (tracker != 0)
					return;
				brace--;
			}
			content = content.Insert(brace, template);
			File.WriteAllText(path, content);
		}
	}
}
