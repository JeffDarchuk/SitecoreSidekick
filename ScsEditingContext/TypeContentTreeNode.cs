using System;
using Rainbow.Model;
using Sitecore.Data.Items;
using SitecoreSidekick.ContentTree;

namespace ScsEditingContext
{
	public class TypeContentTreeNode : ContentTreeNode
	{
		public string Type { get; set; }
		public TypeContentTreeNode(IItemData item) : base(item)
		{
			if (item == null)
			{
				return;
			}

			string tmp = item.Path;
			try
			{
				Type = tmp.Substring(10, tmp.IndexOf("/", 10, StringComparison.Ordinal) - 10);
			}
			catch (ArgumentOutOfRangeException)
			{
				Type = tmp.Substring(10, tmp.Length - 10);
			}
		}
	}
}
