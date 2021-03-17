using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql
{
    public class BulkLoadSqlContext : SqlContext
    {
        public BulkLoadSqlContext(SqlConnection connection, Type defaultSubject = null) 
            : base(connection, defaultSubject: defaultSubject)
        {
        }

	    public virtual string GetEmbeddedSql(BulkLoadContext context, string relativePath, Type subject = null)
        {
            return PostProcessSql(context, GetEmbeddedSql(relativePath, subject));
        }

        public virtual string PostProcessSql(BulkLoadContext context, string sql)
        {
            if (context.StageDataWithoutProcessing)
                return sql.Replace("#", "tmp_");

            return sql;
        }

        [SuppressMessage("Microsoft.Security", "CA2100", Justification = "No user parameters")]
        public virtual void DropStageTables()
        {
            var sql =
                "SELECT TABLE_SCHEMA, TABLE_NAME " +
                "FROM INFORMATION_SCHEMA.TABLES " +
                "WHERE TABLE_NAME LIKE 'tmp_%'";

            var toDelete = new List<string>();
            using (var cmd = NewSqlCommand(sql))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    toDelete.Add($"[{reader.GetString(0)}].[{reader.GetString(1)}]");
                }
            }
            foreach (var table in toDelete)
            {
                using (var cmd = NewSqlCommand("DROP TABLE " + table))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}