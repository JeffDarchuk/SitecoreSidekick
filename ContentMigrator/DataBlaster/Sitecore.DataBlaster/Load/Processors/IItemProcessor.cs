using System.Collections.Generic;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface IItemProcessor
    {
        IEnumerable<BulkLoadItem> Process(BulkLoadContext context, IEnumerable<BulkLoadItem> items);
    }
}