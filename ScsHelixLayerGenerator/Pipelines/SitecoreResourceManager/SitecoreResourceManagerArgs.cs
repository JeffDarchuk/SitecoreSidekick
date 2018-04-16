using ScsSitecoreResourceManager.Data;
using ScsSitecoreResourceManager.Data.Properties;
using ScsSitecoreResourceManager.Data.Properties.Collectors;
using ScsSitecoreResourceManager.Data.Targets;
using Sitecore.Pipelines;
using SitecoreSidekick.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class SitecoreResourceManagerArgs : DefaultPropertiesArgs
	{
		public PropertiesWrapper Wrapper { get; set; }
		public IEnumerable<string> NewOverlayFiles { get; set; }
		public IEnumerable<string> NewOverlayDirectories { get; set; }

		public string GeneratedTemplateId { get; set; }
		public string GeneratedRenderingId { get; set; }
		public List<string> EventLog { get; set; } = new List<string>();
		public SitecoreResourceManagerArgs(Dictionary<string,DefaultCollector> properties, PropertiesWrapper wrapper):base(properties)
		{
			
			Wrapper = wrapper;
		}

		public Dictionary<string, object> Output => new Dictionary<string, object>()
		{
			{ "template", GeneratedTemplateId},
			{ "rendering", GeneratedRenderingId},
			{ "events", EventLog }
		};
	}
}
