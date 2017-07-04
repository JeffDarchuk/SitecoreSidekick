using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;

namespace SitecoreSidekick.ContentTree
{
	public class ContentTreeNode
	{
		public string Icon = "";
		public string AltIcon = "";
		public string FallbackIcon = "/scs/platform/resources/scsphoto_scenery.png";
		public string DisplayName;
		public string DatabaseName;
		public string Id;
		public bool Open;
		public List<ContentTreeNode> Nodes;
		public string Server;

		public ContentTreeNode()
		{
		}

		public ContentTreeNode(string id) : this(Factory.GetDatabase("master", false)?.GetItem(id), false)
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
				Icon = GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", ""));
				AltIcon = Icon.Replace("/sitecore/shell/themes/standard/-/media/", "/-/media/");
			}
			//if (!string.IsNullOrWhiteSpace(Icon))
			//{
			//	string[] parts = Icon.Split('/');
			//	Icon = string.Join("/", parts.Skip(parts.Length - 3));
			//}
		}
		private string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
	}
}
