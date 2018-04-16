using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager;

namespace ScsSitecoreResourceManager.Pipelines.PropertyProcessorPostGather
{
	public class PropertyLayeredTokenReplacer
	{
		public void Process(SitecoreResourceManagerArgs args)
		{
			for (int i = 0; i < 4; i++)
			{
				foreach (var prop in args.GetAllProperties())
				{
					args[prop.Key] = ReplaceAllTokens.ReplaceTokens(prop.Value, args, prop.Key);
				}
			}
		}
	}
}
