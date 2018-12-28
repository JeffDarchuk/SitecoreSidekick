using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface IValidateStagedData
    {
        bool ValidateLoadStage(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext);
    }
}