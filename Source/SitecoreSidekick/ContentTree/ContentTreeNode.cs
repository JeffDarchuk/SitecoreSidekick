using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.SecurityModel;
using Sidekick.Core.Services.Interface;

namespace Sidekick.Core.ContentTree
{
	public class ContentTreeNode
	{
		private readonly ISitecoreDataAccessService _sitecoreDataAccessService;
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
			_sitecoreDataAccessService = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		}

		public ContentTreeNode(string id) : this()
		{
			Initialize(_sitecoreDataAccessService.GetItemData(id), false);			
		}

		public ContentTreeNode(IItemData item, bool open = true) : this()
		{			
			Initialize(item, open);
		}
	
		private void Initialize(IItemData item, bool open)
		{
			if (item == null) return;
			DatabaseName = item.DatabaseName;
			Open = open;
			SetIcon(item);
			DisplayName = item.Name;
			Id = item.Id.ToString();
			if (Open)
				using (new SecurityDisabler())
					Nodes = item.GetChildren().Select(c => new ContentTreeNode(c, false)).ToList();
		}

		public void SetIcon(IItemData item)
		{
			if (item == null) return;
			Icon = GetSrc(_sitecoreDataAccessService.GetIconSrc(item));
			AltIcon = Icon.Replace("/sitecore/shell/themes/standard/-/media/", "/-/media/");
		}

		private string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
	}
}
