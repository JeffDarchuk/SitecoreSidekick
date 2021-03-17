using System.Diagnostics.CodeAnalysis;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
	public class ValidateDataIntegrity : IValidateStagedData
    {
        public bool ValidateLoadStage(BulkLoadContext context, BulkLoadSqlContext sqlContext)
		{
			//return true;
            if (!CheckTempData(context, sqlContext))
            {
                context.StageFailed(Stage.Load, "Found missing templates, parents, items or fields.");
                return false;
            }
            return true;
        }

        [SuppressMessage("Microsoft.Security", "CA2100", Justification = "No user parameters")]
        private bool CheckTempData(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext)
        {
            var check = sqlContext.GetEmbeddedSql(loadContext, "Sql.07.CheckTempData.sql");
            var hasErrors = false;

            using (var cmd = sqlContext.NewSqlCommand(check))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!reader.GetBoolean(reader.GetOrdinal("HasParent")))
                        loadContext.Log.Error(
                            $"Unable to find parent '{reader["ParentId"]}' for item with id '{reader["Id"]}', " +
                            $"item path '{reader["ItemPath"]}' and source info '{reader["SourceInfo"]}'.");
                    if (!reader.GetBoolean(reader.GetOrdinal("HasTemplate")))
                        loadContext.Log.Error(
                            $"Unable to find template '{reader["TemplateName"]}' with id '{reader["TemplateId"]}' " +
                            $"for item with id '{reader["Id"]}', item path '{reader["ItemPath"]}' and source info '{reader["SourceInfo"]}'.");
                    hasErrors = true;
                }
                reader.NextResult();
                while (reader.Read())
                {
                    if (!reader.GetBoolean(reader.GetOrdinal("HasItem")))
                        loadContext.Log.Error(
                            $"Unable to find item with id '{reader["ItemId"]}', item path '{reader["ItemPath"]}' " +
                            $"and source info '{reader["SourceInfo"]}' for field '{reader["FieldName"]}' with id '{reader["FieldId"]}'.");
                    if (!reader.GetBoolean(reader.GetOrdinal("HasField")))
                        loadContext.Log.Error(
                            $"Unable to find field '{reader["FieldName"]}' with id '{reader["FieldId"]}' " +
                            $"for item with id '{reader["ItemId"]}', item path '{reader["ItemPath"]}' and source info '{reader["SourceInfo"]}'.");
                    hasErrors = true;
                }
            }

            return !hasErrors;
        }
    }
}