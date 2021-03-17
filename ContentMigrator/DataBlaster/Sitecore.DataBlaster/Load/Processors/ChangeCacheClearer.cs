using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util;
using Sitecore.Configuration;
using Sitecore.Data;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public class ChangeCacheClearer : IChangeProcessor
    {
	    private readonly CacheUtil _cachUtil;

	    public ChangeCacheClearer(CacheUtil cachehUtil = null)
	    {
		    _cachUtil = cachehUtil ?? new CacheUtil();
	    }

        public void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, ICollection<ItemChange> changes)
        {
            if (!loadContext.RemoveItemsFromCaches) return;

            var stopwatch = Stopwatch.StartNew();

            // Remove items from database cache.
            // We don't do this within the transaction so that items will be re-read from the committed data.
            var db = Factory.GetDatabase(loadContext.Database, true);
            _cachUtil.RemoveItemsFromCachesInBulk(db, GetCacheClearEntries(loadContext.ItemChanges));

            loadContext.Log.Info($"Caches cleared: {(int)stopwatch.Elapsed.TotalSeconds}s");
        }

        protected virtual IEnumerable<Tuple<ID, ID, string>> GetCacheClearEntries(IEnumerable<ItemChange> itemChanges)
        {
            if (itemChanges == null) yield break;

            // Since we don't include the language in the cache clear entries,
            // we should de-duplicate the ItemChanges by language first to avoid duplicate cache clear entries.
            // Result should be one cache-clear entry per Item, not per ItemChange.
            var seenKeys = new HashSet<string>();
            var filteredItemChanges = itemChanges.Where(x => seenKeys.Add($"{x.ItemId}{x.ParentId}{x.OriginalParentId}{x.ItemPath}"));

            foreach (var itemChange in filteredItemChanges)
            {
                yield return new Tuple<ID, ID, string>(ID.Parse(itemChange.ItemId), ID.Parse(itemChange.ParentId), itemChange.ItemPath);

                // Support moved items.
                if (itemChange.ParentId != itemChange.OriginalParentId)
                    yield return new Tuple<ID, ID, string>(ID.Parse(itemChange.ItemId), ID.Parse(itemChange.OriginalParentId), itemChange.ItemPath);
            }
        }
    }
}