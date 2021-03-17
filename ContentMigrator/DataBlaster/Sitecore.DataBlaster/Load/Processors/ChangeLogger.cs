using System.Collections.Generic;
using System.Linq;
using log4net.spi;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public class ChangeLogger : IChangeProcessor
    {
        public void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, ICollection<ItemChange> changes)
        {
            loadContext.Log.Info($"Item changes in database: created: {loadContext.ItemChanges.Count(ic => ic.Created)}, " +
                             $"saved: {loadContext.ItemChanges.Count(ic => ic.Saved)}, moved: {loadContext.ItemChanges.Count(ic => ic.Moved)}, " +
                             $"deleted: {loadContext.ItemChanges.Count(ic => ic.Deleted)}");

            if (!loadContext.Log.Logger.IsEnabledFor(Level.TRACE)) return;

            foreach (var change in loadContext.ItemChanges)
            {
                if (change.Created)
                    loadContext.Log.Trace(
                        $"Created item with path '{change.ItemPath ?? "UNKNOWN"}', id '{change.ItemId}', " +
                        $"language '{change.Language ?? "NULL"}' and source info '{change.SourceInfo}' in database.");
                if (change.Saved & !change.Created)
                    loadContext.Log.Trace(
                        $"Saved item with path '{change.ItemPath ?? "UNKNOWN"}', id '{change.ItemId}', " +
                        $"language '{change.Language ?? "NULL"}' and source info '{change.SourceInfo}' in database.");
                if (change.Moved)
                    loadContext.Log.Trace(
                        $"Moved item with path '{change.ItemPath ?? "UNKNOWN"}', id '{change.ItemId}', " +
                        $"language '{change.Language ?? "NULL"}' and source info '{change.SourceInfo}' in database.");
                if (change.Deleted)
                    loadContext.Log.Trace(
                        $"Deleted item with path '{change.ItemPath ?? "UNKNOWN"}', id '{change.ItemId}', " +
                        $"language '{change.Language ?? "NULL"}' and source info '{change.SourceInfo}' in database.");
            }
        }
    }
}