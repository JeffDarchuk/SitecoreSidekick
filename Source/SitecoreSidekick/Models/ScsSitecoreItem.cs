using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data.Items;

namespace SitecoreSidekick.Models
{
	public class ScsSitecoreItem
	{
		public string Id { get; set; }
		public ScsSitecoreItem Parent { get; set; }

		public ScsSitecoreItem()
		{
		}

		public ScsSitecoreItem(Item sitecoreItem)
		{
			Id = sitecoreItem.ID.ToString();
			Parent = sitecoreItem.Parent == null ? null : new ScsSitecoreItem(sitecoreItem.Parent);
		}
	}
}
