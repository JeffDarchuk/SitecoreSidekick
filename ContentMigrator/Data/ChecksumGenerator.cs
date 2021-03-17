using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;

namespace Sidekick.ContentMigrator.Data
{
	public class ChecksumGenerator
	{
		public Checksum Generate(List<ID> ids, string database)
		{
			using (var sqlConnection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[database].ConnectionString))
			{
				try
				{
					sqlConnection.Open();
					using (var sqlCommand = ConstructSqlBatch(ids))
					{
						sqlCommand.Connection = sqlConnection;

						using (var reader = sqlCommand.ExecuteReader())
						{
							var checksum = new Checksum();
							while (reader.Read())
							{
								checksum.LoadRow(reader["ID"].ToString(), reader["ParentID"].ToString(), reader["Value"].ToString(), reader["Language"].ToString(), reader["Version"] as int?);
							}

							checksum.Generate();
							return checksum;
						}
					}
				}
				catch (Exception e)
				{
					Log.Warn("Checksum generation failed.",e, this);
				}
				finally
				{
					sqlConnection.Close();
				}
			}

			return null;
		}

		private SqlCommand ConstructSqlBatch(List<ID> id)
		{
			var command = new SqlCommand();

			var sql = new StringBuilder(8000);

			sql.Append($@"
				WITH Roots AS (
					SELECT Id
					FROM Items
					WITH (NOLOCK)
					where Id {BuildSqlInStatement(id, "r")}
				), tree AS (
					SELECT x.ID, x.ParentID
					FROM Items x
					WITH (NOLOCK)
					INNER JOIN Roots ON x.ID = Roots.ID
					UNION ALL
					SELECT y.ID, y.ParentID
					FROM Items y
					WITH (NOLOCK)
					INNER JOIN tree t ON y.ParentID = t.ID
				)
				SELECT t.ID, t.ParentID, v.Value, v.Language, v.Version
				FROM tree t
				WITH (NOLOCK)
				inner join VersionedFields v on v.ItemId = t.ID
				WHERE v.FieldId = '{FieldIDs.Revision.Guid:D}'

");

			command.CommandText = sql.ToString();
			return command;
		}

		private StringBuilder BuildSqlInStatement(List<ID> parameters, string parameterPrefix)
		{

			var inStatement = new StringBuilder(((parameterPrefix.Length + 4) * parameters.Count) + 5);
			inStatement.Append("IN (");
			inStatement.Append("'"); // first element param @, subsequent get from join below
			inStatement.Append(string.Join("', '", parameters.Select(x => x.Guid.ToString("D").ToUpper())));
			inStatement.Append("')");

			return inStatement;
		}
	}
}
