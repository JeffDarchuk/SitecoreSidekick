using System.Collections.Generic;
using System.Linq;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public class ItemValidator : IItemProcessor, IValidateStagedData
    {
        private bool HasItemsWithoutFields { get; set; }
        private bool HasItemsWithoutPaths { get; set; }

        public IEnumerable<BulkLoadItem> Process(BulkLoadContext context, IEnumerable<BulkLoadItem> items)
        {
            // We don't support items without fields.
            items = items.Where(x =>
            {
                if (x.FieldCount != 0) return true;
                context.SkipItemWarning(
                    $"Item with id '{x.Id}', item path '{x.ItemPath}' and source info '{x.SourceInfo}' has no fields.");
                HasItemsWithoutFields = true;
                return false;
            });

            // Item path must be available when item ids need to be looked up.
            items = items.Where(x =>
            {
                if (!context.LookupItemIds) return true;
                if (!string.IsNullOrWhiteSpace(x.ItemPath)) return true;
                context.SkipItemWarning($"Item with id '{x.Id}', and source info '{x.SourceInfo}' has no item path.");
                HasItemsWithoutPaths = true;
                return false;
            });

            return items;
        }

        public bool ValidateLoadStage(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext)
        {
            if (HasItemsWithoutFields)
            {
                loadContext.StageFailed(Stage.Load, "Items without fields were found .");
                return false;
            }

            if (HasItemsWithoutPaths)
            {
                loadContext.StageFailed(Stage.Load, "Items were found without paths.");
                return false;
            }

            return true;
        }
    }
}