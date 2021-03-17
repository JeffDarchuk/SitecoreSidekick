using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Read
{
    public class BulkReader
    {
	    /// <summary>
	    /// Gets the item header information for all descendants of an item.
	    /// </summary>
	    /// <param name="sqlContext">Sql context to read from.</param>
	    /// <param name="itemId">Ancestor item id.</param>
	    /// <param name="itemPath">Ancestor item path.</param>
	    /// <param name="ofTemplates">Filter descendants by template(s). No inheritance supported.</param>
	    /// <param name="modifiedSince">Filter descendants by modification timestamp.</param>
	    /// <returns>Stream of item header information.</returns>
	    public virtual IEnumerable<ItemHeader> GetDescendantHeaders(SqlContext sqlContext,
		    Guid itemId, string itemPath, Guid[] ofTemplates = null, DateTime? modifiedSince = null)
	    {
		    var templateCsv = ofTemplates == null || ofTemplates.Length == 0
			    ? null
			    : string.Join(",", ofTemplates.Select(x => $"'{x:D}'"));

		    var sql = sqlContext.GetEmbeddedSqlLines("Sql.GetDescendantHeaders.sql", typeof(BulkReader))
				.ExpandParameterLineIf(() => ofTemplates?.Length > 1, "@templateIdsCsv", templateCsv)
			    .RemoveParameterLineIf(() => ofTemplates == null || ofTemplates.Length != 1, "@templateId")
			    .RemoveParameterLineIf(() => ofTemplates == null || ofTemplates.Length <= 1, "@templateIdsCsv")
			    .RemoveParameterLineIf(() => modifiedSince == null, "@modifiedSince");

		    using (var reader = sqlContext.ExecuteReader(sql, commandProcessor: cmd =>
		    {
			    cmd.Parameters.AddWithValue("@rootItemId", itemId);
			    cmd.Parameters.AddWithValue("@rootItemPath", itemPath);
			    if (ofTemplates != null && ofTemplates.Length == 1)
				    cmd.Parameters.AddWithValue("templateId", ofTemplates[0]);
			    if (modifiedSince.HasValue)
				    cmd.Parameters.AddWithValue("@modifiedSince", modifiedSince.Value.ToUniversalTime());
		    }))
		    {
			    while (reader.Read())
			    {
				    yield return new ItemHeader(
					    reader.GetGuid(0),
					    reader.GetString(1),
					    reader.GetString(2),
					    reader.GetGuid(3),
					    reader.GetGuid(4),
					    reader.GetGuid(5),
					    reader.GetDateTime(6),
					    reader.GetDateTime(7)
				    );
			    }
		    }
	    }

	    /// <summary>
	    /// Gets the item header information for all descendants of an item.
	    /// </summary>
	    /// <param name="item">Item to get descendant headers for.</param>
	    /// <param name="ofTemplates">Filter descendants by template(s). No inheritance supported.</param>
	    /// <param name="modifiedSince">Filter descendants by modification timestamp.</param>
	    /// <returns>Stream of item header information.</returns>
		public virtual IEnumerable<ItemHeader> GetDescendantHeaders(Item item,
            Guid[] ofTemplates = null, DateTime? modifiedSince = null)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

	        using (var conn = GetSqlConnection(item.Database, open: true))
	        {
		        foreach (var header in GetDescendantHeaders(new SqlContext(conn, typeof(BulkReader)),
			        item.ID.Guid, item.Paths.Path, ofTemplates, modifiedSince))
		        {
			        yield return header;
		        }
	        }
        }

		/// <summary>
		/// Gets the item version header information for all descendants of an item.
		/// </summary>
		/// <param name="sqlContext">Sql context to read from.</param>
		/// <param name="itemId">Ancestor item id.</param>
		/// <param name="itemPath">Ancestor item path.</param>
		/// <param name="ofTemplates">Filter descendants by template(s). No inheritance supported.</param>
		/// <param name="itemModifiedSince">Filter descendants by item modification timestamp.</param>
		/// <param name="itemVersionModifiedSince">Filter descendants by item version modification timestamp.</param>
	    /// <returns>Stream of item version header information.</returns>
		public virtual IEnumerable<ItemVersionHeader> GetDescendantVersionHeaders(SqlContext sqlContext, Guid itemId, string itemPath,
		    Guid[] ofTemplates = null, DateTime? itemModifiedSince = null, DateTime? itemVersionModifiedSince = null)
	    {
		    var templateCsv = ofTemplates == null || ofTemplates.Length == 0
			    ? null
			    : string.Join(",", ofTemplates.Select(x => $"'{x:D}'"));

		    var sql = sqlContext.GetEmbeddedSqlLines("Sql.GetDescendantVersionHeaders.sql", typeof(BulkReader))
			    .ExpandParameterLineIf(() => ofTemplates?.Length > 1, "@templateIdsCsv", templateCsv)
			    .RemoveParameterLineIf(() => ofTemplates == null || ofTemplates.Length != 1, "@templateId")
			    .RemoveParameterLineIf(() => ofTemplates == null || ofTemplates.Length <= 1, "@templateIdsCsv")
			    .RemoveParameterLineIf(() => itemModifiedSince == null, "@itemModifiedSince")
			    .RemoveParameterLineIf(() => itemVersionModifiedSince == null, "@versionModifiedSince");

		    using (var reader = sqlContext.ExecuteReader(sql, commandProcessor: cmd =>
		    {
			    cmd.Parameters.AddWithValue("@rootItemId", itemId);
			    cmd.Parameters.AddWithValue("@rootItemPath", itemPath);
			    if (ofTemplates != null && ofTemplates.Length == 1)
				    cmd.Parameters.AddWithValue("templateId", ofTemplates[0]);
			    if (itemModifiedSince.HasValue)
				    cmd.Parameters.AddWithValue("@itemModifiedSince", itemModifiedSince.Value.ToUniversalTime());
			    if (itemVersionModifiedSince.HasValue)
				    cmd.Parameters.AddWithValue("@versionModifiedSince", itemVersionModifiedSince.Value.ToUniversalTime());
		    }))
		    {
			    while (reader.Read())
			    {
				    yield return new ItemVersionHeader(
					    reader.GetGuid(0),
					    reader.GetString(1),
					    reader.GetString(2),
					    reader.GetGuid(3),
					    reader.GetGuid(4),
					    reader.GetGuid(5),
					    reader.GetString(6),
					    reader.GetInt32(7),
					    reader.GetDateTime(8),
					    reader.GetDateTime(9)
				    );
			    }
		    }
	    }

		/// <summary>
		/// Gets the item version header information for all descendants of an item.
		/// </summary>
		/// <param name="item">Item to get descendant version headers for.</param>
		/// <param name="ofTemplates">Filter descendants by template(s). No inheritance supported.</param>
		/// <param name="itemModifiedSince">Filter descendants by item modification timestamp.</param>
		/// <param name="itemVersionModifiedSince">Filter descendants by item version modification timestamp.</param>
		/// <returns>Stream of item version header information.</returns>
		public virtual IEnumerable<ItemVersionHeader> GetDescendantVersionHeaders(Item item,
		    Guid[] ofTemplates = null, DateTime? itemModifiedSince = null, DateTime? itemVersionModifiedSince = null)
	    {
		    if (item == null) throw new ArgumentNullException(nameof(item));

		    using (var conn = GetSqlConnection(item.Database, open: true))
		    {
			    foreach (var header in GetDescendantVersionHeaders(new SqlContext(conn, typeof(BulkReader)),
				    item.ID.Guid, item.Paths.Path, ofTemplates, itemModifiedSince, itemVersionModifiedSince))
			    {
				    yield return header;
			    }
		    }
	    }

	    /// <summary>
	    /// Gets all descendants of an item.
	    /// </summary>
	    /// <param name="sqlContext">Sql context to read from.</param>
	    /// <param name="itemId">Ancestor item id.</param>
	    /// <param name="itemPath">Ancestor item path.</param>
	    /// <param name="ofTemplates">Filter descendants by template(s). No inheritance supported.</param>
	    /// <param name="modifiedSince">Filter descendants by modification timestamp.</param>
	    /// <param name="onlyPublishable">Filter out descendants that are set to 'never publish'.</param>
	    /// <returns>Stream of bulk items.</returns>
		public virtual IEnumerable<BulkItem> GetDescendants(SqlContext sqlContext, Guid itemId, string itemPath,
		    Guid[] ofTemplates = null, DateTime? modifiedSince = null, bool onlyPublishable = false)
	    {
		    var templateCsv = ofTemplates == null || ofTemplates.Length == 0
			    ? null
			    : string.Join(",", ofTemplates.Select(x => $"'{x:D}'"));

		    var sql = sqlContext.GetEmbeddedSqlLines("Sql.GetDescendants.sql", typeof(BulkReader))
				.ExpandParameterLineIf(() => ofTemplates?.Length > 1, "@templateIdsCsv", templateCsv)
			    .RemoveParameterLineIf(() => ofTemplates == null || ofTemplates.Length != 1, "@templateId")
			    .RemoveParameterLineIf(() => ofTemplates == null || ofTemplates.Length <= 1, "@templateIdsCsv")
			    .RemoveParameterLineIf(() => modifiedSince == null, "@modifiedSince")
			    .RemoveParameterLineIf(() => !onlyPublishable, "@neverPublishFieldId")
			    .RemoveParameterLineIf(() => !onlyPublishable, "@neverPublish");

		    using (var reader = sqlContext.ExecuteReader(sql, commandProcessor: cmd =>
		    {
			    cmd.Parameters.AddWithValue("@rootItemId", itemId);
			    cmd.Parameters.AddWithValue("@rootItemPath", itemPath);
			    if (ofTemplates != null && ofTemplates.Length == 1)
				    cmd.Parameters.AddWithValue("templateId", ofTemplates[0]);
			    if (modifiedSince.HasValue)
				    cmd.Parameters.AddWithValue("@modifiedSince", modifiedSince.Value.ToUniversalTime());
			    if (onlyPublishable)
			    {
				    cmd.Parameters.AddWithValue("@neverPublishFieldId", global::Sitecore.FieldIDs.NeverPublish.Guid);
				    cmd.Parameters.AddWithValue("@neverPublish", "1");
			    }
		    }))
		    {
			    BulkItem item = null;
			    while (reader.Read())
			    {
				    var recordItemId = reader.GetGuid(0);

				    if (item != null && item.Id != recordItemId)
				    {
					    yield return item;
					    item = null;
				    }

				    if (item == null)
					    item = new BulkItem(recordItemId,
						    reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5), reader.GetString(2));

				    var language = reader.IsDBNull(10) ? null : reader.GetString(10);
				    var version = reader.IsDBNull(11) ? null : (int?) reader.GetInt32(11);

				    if (language != null && version != null)
					    item.AddVersionedField(reader.GetGuid(8), language, version.Value, reader.GetString(9));
				    else if (language != null)
					    item.AddUnversionedField(reader.GetGuid(8), language, reader.GetString(9));
				    else
					    item.AddSharedField(reader.GetGuid(8), reader.GetString(9));
			    }
			    if (item != null) yield return item;
		    }
	    }

		/// <summary>
		/// Gets all descendants of an item.
		/// </summary>
		/// <param name="item">Item to get descendants for.</param>
		/// <param name="ofTemplates">Filter descendants by template(s). No inheritance supported.</param>
		/// <param name="modifiedSince">Filter descendants by modification timestamp.</param>
		/// <param name="onlyPublishable">Filter out descendants that are set to 'never publish'.</param>
		/// <returns>Stream of bulk items.</returns>
		public virtual IEnumerable<BulkItem> GetDescendants(Item item,
		    Guid[] ofTemplates = null, DateTime? modifiedSince = null, bool onlyPublishable = false)
	    {
		    if (item == null) throw new ArgumentNullException(nameof(item));

		    using (var conn = GetSqlConnection(item.Database, open: true))
		    {
			    foreach (var descendant in GetDescendants(new SqlContext(conn, typeof(BulkReader)),
				    item.ID.Guid, item.Paths.Path, ofTemplates, modifiedSince, onlyPublishable))
			    {
				    yield return descendant;
			    }
		    }
	    }

		public virtual SqlConnection GetSqlConnection(Database database, bool open = false)
	    {
		    var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[database.ConnectionStringName].ConnectionString);
		    if (open) conn.Open();
		    return conn;
	    }
	}
}