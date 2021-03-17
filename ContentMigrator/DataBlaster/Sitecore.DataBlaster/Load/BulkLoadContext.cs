using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Sitecore.Buckets.Util;
using Sitecore.ContentSearch;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
    /// <summary>
    /// Should be created a fresh for every bulk import action.
    /// </summary>
    public class BulkLoadContext
    {
        private ILog _log;
        public ILog Log
        {
            get { return _log; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _log = value;
            }
        }
        public string FailureMessage { get; private set; }

        public string Database { get; private set; }
       
        /// <summary>
        /// Stages data to temp tables, but don't merge it with existing data.
        /// Useful for debugging.
        /// </summary>
        public bool StageDataWithoutProcessing { get; set; }

        /// <summary>
        /// Whether to lookup item ids in database by item name or use the item id provided in the value of the field.
        /// </summary>
        public bool LookupItemIds { get; set; }

        /// <summary>
        /// Performance optimization when loading items into a specific repository (supports bucketing).
        /// </summary>
        public ItemReference Destination { get; private set; }

        /// <summary>
        /// Whether to lookup blob ids in database or use the blob id provided in the value of the field.
        /// </summary>
        public bool LookupBlobIds { get; set; }

        /// <summary>
        /// Whether bulk load allows template changes.
        /// Typically used during serialization.
        /// When set, provided bulk items should contain ALL fields and not partial data.
        /// </summary>
        public bool AllowTemplateChanges { get; set; }

        /// <summary>
        /// Forces updates in Sitecore database, so that all loaded items will have an item change.
        /// All modification dates will be reset. 
        /// </summary>
        public bool ForceUpdates { get; set; }

        /// <summary>
        /// Additional processing rules for fiels which will affect all items with those specified fields.
        /// </summary>
        public IList<FieldRule> FieldRules { get; set; }

        /// <summary>
        /// Will ensure bucket folder structure for items that are directly added to a parent that is a bucket.
        /// Be aware, this needs to do additional database reads while processing the item stream.
        /// </summary>
        public bool BucketIfNeeded { get; set; }
        
        /// <summary>
        /// Resolves the paths for items in buckets.
        /// </summary>
        public IDynamicBucketFolderPath BucketFolderPath { get; set; }

        /// <summary>
        /// Whether to remove updated items from Sitecore caches. Enabled by default.
        /// </summary>
        public bool RemoveItemsFromCaches { get; set; }

        /// <summary>
        /// Whether to update the history engine of Sitecore. This engine is e.g. used for index syncs.
        /// </summary>
        public bool? UpdateHistory { get; set; }

        /// <summary>
        /// Whether to update the publish queue of Sitecore. This queue is used for incremental publishing.
        /// </summary>
        public bool? UpdatePublishQueue { get; set; }

		/// <summary>
        /// Whether to update the link database.
        /// </summary>
        public bool? UpdateLinkDatabase { get; set; }

        /// <summary>
        /// Whether to update the indexes of Sitecore. Enabled by default.
        /// </summary>
        public bool UpdateIndexes { get; set; }

        private IList<ISearchIndex> _allIndexes;
        private IList<ISearchIndex> _indexesToUpdate;

        /// <summary>
        /// Which indexes to update, will be detected from database by default.
        /// </summary>
        public IList<ISearchIndex> IndexesToUpdate
        {
            get
            {
                if (_indexesToUpdate != null)
                    return _indexesToUpdate;

                // No specific indexes provided, fallback to all indexes for the current Database
                if (_allIndexes != null)
                    return _allIndexes;

	            _allIndexes = ContentSearchManager.Indexes
		            .Where(idx => idx.Crawlers
			        .OfType<SitecoreItemCrawler>()
			        .Any(c => Database.Equals(c.Database, StringComparison.OrdinalIgnoreCase)))
		            .ToList();
				return _allIndexes;
            }
            set { _indexesToUpdate = value; }
        }

        /// <summary>
        /// Threshold percentage to refresh destination in index instead of updating it one by one.
        /// </summary>
        public int? IndexRefreshThresholdPercentage { get; set; }

        /// <summary>
        /// Threshold percentage to rebuild index instead of updating it one by one.
        /// </summary>
        public int? IndexRebuildThresholdPercentage { get; set; }

        /// <summary>
        /// Data is staged in database but no changes are made yet.
        /// </summary>
        public Action<BulkLoadContext> OnDataStaged { get; set; }
        /// <summary>
        /// Data is loaded in database.
        /// </summary>
        public Action<BulkLoadContext> OnDataLoaded { get; set; }
        /// <summary>
        /// Data is indexed.
        /// </summary>
        public Action<BulkLoadContext, ICollection<ItemChange>> OnDataIndexed { get; set; }

        public LinkedList<ItemChange> ItemChanges { get; } = new LinkedList<ItemChange>();

        protected internal BulkLoadContext(string database)
        {
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException(nameof(database));

            Database = database;
            RemoveItemsFromCaches = true;
            UpdateIndexes = true;

            Log = LoggerFactory.GetLogger(typeof(BulkLoader));
        }

        public void LookupItemsIn(Guid itemId, string itemPath)
        {
            LookupItemIds = true;
            Destination = new ItemReference(itemId, itemPath);
        }

        public void LookupItemsIn(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            LookupItemsIn(item.ID.Guid, item.Paths.Path);
        }

        public bool ShouldUpdateIndex(ISearchIndex searchIndex)
        {
            // Always update when index has been explicitly set as to update
            if (_indexesToUpdate != null && _indexesToUpdate.Contains(searchIndex))
                return true;

            // Only rebuild when index is not empty
            return searchIndex.Summary.NumberOfDocuments > 0;
        }
        
        #region Stage results and feedback

        private readonly Dictionary<Stage, StageResult> _stageResults = new Dictionary<Stage, StageResult>();

        public bool AnyStageFailed => _stageResults.Any(x => x.Value.HasFlag(StageResult.Failed));

        protected virtual void AddStageResult(Stage stage, StageResult result)
        {
            StageResult r;
            r = _stageResults.TryGetValue(stage, out r)
                ? r | result
                : result;
            _stageResults[stage] = r;
        }

        public virtual void StageSucceeded(Stage stage)
        {
            AddStageResult(stage, StageResult.Succeeded);
        }

        public virtual void StageFailed(Stage stage, Exception ex, string message)
        {
            AddStageResult(stage, StageResult.Failed);

            if (ex == null)
                Log.Fatal(message);
            else
                Log.Fatal(message +
                    $"\nException type: {ex.GetType().Name}\nException message: {ex.Message}\nStack trace: {ex.StackTrace}");

            FailureMessage = message;
        }

        public virtual void StageFailed(Stage stage, string message)
        {
            StageFailed(stage, null, message);
        }

        public virtual void SkipItemWarning(string message)
        {
            Log.Warn(message + " Skipping item.");
        }

        public virtual void SkipItemDebug(string message)
        {
            Log.Debug(message + " Skipping item.");
        }

        #endregion

        #region Tracked item data

        /// <summary>
        /// Tracks path and template info of the bulk item within the context, 
        /// so that we can check whether bucketing is still needed, or do lookups by path.
        /// Doesn't keep a reference to the item, so that we're not too memory intensive.
        /// </summary>
        /// <param name="item">Item to attach.</param>
        public virtual void TrackPathAndTemplateInfo(BulkLoadItem item)
        {
            // Cache template id per item.
            var templateCache = GetTemplateCache();
            templateCache[item.Id] = item.TemplateId;

			// Cache path.
	        IDictionary<string, Guid> pathCache = null;
            if (!string.IsNullOrWhiteSpace(item.ItemPath))
            {
                pathCache = GetPathCache();
                pathCache[item.ItemPath] = item.Id;
            }

			// Cache lookup path.
	        if (!string.IsNullOrWhiteSpace(item.ItemLookupPath))
	        {
		        pathCache = pathCache ?? GetPathCache();
				pathCache[item.ItemLookupPath] = item.Id;
	        }
        }

        private IDictionary<string, Guid> GetPathCache()
        {
            return GetOrAddState("Transform.PathCache",
                () => new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));
        }

        private IDictionary<Guid, Guid> GetTemplateCache()
        {
            return GetOrAddState("Import.TemplateCache",
                () => new Dictionary<Guid, Guid>());
        }

        public virtual Guid? GetProcessedPath(string itemPath)
        {
            if (string.IsNullOrWhiteSpace(itemPath)) return null;

            var cache = GetPathCache();
            Guid id;
            return cache.TryGetValue(itemPath, out id) ? id : (Guid?)null;
        }

        public virtual Guid? GetProcessedItemTemplateId(Guid itemId)
        {
            var cache = GetTemplateCache();
            Guid id;
            return cache.TryGetValue(itemId, out id) ? id : (Guid?)null;
        }

        #endregion

        #region Additional state

        private readonly Dictionary<string, object> _state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets state from the context.
        /// </summary>
        /// <typeparam name="T">Type of the state.</typeparam>
        /// <param name="key">Key for the state.</param>
        /// <param name="defaultValue">Default value when state is not present.</param>
        /// <returns>Retrieved state or default value.</returns>
        /// <remarks>Not thread safe.</remarks>
        public T GetState<T>(string key, T defaultValue = default(T))
        {
            object state;
            if (!_state.TryGetValue(key, out state))
            {
                return defaultValue;
            }
            return (T)state;
        }

        /// <summary>
        /// Gets or adds new state to the context.
        /// </summary>
        /// <typeparam name="T">Type of the state.</typeparam>
        /// <param name="key">Key for the state.</param>
        /// <param name="stateFactory">Factory to create new state.</param>
        /// <returns>Retrieved or newly added state.</returns>
        /// <remarks>Not thread safe.</remarks>
        public T GetOrAddState<T>(string key, Func<T> stateFactory)
        {
            object state;
            if (!_state.TryGetValue(key, out state))
            {
                state = stateFactory();
                _state[key] = state;
            }
            return (T)state;
        }

        #endregion
    }
}