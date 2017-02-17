using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data.Items;
using SitecoreSidekick.ContentTree;

namespace ScsEditingContext
{
	public class TypeContentTreeNode : ContentTreeNode
	{
		public string Type { get; set; }
		public TypeContentTreeNode(Item item) : base(item)
		{
			string tmp = item.Paths.FullPath;
			Type = tmp.Substring(10, tmp.IndexOf("/", 10, StringComparison.Ordinal)-10);
		}
	}
}
