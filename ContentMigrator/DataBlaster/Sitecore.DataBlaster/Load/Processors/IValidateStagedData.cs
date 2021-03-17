using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface IValidateStagedData
    {
        bool ValidateLoadStage(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext);
    }
}