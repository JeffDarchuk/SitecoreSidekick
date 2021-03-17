using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Links;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql;
using Sitecore.Configuration;
using Sitecore.Data.SqlServer;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
	public class ItemLinker : IItemProcessor, IChangeProcessor
	{
		private readonly BulkItemLinkParser _itemLinkParser;

		public ItemLinker(BulkItemLinkParser itemLinkParser = null)
		{
			_itemLinkParser = itemLinkParser ?? new BulkItemLinkParser();
		}

	    public IEnumerable<BulkLoadItem> Process(BulkLoadContext context, IEnumerable<BulkLoadItem> items)
	    {
		    if (!context.UpdateLinkDatabase.GetValueOrDefault()) return items;

		    var links = GetItemLinksFromContext(context);
		    return _itemLinkParser.ExtractLinks(items, context, links);
	    }

	    public void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, ICollection<ItemChange> changes)
        {
            if (!loadContext.UpdateLinkDatabase.GetValueOrDefault()) return;
	        if (changes.Count == 0) return;

            var stopwatch = Stopwatch.StartNew();
            
            // Update link database, is in core database, so we can't do this within the transaction.
            // Links are detected when reading the bulk item stream, 
            // so we assume that the same set will be presented again after a crash.
            UpdateLinkDatabase(loadContext, sqlContext, GetItemLinksFromContext(loadContext), changes);

            loadContext.Log.Info($"Updated link database: {(int)stopwatch.Elapsed.TotalSeconds}s");
        }

		protected virtual LinkedList<BulkItemLink> GetItemLinksFromContext(BulkLoadContext context)
		{
			return context.GetOrAddState("Load.ExtractedLinks", () => new LinkedList<BulkItemLink>());
		}

		protected virtual void UpdateLinkDatabase(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, 
			LinkedList<BulkItemLink> links, ICollection<ItemChange> changes)
        {
            // Index all items that were actually changed in db.
            var touchedItemsMap = changes
                .GroupBy(x => x.ItemId)
                .ToDictionary(x => x.First().OriginalItemId, x => x.First().ItemId);

            // Get links and filter and map them by touched items.
            if (links.Count == 0) return;
            var linksForTouchedItems = links
                .Where(x => touchedItemsMap.ContainsKey(x.SourceItemID.Guid))
                .Select(x => x.Map(x.SourceItemID.Guid, touchedItemsMap[x.SourceItemID.Guid]));

            // Create temp table, bulk load temp table, merge records in link database.
            // The link database is probably not the same physical db as the one were loading data to.
            var createLinkTempTable = sqlContext.GetEmbeddedSql(loadContext, "Sql.20.CreateLinkTempTable.sql");
            var mergeLinkData = sqlContext.GetEmbeddedSql(loadContext, "Sql.21.MergeLinkTempData.sql");

            var connectionString = ((SqlServerLinkDatabase) Factory.GetLinkDatabase()).ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var linkSqlContext = new SqlContext(conn);

                linkSqlContext.ExecuteSql(createLinkTempTable);

                using (var bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.BulkCopyTimeout = int.MaxValue;
                    bulkCopy.EnableStreaming = true;
                    bulkCopy.DestinationTableName = sqlContext.PostProcessSql(loadContext, "#ItemLinks");

                    try
                    {
                        bulkCopy.WriteToServer(new ItemLinksReader(() => linksForTouchedItems.GetEnumerator()));
                    }
                    catch (Exception exception)
                    {
                        loadContext.StageFailed(Stage.Load, exception, $"Write to #ItemLinks failed with message: {exception.Message}");
                        return;
                    }
                }
                linkSqlContext.ExecuteSql(mergeLinkData);
            }
        }

        private class ItemLinksReader : AbstractEnumeratorReader<BulkItemLink>
        {
            private readonly object[] _fields;

            public override int FieldCount => 11;

            [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Not an issue in this case.")]
            public ItemLinksReader(Func<IEnumerator<BulkItemLink>> enumerator)
                : base(enumerator)
            {
                _fields = new object[FieldCount];
            }
            public override bool Read()
            {
                if (!base.Read())
                {
                    // Clear the fields.
                    for (var i = 0; i < _fields.Length; i++)
                        _fields[i] = null;
                    return false;
                }

                _fields[0] = Current.SourceDatabaseName;
                _fields[1] = Current.SourceItemID.Guid;
                _fields[2] = (object)Current.SourceItemLanguage?.Name ?? DBNull.Value;
                _fields[3] = (object)Current.SourceItemVersion?.Number ?? DBNull.Value;
                _fields[4] = Current.SourceFieldID.Guid;

                _fields[5] = Current.TargetDatabaseName;
                _fields[6] = Current.TargetItemID.Guid;
                _fields[7] = (object)Current.TargetItemLanguage?.Name ?? DBNull.Value;
                _fields[8] = (object)Current.TargetItemVersion?.Number ?? DBNull.Value;
                _fields[9] = (object)Current.TargetPath ?? DBNull.Value;

                _fields[10] = Current.ItemAction.ToString();

                return true;
            }

            public override object GetValue(int i)
            {
                return _fields[i];
            }
        }
    }
}