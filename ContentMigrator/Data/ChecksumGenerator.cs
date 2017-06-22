using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sitecore.Data;
using Sitecore.Diagnostics;

namespace ScsContentMigrator.Data
{
	public class ChecksumGenerator
	{
		public int Generate(string id, string database)
		{
			using (var sqlConnection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[database].ConnectionString))
			{
				sqlConnection.Open();
				using (var sqlCommand = ConstructSqlBatch(id))
				{
					sqlCommand.Connection = sqlConnection;
				
					using (var reader = sqlCommand.ExecuteReader())
					{
						StringBuilder sb = new StringBuilder();
						while (reader.Read())
						{
							sb.Append(reader["Value"]);
						}
						return sb.ToString().GetHashCode();
					}
				}
			}
		}
		private SqlCommand ConstructSqlBatch(string id)
		{
			var command = new SqlCommand();

			var sql = new StringBuilder(8000);

			// ITEM DATA QUERY - gets top level metadata about included items (no fields)
			sql.Append($@"
				IF OBJECT_ID('tempdb..#TempItemData') IS NOT NULL DROP Table #TempItemData
				CREATE TABLE #TempItemData(
					 ID uniqueidentifier,
					 Name nvarchar(256),
					 TemplateID uniqueidentifier,
					 MasterID uniqueidentifier,
					 ParentID uniqueidentifier
				 );
				WITH Roots AS (
					SELECT Id
					FROM Items
					WHERE ID = '{id}'
				), tree AS (
					SELECT x.ID, x.Name, x.TemplateID, x.MasterID, x.ParentID
					FROM Items x
					INNER JOIN Roots ON x.ID = Roots.ID
					UNION ALL
					SELECT y.ID, y.Name, y.TemplateID, y.MasterID, y.ParentID
					FROM Items y
					INNER JOIN tree t ON y.ParentID = t.ID
				)
				INSERT INTO #TempItemData
				SELECT *
				FROM tree

				SELECT DISTINCT Value
				FROM VersionedFields v
				INNER JOIN #TempItemData t ON v.ItemId = t.ID
				WHERE v.FieldId = '8CDC337E-A112-42FB-BBB4-4143751E123F'
				ORDER BY Value

");

			command.CommandText = sql.ToString();


			return command;
		}
		private StringBuilder BuildSqlInStatement(List<ID> parameters, SqlCommand command, string parameterPrefix)
		{

			var inStatement = new StringBuilder(((parameterPrefix.Length + 4) * parameters.Count) + 5); // ((prefixLength + '@, ') * paramCount) + 'IN ()'
			inStatement.Append("IN (");
			inStatement.Append("'"); // first element param @, subsequent get from join below
			inStatement.Append(string.Join("', '", parameters.Select(x => x.Guid.ToString("D").ToUpper())));
			inStatement.Append("')");

			return inStatement;
		}
	}
}
