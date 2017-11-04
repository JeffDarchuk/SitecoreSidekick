using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using ScsEditingContext.Services.Interface;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Version = Sitecore.Data.Version;

namespace ScsEditingContext.Services
{
	public class SitecoreDataAccessService : ISitecoreDataAccessService
	{
		private readonly Database _db = Context.ContentDatabase ?? Context.Database ?? Factory.GetDatabase("master");

		public bool TryGetItemData(string id, out IItemData itemData)
		{
			itemData = null;

			Item item = GetItem(id);

			if (item == null)
				return false;

			itemData = new Rainbow.Storage.Sc.ItemData(item);
			return true;
		}

		public IItemData GetLatestItemData(string id, Database db = null)
		{
			var item = GetItem(id, db, LanguageManager.DefaultLanguage, Version.Latest);
			if (item == null)
				return null;
			return new Rainbow.Storage.Sc.ItemData(item);
		}

		public IEnumerable<IItemData> GetItemReferences(IItemData itemData)
		{
			Item item = GetItem(itemData.Id.ToString());

			return Globals.LinkDatabase.GetItemReferences(item, true).Select(x => new Rainbow.Storage.Sc.ItemData(x.GetTargetItem()));
		}

		public IEnumerable<IItemData> GetItemReferrers(IItemData itemData)
		{
			Item item = GetItem(itemData.Id.ToString());

			return Globals.LinkDatabase.GetItemReferences(item, true).Select(x => new Rainbow.Storage.Sc.ItemData(x.GetSourceItem()));
		}

		private Item GetItem(string id, Database db = null, Language language = null, Version version = null)
		{
			if (db == null) db = _db;
			ID tmp;
			try
			{
				tmp = new ID(id);
			}
			catch (Exception)
			{
				return null;
			}
			Item item;
			using (new SecurityDisabler())
			{
				if (language == null || version == null)
					item = db.GetItem(tmp);
				else
					item = db.GetItem(tmp, language, version);
			}

			return item;
		}
	}
}
