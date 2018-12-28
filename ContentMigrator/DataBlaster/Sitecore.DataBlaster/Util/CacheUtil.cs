using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.DataProviders.Sql;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util
{
	public class CacheUtil
	{
		public virtual void RemoveItemFromCaches(Database database, ID itemId, ID parentId, string itemPath = null)
		{
			if (database == null) throw new ArgumentNullException(nameof(database));
			if (itemId == (ID)null) throw new ArgumentNullException(nameof(itemId));
			if (parentId == (ID)null) throw new ArgumentNullException(nameof(parentId));

			// Parent needs to be cleared because it holds a reference to its children.
			var parentPath = itemPath?.Substring(0, itemPath.LastIndexOf("/", StringComparison.Ordinal));
			RemoveItemFromCaches(database, parentId, parentPath);

			RemoveItemFromCaches(database, itemId, itemPath);
		}

		public virtual void RemoveFromCaches(Item item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));

			RemoveItemFromCaches(item.Database, item.ID, item.ParentID, item.Paths.Path);
		}

		public virtual void RemoveItemsFromCachesInBulk(Database database, IEnumerable<Tuple<ID, ID, string>> itemIdParentIdAndItemPaths)
		{
			if (database == null) throw new ArgumentNullException(nameof(database));
			if (itemIdParentIdAndItemPaths == null) throw new ArgumentNullException(nameof(itemIdParentIdAndItemPaths));

			// In previous versions of this method, we did some crazy custom cache clearing to optimize performance.
			// Since 8.2 Sitecore now consistently supports cache key indexing,
			// so if you have performance issues with partial cache clears, please set enable following settings:
			//     <setting name="Caching.CacheKeyIndexingEnabled.ItemCache" value="true" />
			//     <setting name="Caching.CacheKeyIndexingEnabled.ItemPathsCache" value="true" />
			//     <setting name="Caching.CacheKeyIndexingEnabled.PathCache" value="true" />

			Parallel.ForEach(itemIdParentIdAndItemPaths,
				new ParallelOptions { MaxDegreeOfParallelism = (int)(Environment.ProcessorCount * 0.666) },
				x =>
				{
					RemoveItemFromCaches(database, x.Item1, x.Item2, x.Item3);
				});
		}
		
		protected virtual void RemoveItemFromCaches(Database database, ID itemId, string itemPath = null)
		{
			if (database == null) throw new ArgumentNullException(nameof(database));
			if (itemId == (ID)null) throw new ArgumentNullException(nameof(itemId));

			var dbCaches = database.Caches;

			// To clear the item paths cache we need an actual item, so we get on from the caches to avoid IO.
			var item = GetCachedItems(dbCaches, itemId).FirstOrDefault();

			dbCaches.DataCache.RemoveItemInformation(itemId);
			dbCaches.ItemCache.RemoveItem(itemId); // Will also remove the items from the filter cache per site.
			dbCaches.StandardValuesCache.RemoveKeysContaining(itemId.ToString());
			if (!string.IsNullOrWhiteSpace(itemPath))
				dbCaches.PathCache.RemoveKeysContaining(itemPath.ToLower());
			if (item != null)
				dbCaches.ItemPathsCache.InvalidateCache(item);

			var prefetch = CacheManager.FindCacheByName<ID>("SqlDataProvider - Prefetch data(" + database.Name + ")");
			prefetch?.Remove(itemId);
		}

		protected virtual IEnumerable<Item> GetCachedItems(DatabaseCaches caches, ID itemId)
		{
			if (caches == null) throw new ArgumentNullException(nameof(caches));
			if (itemId == (ID)null) throw new ArgumentNullException(nameof(itemId));

			var info = caches.DataCache.GetItemInformation(itemId);
			var languages = info?.GetLanguages();
			if (languages == null) yield break;

			foreach (var language in languages)
			{
				var versions = info.GetVersions(language);
				if (versions == null) continue;

				foreach (var version in versions)
				{
					var item = caches.ItemCache.GetItem(itemId, language, version);
					if (item != null) yield return item;
				}
			}
		}

		public virtual void ClearLanguageCache(Database database)
		{
			if (database == null) throw new ArgumentNullException(nameof(database));

			// Clear protected property 'Languages' on the SqlDataProvider for the database.
			var field = typeof(SqlDataProvider).GetProperty("Languages", BindingFlags.Instance | BindingFlags.NonPublic);
			Debug.Assert(field != null, "Protected field not found in Sitecore, did you change Sitecore version?");
			foreach (var provider in database.GetDataProviders().OfType<SqlDataProvider>())
			{
				field.SetValue(provider, null);
			}

			// Remove the database from the language cache.
			var cache = CacheManager.GetNamedInstance("LanguageProvider - Languages", Settings.Caching.SmallCacheSize, true);
			cache.Remove(database.Name);
		}
	}
}