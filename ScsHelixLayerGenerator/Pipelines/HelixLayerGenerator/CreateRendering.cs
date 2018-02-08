using Sitecore.Configuration;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Pipelines.HelixLayerGenerator
{
	public class CreateRendering
	{
		public void Process(HelixLayerGeneratorArgs args)
		{
			var folderId = args.Properties["_RENDERINGFOLDER_"].Value;
			var renderingPath = args.Properties["_RENDERINGPATH_"].Value;
			var db = Factory.GetDatabase("master", false);
			Item folder = db.GetItem(renderingPath);
		}

	}
}
