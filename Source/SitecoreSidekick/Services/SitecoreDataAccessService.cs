using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using SitecoreSidekick.Models;
using SitecoreSidekick.Services.Interface;
using ItemData = Rainbow.Storage.Sc.ItemData;
using Version = Sitecore.Data.Version;

namespace SitecoreSidekick.Services
{
	public class SitecoreDataAccessService : ISitecoreDataAccessService
	{
		private readonly Database _db = Factory.GetDatabase("master", false);

		public ScsSitecoreItem GetScsSitecoreItem(string id)
		{
			Item item = _db.GetItem(id);

			return new ScsSitecoreItem(item);
		}

		public IItemData GetLatestItemData(string idataId, string database = null) => GetLatestItemData(new ID(idataId), database);
		public IItemData GetLatestItemData(Guid idataId, string database = null) => GetLatestItemData(new ID(idataId), database);
		private IItemData GetLatestItemData(ID id, string database = null)
		{
			var item = GetItem(id, database, LanguageManager.DefaultLanguage, Version.Latest);
			if (item == null)
				return null;
			return new ItemData(item);
		}

		public IItemData GetItemData(string idataId, string database = null) => GetItemData(new ID(idataId), database);
		public IItemData GetItemData(Guid idataId, string database = null) => GetItemData(new ID(idataId), database);
		private IItemData GetItemData(ID id, string database = null)
		{
			var item = GetItem(id, database);
			if (item == null)
				return null;
			return new ItemData(item);
		}

		public IItemData GetRootItemData(string database = null)
		{
			Database db = database == null ? _db : Factory.GetDatabase(database);

			using (new SecurityDisabler())
			{
				return new ItemData(db.GetRootItem());
			}
		}

		public List<Guid> GetChildrenIds(Guid guid)
		{
			return GetItem(guid).Children.Select(x => x.ID.Guid).ToList();
		}

		public IEnumerable<string> GetVersions(IItemData itemData)
		{
			Item item = GetItem(itemData.Id);
			return item.Versions.GetVersions(true).Select(v => v[FieldIDs.Revision]);
		}

		public HashSet<Guid> GetSubtreeOfGuids(IEnumerable<Guid> rootIds)
		{
			HashSet<Guid> ret = new HashSet<Guid>();
			Stack<Item> processing = new Stack<Item>(rootIds.Select(x=>GetItem(x)));
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

		public string GetIconSrc(IItemData item, int width = 32, int height = 32, string align = "", string margin = "")
		{			
			return ThemeManager.GetIconImage(GetItem(item.Id), width, height, align, margin);
		}

		public void RecycleItem(string id) => RecycleItem(new ID(id));
		public void RecycleItem(Guid id) => RecycleItem(new ID(id));
		private void RecycleItem(ID id)
		{
			Item item = GetItem(id);
			item?.Recycle();
		}
		
		public string GetItemYaml(string idataId, Func<object, string> serializationFunc) => GetItemYaml(new ID(idataId), serializationFunc);
		public string GetItemYaml(Guid idataId, Func<object, string> serializationFunc) => GetItemYaml(new ID(idataId), serializationFunc);
		private string GetItemYaml(ID idataId, Func<object, string> serializationFunc)
		{
			var item = GetItem(idataId);
			if (item == null)
				return null;
			return serializationFunc?.Invoke(item);			
		}

		private Item GetItem(string id, string database = null, Language language = null, Version version = null) =>
			GetItem(new ID(id), database, language, version);

		private Item GetItem(Guid id, string database = null, Language language = null, Version version = null) =>
			GetItem(new ID(id), database, language, version);

		private Item GetItem(ID id, string database = null, Language language = null, Version version = null)
		{
			Database db = database == null ? _db : Factory.GetDatabase(database, true);
			using (new SecurityDisabler())
			{
				if (language == null || version == null)
				{
					return db.GetItem(id);
				}
				else
				{
					return db.GetItem(id, language, version);
				}
			}
		}
	}
}
