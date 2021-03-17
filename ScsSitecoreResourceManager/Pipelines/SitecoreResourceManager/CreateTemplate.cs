using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class CreateTemplate
	{
		public const string _standardTemplateId = "{AB86861A-6030-46C5-B394-E8F99E8B87DB}";
		public void Process(SitecoreResourceManagerArgs args)
		{
			if (args.TemplateFolderTemplateId == null || args.BaseTemplateId == null || args.TemplateName == null)
				return;
			var folderId = args.TemplateFolderTemplateId;
			var templatePath = args.TemplatePath;
			var db = Factory.GetDatabase("master", false);
			Item folder = db.GetItem(templatePath);
			if (folder == null)
			{
				var parts = templatePath.Split('/');
				for (int i = 1; i < parts.Length; i++)
				{
					folder = db.GetItem(string.Join("/", parts.Take(parts.Length - i)));
					if (folder != null)
					{
						using (new SecurityDisabler())
						{
							for (int k = i; k > 0; k--)
							{
								folder = folder.Add(parts[parts.Length-k], new TemplateID(new ID(folderId)));
							}
						}
						break;
					}
				}
			}
			string id = args.BaseTemplateId ?? _standardTemplateId;
			var template = folder.Add(args.TemplateName, new TemplateID(new ID(args.BaseTemplateId)));
			using (new SecurityDisabler())
			using (new EditContext(template))
			{
				template[FieldIDs.Icon] = args.SitecoreIcon;
			}
			args["_GENERATEDTEMPLATEID_"] = template.ID.ToString();
			args.EventLog.Add($"Creating new template {args.GeneratedTemplateId}");

		}
	}
}
