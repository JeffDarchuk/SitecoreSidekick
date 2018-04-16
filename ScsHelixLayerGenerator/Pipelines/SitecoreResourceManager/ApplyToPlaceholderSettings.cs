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

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class ApplyToPlaceholderSettings
	{
		public const string PlaceholderSettingsTemplateId = "{5C547D4E-7111-4995-95B0-6B561751BF2E}";
		public void Process(SitecoreResourceManagerArgs args)
		{

			var placeholderSettings = Factory.GetDatabase("master", false)?.GetItem(args.PlaceholderSettings);
			if (placeholderSettings != null && placeholderSettings.TemplateID.ToString() == PlaceholderSettingsTemplateId && !placeholderSettings["Allowed Controls"].Contains(args.GeneratedRenderingId))
			{
				using (new SecurityDisabler())
				using (new EditContext(placeholderSettings))
				{
					placeholderSettings["Allowed Controls"] = string.IsNullOrWhiteSpace(placeholderSettings["Allowed Controls"]) ? args.GeneratedRenderingId : placeholderSettings["Allowed Controls"]+ "|" + args.GeneratedRenderingId;
				}
				args.EventLog.Add($"Adding new rendering {args.GeneratedRenderingId} to the placeholder settings item {args.PlaceholderSettings}");
			}
		}

	}
}
