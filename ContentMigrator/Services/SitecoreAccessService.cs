using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using ScsContentMigrator.Services.Interface;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.SecurityModel;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace ScsContentMigrator.Services
{
	public class SitecoreAccessService : ISitecoreAccessService
	{
		private Database db = Factory.GetDatabase("master", false);
		public IItemData GetItemData(Guid idataId)
		{
			var item = GetItem(idataId);
			if (item == null)
				return null;
			return new ItemData(item);
		}

		public string GetItemIconSrc(IItemData localData)
		{
			return GetSrc(ThemeManager.GetIconImage(GetItem(localData.Id), 32, 32, "", ""));
		}

		public ConcurrentHashSet<Guid> GetSubtreeOfGuids(IEnumerable<Guid> rootIds)
		{
			ConcurrentHashSet<Guid> ret = new ConcurrentHashSet<Guid>();
			Stack<Item> processing = new Stack<Item>(rootIds.Select(GetItem));
			while (processing.Any())
			{
				Item item = processing.Pop();
				ret.Add(item.ID.Guid);
				foreach (Item child in item.Children)
				{
					processing.Push(child);
				}
			}
			return ret;
		}

		public void RecycleItem(Guid itemId)
		{
			Item i = GetItem(itemId);
			i.Recycle();
		}
		public Item GetItem(Guid id)
		{
			using (new SecurityDisabler())
			{
				return db.GetItem(new ID(id));
			}
		}

		public List<Guid> GetChildrenIds(Guid guid)
		{
			return GetItem(guid).Children.Select(x => x.ID.Guid).ToList();
		}
		private string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
	}
}
