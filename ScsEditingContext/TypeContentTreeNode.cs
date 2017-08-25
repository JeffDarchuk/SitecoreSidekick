using System;
using Sitecore.Data.Items;
using SitecoreSidekick.ContentTree;

namespace ScsEditingContext
{
	public class TypeContentTreeNode : ContentTreeNode
	{
		public string Type { get; set; }
		public TypeContentTreeNode(Item item) : base(item)
		{
			if (item == null)
			{
				return;
			}
			string tmp = item.Paths.FullPath;
			Type = tmp.Substring(10, tmp.IndexOf("/", 10, StringComparison.Ordinal) - 10);
		}
	}
}
