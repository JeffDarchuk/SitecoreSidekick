using System.Collections.Generic;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface IItemProcessor
    {
        IEnumerable<BulkLoadItem> Process(BulkLoadContext context, IEnumerable<BulkLoadItem> items);
    }
}