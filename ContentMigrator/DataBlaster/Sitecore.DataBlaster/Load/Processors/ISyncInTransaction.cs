using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public interface ISyncInTransaction
    {
	    void Process(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext);
    }
}