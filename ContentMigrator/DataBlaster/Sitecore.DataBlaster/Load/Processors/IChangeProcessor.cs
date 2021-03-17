using System.Collections.Generic;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface IChangeProcessor
    {
        void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, ICollection<ItemChange> changes);
    }
}