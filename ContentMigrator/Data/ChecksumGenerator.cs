using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;

namespace ScsContentMigrator.Data
{
	public class ChecksumGenerator
	{
		public Checksum Generate(List<ID> ids, string database)
		{
			using (var sqlConnection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[database].ConnectionString))
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
							checksum.LoadRow(reader["ID"].ToString(), reader["ParentID"].ToString(), reader["Value"].ToString());
						}
						checksum.Generate();
						return checksum;
					}
				}
			}
		}
		//to get on demand checksums, prooved to not scale well.
		//public int Generate(ID id, string database)
		//{
		//	using (var sqlConnection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[database].ConnectionString))
		//	{
		//		sqlConnection.Open();
		//		using (var sqlCommand = ConstructSqlBatch(id))
		//		{
		//			sqlCommand.Connection = sqlConnection;

		//			using (var reader = sqlCommand.ExecuteReader())
		//			{
		//				var checksum = new Checksum();
		//				StringBuilder sb = new StringBuilder();
		//				while (reader.Read())
		//				{
		//					sb.Append(reader["Value"]);
		//				}
		//				checksum.Generate();
		//				return sb.ToString().GetHashCode();
		//			}
		//		}
		//	}
		//}
		private SqlCommand ConstructSqlBatch(List<ID> id)
		{
			var command = new SqlCommand();

			var sql = new StringBuilder(8000);

			sql.Append($@"
				WITH Roots AS (
					SELECT Id
					FROM Items
					where Id {BuildSqlInStatement(id, command, "r")}
				), tree AS (
					SELECT x.ID, x.ParentID
					FROM Items x
					INNER JOIN Roots ON x.ID = Roots.ID
					UNION ALL
					SELECT y.ID, y.ParentID
					FROM Items y
					INNER JOIN tree t ON y.ParentID = t.ID
				)
				SELECT t.ID, t.ParentID, v.Value
				FROM tree t
				inner join VersionedFields v on v.ItemId = t.ID
				WHERE v.FieldId = '{FieldIDs.Revision.Guid:D}'

");

			command.CommandText = sql.ToString();
			return command;
		}
//		private SqlCommand ConstructSqlBatch(ID id)
//		{
//			var command = new SqlCommand();

//			var sql = new StringBuilder(8000);

//			sql.Append($@"
//				WITH Roots AS (
//					SELECT Id
//					FROM Items
//					where Id = '{id.Guid:D}'
//				), tree AS (
//					SELECT x.ID, x.ParentID
//					FROM Items x
//					INNER JOIN Roots ON x.ID = Roots.ID
//					UNION ALL
//					SELECT y.ID, y.ParentID
//					FROM Items y
//					INNER JOIN tree t ON y.ParentID = t.ID
//				)
//				SELECT v.Value
//				FROM tree t
//				inner join VersionedFields v on v.ItemId = t.ID
//				WHERE v.FieldId = '{FieldIDs.Revision.Guid:D}'
//				order by v.Value

//");

//			command.CommandText = sql.ToString();
//			return command;
//		}
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
