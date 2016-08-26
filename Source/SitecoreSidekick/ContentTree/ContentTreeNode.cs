using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;

namespace SitecoreSidekick.ContentTree
{
	public class ContentTreeNode
	{
		public string Icon = "";
		public string DisplayName;
		public string DatabaseName;
		public string Id;
		public bool Open;
		public List<ContentTreeNode> Nodes;

		public ContentTreeNode()
		{
		}

		public ContentTreeNode(Item item, bool open = true)
		{
			if (item != null)
			{
				DatabaseName = item.Database.Name;
				Open = open;
				SetIcon(item);
				DisplayName = item.DisplayName;
				Id = item.ID.ToString();
				if (Open)
					Nodes = item.Children.Select(c => new ContentTreeNode(c, false)).ToList();
			}
		}

		public void SetIcon(Item item)
		{
			if (item != null)
			{

				Icon = null;
				Icon = item[FieldIDs.Icon];
				if ((Icon.IsNullOrEmpty() || Icon.StartsWith("-") || Icon.StartsWith("~")) && item.Template != null)
				{
					Icon = item.Template.Icon;
				}
			}
			if (!string.IsNullOrWhiteSpace(Icon))
			{
				string[] parts = Icon.Split('/');
				Icon = string.Join("/", parts.Skip(parts.Length - 3));
			}
		}
	}
}
