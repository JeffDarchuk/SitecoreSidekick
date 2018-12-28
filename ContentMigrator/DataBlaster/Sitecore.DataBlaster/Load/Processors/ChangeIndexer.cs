using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Events;
using Sitecore.Globalization;
using Sitecore.Jobs;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public class ChangeIndexer : IChangeProcessor
    {
	    private static readonly Guid BucketFolderTemplate = Guid.Parse(global::Sitecore.Buckets.Util.Constants.BucketFolder);

		public virtual void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, ICollection<ItemChange> changes)
        {
            if (!loadContext.UpdateIndexes) return;
            if (loadContext.IndexesToUpdate == null || loadContext.IndexesToUpdate.Count == 0) return;

            // We don't index bucket folders.
            changes = changes?.Where(x => x.TemplateId != BucketFolderTemplate)?.ToList();
            if (changes == null || changes.Count == 0) return;

            var stopwatch = Stopwatch.StartNew();

            UpdateIndexes(loadContext, changes);
            loadContext.Log.Info($"Updated content search indexes: {(int)stopwatch.Elapsed.TotalSeconds}s");

            loadContext.OnDataIndexed?.Invoke(loadContext, changes);
            Event.RaiseEvent("bulkloader:dataindexed", loadContext);
        }

        protected virtual void UpdateIndexes(BulkLoadContext context, ICollection<ItemChange> itemChanges)
        {
            var db = Factory.GetDatabase(context.Database, true);

            foreach (var index in context.IndexesToUpdate)
            {
                UpdateIndex(context, itemChanges, db, index);
            }
        }

        protected virtual void UpdateIndex(BulkLoadContext context, ICollection<ItemChange> itemChanges, 
            Database database, ISearchIndex index)
        {
            Job job = null;

            if (!context.ShouldUpdateIndex(index))
            {
                context.Log.Warn($"Skipping updating index '{index.Name}' because its empty.");
                return;
            }

            var touchedPercentage = (uint)Math.Ceiling((double)itemChanges.Count / Math.Max(1, index.Summary.NumberOfDocuments) * 100);
            if (context.IndexRebuildThresholdPercentage.HasValue
                && touchedPercentage > context.IndexRebuildThresholdPercentage.Value)
            {
                context.Log.Info($"Rebuilding index '{index.Name}' because {touchedPercentage}% is changed.");
                job = IndexCustodian.FullRebuild(index);
            }
            else if (context.Destination != null
                     && !itemChanges.Any(ic => ic.Deleted)   // Refresh doesn't do deletes.
                     && context.IndexRefreshThresholdPercentage.HasValue
                     && touchedPercentage > context.IndexRefreshThresholdPercentage.Value)
            {
                context.Log.Info($"Refreshing index '{index.Name}' from '{context.Destination.ItemPath}' because {touchedPercentage}% is changed.");
                job = IndexCustodian.Refresh(index, new SitecoreIndexableItem(database.GetItem(new ID(context.Destination.ItemId))));
            }
            else
            {
                var sitecoreIds = GetItemsToIndex(itemChanges, database);
                context.Log.Info($"Updating index '{index.Name}' with {sitecoreIds.Count} items.");
                job = IndexCustodian.IncrementalUpdate(index, sitecoreIds);
            }
            job.Wait();
        }

        protected virtual IList<SitecoreItemUniqueId> GetItemsToIndex(ICollection<ItemChange> itemChanges, Database db)
        {
            var identifiers = new List<SitecoreItemUniqueId>(itemChanges.Count);

            // The SitecoreItemCrawler has a bad habit of updating *all* languages of an item
            // when asked to index a single language. However, it will not take care de-indexing
            // languages for which the item does not exist (any more). For those, we need to send
            // the exact identifier for the version that needs to be removed from the index.
            //      See: Crawler<T>.Update()
            //           SitecoreItemCrawler.DoUpdate()

            // So, the strategy here is to separate index 'update' from 'remove' requests.
            // For deletes, *all* item changes need to be registered.
            // for upserts, it is sufficient to supply one existing entry.

            identifiers.AddRange(itemChanges
                .Where(ic => ic.Deleted)
                .Select(ic =>
                {
                    var language = !string.IsNullOrEmpty(ic.Language) ? Language.Parse(ic.Language) : LanguageManager.DefaultLanguage;
                    var version = ic.Version.HasValue ? global::Sitecore.Data.Version.Parse(ic.Version.Value) : global::Sitecore.Data.Version.First;
                    return new SitecoreItemUniqueId(new ItemUri(new ID(ic.ItemId), language, version, db));
                }));

            identifiers.AddRange(itemChanges
                .Where(ic => !ic.Deleted)
                .GroupBy(x => x.ItemId)
                .Select(g =>
                {
                    // Find an entry whose language and version is not empty/unknown (= look for versioned field changes).
                    var ic = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.Language) && x.Version.HasValue)
                             ?? g.FirstOrDefault(x => !string.IsNullOrEmpty(x.Language));

                    var language = ic != null ? Language.Parse(ic.Language) : LanguageManager.DefaultLanguage;
                    var version = ic != null && ic.Version.HasValue
                        ? global::Sitecore.Data.Version.Parse(ic.Version.Value)
                        : global::Sitecore.Data.Version.First; // Take first (1), not latest (0).

                    return new SitecoreItemUniqueId(new ItemUri(new ID(g.Key), language, version, db));
                }));

            return identifiers;
        }
    }
}