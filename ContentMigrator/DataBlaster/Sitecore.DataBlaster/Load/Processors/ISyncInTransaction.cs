using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface ISyncInTransaction
    {
	    void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext);
    }
}