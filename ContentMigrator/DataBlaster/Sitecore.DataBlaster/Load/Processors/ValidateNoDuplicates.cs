using System.Diagnostics.CodeAnalysis;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    public class ValidateNoDuplicates : IValidateStagedData
    {
        public bool ValidateLoadStage(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext)
		{
			return true;
            //if (!CheckDuplicates(loadContext, sqlContext))
            //{
            //    loadContext.StageFailed(Stage.Load, "Duplicate items were found in set to load.");
            //    return false;
            //}
            //return true;
        }

        [SuppressMessage("Microsoft.Security", "CA2100", Justification = "No user parameters")]
        private bool CheckDuplicates(BulkLoadContext context, BulkLoadSqlContext sqlContext)
		{
			//return true;
            var check = sqlContext.GetEmbeddedSql(context, "Sql.05.CheckDuplicates.sql");
            var hasErrors = false;

            using (var cmd = sqlContext.NewSqlCommand(check))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    context.Log.Error(
                        $"Duplicate item found with id '{reader["Id"]}', item path '{reader["ItemPath"]}' " +
                        $"and source info '{reader["SourceInfo"]}'.");
                    hasErrors = true;
                }
            }
            return !hasErrors;
        }
    }
}