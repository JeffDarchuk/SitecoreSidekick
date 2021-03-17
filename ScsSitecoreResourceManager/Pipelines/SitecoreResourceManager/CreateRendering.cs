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
using Sitecore.Diagnostics;

namespace Sidekick.SitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class CreateRendering
	{
		public const string ControllerRenderingTemplateId = "{2A3E91A0-7987-44B5-AB34-35C2D9DE83B9}";
		public const string ViewRenderingTemplateId = "{99F8905D-4A87-4EB8-9F8B-A9BEBFB3ADD6}";
		public void Process(SitecoreResourceManagerArgs args)
		{
			if (args.RenderingFolderTemplateId == null || args.RenderingPath == null)
				return;
			var folderId = args.RenderingFolderTemplateId;
			var renderingPath = args.RenderingPath;
			var db = Factory.GetDatabase("master", false);
			Item folder = db.GetItem(renderingPath);
			if (folder == null)
			{
				var parts = renderingPath.Split('/');
				for (int i = 1; i < parts.Length; i++)
				{
					folder = db.GetItem(string.Join("/", parts.Take(parts.Length - i)));
					if (folder != null)
					{
						using (new SecurityDisabler())
						{
							for (int k = i; k > 0; k--)
							{
								folder = folder.Add(parts[parts.Length - k], new TemplateID(new ID(folderId)));
							}
						}
						break;
					}
				}
			}

			if (folder == null)
			{
				Log.Error($"Unable to create rendering folder:{folderId} path:{renderingPath}", this);
				return;
			}
			var rendering = folder.Add(args.RenderingName, new TemplateID(new ID(args.RenderingTemplateId)));
			using (new SecurityDisabler())
			{
				using (new EditContext(rendering)) {
					if (rendering.TemplateID.ToString() == ControllerRenderingTemplateId)
					{
						rendering["Controller"] = $"{args.ControllerNamespace}, {args.AssemblyName}";
						rendering["Controller Action"] = args.ControllerAction;
					}else if (rendering.TemplateID.ToString() == ViewRenderingTemplateId)
					{
						rendering["Path"] = args.ViewPath;
					}
					rendering["Cacheable"] = args.CacheOptions.Contains("Cacheable") ? "1" : "";
					rendering["ClearOnIndexUpdate"] = args.CacheOptions.Contains("Clear on Index Update") ? "1" : "";
					rendering["VaryByData"] = args.CacheOptions.Contains("Vary By Data") ? "1" : "";
					rendering["VaryByDevice"] = args.CacheOptions.Contains("Vary By Device") ? "1" : "";
					rendering["VaryByLogin"] = args.CacheOptions.Contains("Vary By Login") ? "1" : "";
					rendering["VaryByParm"] = args.CacheOptions.Contains("Vary By Parm") ? "1" : "";
					rendering["VaryByQueryString"] = args.CacheOptions.Contains("Vary By Query String") ? "1" : "";
					rendering["VaryByUser"] = args.CacheOptions.Contains("Vary By User") ? "1" : "";
					rendering[FieldIDs.Icon] = args.SitecoreIcon;
					rendering["Datasource Template"] = args.GeneratedTemplateId;
					rendering["Datasource Location"] = args.RenderingDatasourceLocation;
				}
			}
			args["_GENERATEDRENDERINGID_"] = rendering.ID.ToString();
			args.EventLog.Add($"Creating new rendering {args.GeneratedRenderingId}");
		}
	}
}
