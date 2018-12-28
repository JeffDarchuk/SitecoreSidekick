using System.Collections.Generic;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface IChangeProcessor
    {
        void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, ICollection<ItemChange> changes);
    }
}