using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Sql;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql;
using Sitecore.Configuration;
using Sitecore.Data.Managers;
using Sitecore.Events;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
    public class BulkLoader
    {
	    /// <summary>
		/// Allows reading and modifying the stream of bulk items.
		/// </summary>
        public Chain<IItemProcessor> OnItemProcessing { get; }
		/// <summary>
		/// Allows validation of data that is staged in database.
		/// </summary>
		public Chain<IValidateStagedData> OnStagedDataValidating { get; }
		/// <summary>
		/// Allosw validation of indexed temp tables.
		/// </summary>
	    public Chain<IValidateStagedData> OnTempDataValidating { get; }
		/// <summary>
		/// Allows executing database operations in the load transaction.
		/// </summary>
		public Chain<ISyncInTransaction> OnTransactionCommitting { get; }
	    /// <summary>
	    /// Allows performing operations based on the changed items in the database 
	    /// but before they might be accesible through the Sitecore items API.
	    /// </summary>
	    public Chain<IChangeProcessor> OnItemsLoading { get; }
		/// <summary>
		/// Allows performing operations based on the changed items in the database.
		/// </summary>
		public Chain<IChangeProcessor> OnItemsLoaded { get; }

        public BulkLoader()
        {
	        var itemValidator = new ItemValidator();
	        var itemLinker = new ItemLinker();

			OnItemProcessing = new Chain<IItemProcessor>
            {
                new ItemBucketer(),
                new ItemVersionEnsurer(),
                itemValidator,
                new ItemLinker()
            };

	        OnStagedDataValidating = new Chain<IValidateStagedData>
	        {
		        itemValidator,
		        new ValidateNoDuplicates()
	        };

	        OnTempDataValidating = new Chain<IValidateStagedData>
	        {
		        new ValidateDataIntegrity()
	        };

            OnTransactionCommitting = new Chain<ISyncInTransaction>
            {
               new SyncHistoryTable(),
               new SyncPublishQueue()
            };

	        OnItemsLoading = new Chain<IChangeProcessor>
	        {
		        new ChangeCacheClearer()
	        };

			OnItemsLoaded = new Chain<IChangeProcessor>
            {
                new ChangeLogger(),
                itemLinker,
                new ChangeIndexer()
            };
        }

        [SuppressMessage("Microsoft.Security", "CA2100", Justification = "No user parameters")]
        public virtual void LoadItems(BulkLoadContext context, IEnumerable<BulkLoadItem> items)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (items == null) throw new ArgumentNullException(nameof(items));

	        //items = OnItemProcessing.Execute(items, (p, itms) => p.Process(context, itms));

            var db = Factory.GetDatabase(context.Database, true);
            var connectionString = ConfigurationManager.ConnectionStrings[db.ConnectionStringName].ConnectionString;

            var infoMessageHandler = new SqlInfoMessageEventHandler((s, e) => OnSqlInfoMessage(context, s, e));
            using (var conn = new SqlConnection(connectionString))
            {
                var sqlContext = new BulkLoadSqlContext(conn, typeof(BulkLoader));
                SqlTransaction transaction = null;
                try
                {
                    conn.InfoMessage += infoMessageHandler;
                    conn.Open();

                    BulkItemsAndFieldsReader itemAndFieldReader;
                    if (!StageData(context, sqlContext, items, out itemAndFieldReader)) return;

                    if (context.StageDataWithoutProcessing)
                    {
                        context.Log.Info("Data to import is staged in database, no processing done.");
                        context.StageSucceeded(Stage.Load);
                        return;
                    }

                    if (itemAndFieldReader.ReadItemCount > 0)
                    {
                        LookupIds(context, sqlContext, itemAndFieldReader);

                        if (!ValidateAndPrepareData(context, sqlContext)) return;

                        sqlContext.Transaction = transaction = conn.BeginTransaction();
                        MergeData(context, sqlContext, itemAndFieldReader);
                    }

		            OnTransactionCommitting.Execute(p => p.Process(context, sqlContext));

                    // After this point, there's no use in keeping the transaction arround,
                    // because we cannot sync everything transactionally (e.g. indexes, publshing, ...)
                    // Be aware that after this point the process may halt and not everything is consistent.
                    // We mitigate this inconsistency with crash recovery, see below.
                    transaction?.Commit();
                    transaction = null;

					// Allow clearing caches before raising event so that reading the item API in event will result in fresh reads.
					OnItemsLoading.Execute(p => p.Process(context, sqlContext, context.ItemChanges));

                    // Databases are now entirely in sync.
                    context.OnDataLoaded?.Invoke(context);
                    Event.RaiseEvent("bulkloader:dataloaded", context);

					// Execute post processors.y.
	                var itemChanges = GetChanges(context, sqlContext);
	                OnItemsLoaded.Execute(p => p.Process(context, sqlContext, itemChanges));

                    context.StageSucceeded(Stage.Load);
                }
                catch (Exception ex)
                {
                    transaction?.Rollback();
                    context.StageFailed(Stage.Load, ex.Message);
                }
                finally
                {
                    conn.InfoMessage -= infoMessageHandler;
                }
            }
        }

        #region Data staging

        protected virtual bool StageData(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, 
			IEnumerable<BulkItem> items, out BulkItemsAndFieldsReader itemAndFieldReader)
        {
            var sql = sqlContext.GetEmbeddedSql(loadContext, "Sql.01.CreateTempTable.sql");

            // Cleanup left-over staging tables.
            sqlContext.DropStageTables();

			// Create temp table.
			// We don't use table valued parameters because we don't want to change the database schema.
	        sqlContext.ExecuteSql(sql);

            // Load data into temp table.
            itemAndFieldReader = NewReader(items);
            if (!BulkCopyToTempTable(loadContext, sqlContext, itemAndFieldReader, NewFieldRulesReader(loadContext))) return false;

            loadContext.OnDataStaged?.Invoke(loadContext);
            Event.RaiseEvent("bulkloader:datastaged", loadContext);

            return true;
        }

        protected virtual BulkItemsAndFieldsReader NewReader(IEnumerable<BulkItem> items)
        {
            return new BulkItemsAndFieldsReader(() => items.SelectMany(x => x.Fields).GetEnumerator());
        }

        protected virtual FieldRulesReader NewFieldRulesReader(BulkLoadContext context)
        {
            if (context.FieldRules == null || context.FieldRules.Count == 0) return null;

            return new FieldRulesReader(() => context.FieldRules.GetEnumerator());
        }

        protected virtual bool BulkCopyToTempTable(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext,
			BulkItemsAndFieldsReader itemAndFieldReader, FieldRulesReader fieldRulesReader)
        {
            var stopwatch = Stopwatch.StartNew();

            // Bulk copy into temp tables, so that we don't have to buffer stuff,
            // blobs can give OutOfMemoryExceptions.
            using (var bulkCopy = new SqlBulkCopy(sqlContext.Connection))
            {
                bulkCopy.BulkCopyTimeout = int.MaxValue;
                bulkCopy.EnableStreaming = true;
                bulkCopy.DestinationTableName = sqlContext.PostProcessSql(loadContext, "#BulkItemsAndFields");
                try
                {
                    bulkCopy.WriteToServer(itemAndFieldReader);
                }
                catch (Exception exception)
                {
                    loadContext.StageFailed(Stage.Load, exception, $"Write to #BulkItemsAndFields failed with message: {exception.Message}");
                    return false;
                }

                if (fieldRulesReader != null)
                {
                    bulkCopy.DestinationTableName = sqlContext.PostProcessSql(loadContext, "#FieldRules");
                    try
                    {
                        bulkCopy.WriteToServer(fieldRulesReader);
                    }
                    catch (Exception exception)
                    {
                        loadContext.StageFailed(Stage.Load, exception, $"Write to #FieldRules failed with message: {exception}");
                    }
                }
            }
            loadContext.Log.Info($"Loaded data in database: {(int)stopwatch.Elapsed.TotalSeconds}s");
            stopwatch.Restart();
            return true;
        }

        #endregion

        #region Lookup, validation and merging of staged data

        protected virtual void LookupIds(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext, 
			BulkItemsAndFieldsReader itemAndFieldReader)
        {
            var lookupBlobsSql = sqlContext.GetEmbeddedSql(loadContext, "Sql.02.LookupBlobs.sql");
            var lookupItemsSql = sqlContext.GetEmbeddedSql(loadContext, "Sql.03.LookupItems.sql");

            var stopwatch = Stopwatch.StartNew();

            if (loadContext.LookupBlobIds) sqlContext.ExecuteSql(lookupBlobsSql);

            if (loadContext.LookupItemIds)
            {
                // Using sql parameters resets temp tables.
                if (loadContext.Destination != null)
                {
	                lookupItemsSql = sqlContext.ReplaceOneLineSqlStringParameter(lookupItemsSql, "@destinationPath",
                        loadContext.Destination.ItemPath);
	                lookupItemsSql = sqlContext.ReplaceOneLineSqlStringParameter(lookupItemsSql, "@destinationId",
                        loadContext.Destination.ItemId.ToString("D"));
                }
	            sqlContext.ExecuteSql(lookupItemsSql);
            }

            loadContext.Log.Info($"Looked up ids: {(int)stopwatch.Elapsed.TotalSeconds}s");
        }

        protected virtual bool ValidateAndPrepareData(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext)
        {
            var splitTempTablesSql = sqlContext.GetEmbeddedSql(loadContext, "Sql.04.SplitTempTable.sql");
            var indexesSql = sqlContext.GetEmbeddedSql(loadContext, "Sql.06.CreateIndexes.sql");

            var stopwatch = Stopwatch.StartNew();

            sqlContext.ExecuteSql(splitTempTablesSql);

			if (!OnStagedDataValidating.Execute(p => p.ValidateLoadStage(loadContext, sqlContext), breakOnDefault: false))
			{
			}

			sqlContext.ExecuteSql(indexesSql);

			if (!OnTempDataValidating.Execute(p => p.ValidateLoadStage(loadContext, sqlContext), breakOnDefault: false))
			{
			}

            loadContext.Log.Info($"Validated and prepared loaded data: {(int)stopwatch.Elapsed.TotalSeconds}s");
            return true;
        }

        protected virtual void MergeData(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext,
			BulkItemsAndFieldsReader itemAndFieldReader)
        {
            var sql = sqlContext.GetEmbeddedSql(loadContext, "Sql.08.MergeTempData.sql");

            var stopwatch = Stopwatch.StartNew();

            // Execute merge and collect imported items.
            // Using sql parameters resets temp tables.
            sql = sqlContext.ReplaceOneLineSqlBitParameter(sql, "@ProcessDependingFields",
                itemAndFieldReader.HasFieldDependencies);
            sql = sqlContext.ReplaceOneLineSqlBitParameter(sql, "@CleanupBlobs",
                itemAndFieldReader.HasBlobFields);
            sql = sqlContext.ReplaceOneLineSqlBitParameter(sql, "@AllowTemplateChanges",
                loadContext.AllowTemplateChanges);
            sql = sqlContext.ReplaceOneLineSqlStringParameter(sql, "@DefaultLanguage",
                LanguageManager.DefaultLanguage.Name);

            using (var cmd = sqlContext.NewSqlCommand(sql))
            {
                cmd.CommandTimeout = int.MaxValue;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        loadContext.ItemChanges.AddLast(new ItemChange(reader));
                    }
                }
            }
            loadContext.Log.Info($"Merged loaded data: {(int)stopwatch.Elapsed.TotalSeconds}s");
        }

        #endregion

        #region Other

        public virtual BulkLoadContext NewBulkLoadContext(string database)
        {
            return new BulkLoadContext(database);
        }

        protected virtual void OnSqlInfoMessage(BulkLoadContext context, object sender, SqlInfoMessageEventArgs args)
        {
            foreach (var line in args.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                context.Log.Debug($"SQL Info: {line}");
            }
        }

        protected virtual ICollection<ItemChange> GetChanges(BulkLoadContext loadContext, BulkLoadSqlContext sqlContext)
        {
			// By putting this in a virtual method, overriders can implement e.g. crash recovery.
	        return loadContext.ItemChanges;
        }

        protected class BulkItemsAndFieldsReader : AbstractEnumeratorReader<BulkField>
        {
            private readonly object[] _fields;

            public override int FieldCount => 25;

            public int ReadItemCount { get; protected set; }

            public bool HasFieldDependencies { get; protected set; } = false;
            public bool HasPathExpressions { get; protected set; } = false;
            public bool HasBlobFields { get; protected set; }

            [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Not an issue in this case.")]
            public BulkItemsAndFieldsReader(Func<IEnumerator<BulkField>> enumerator)
                : base(enumerator)
            {
                _fields = new object[FieldCount];
            }

            public override bool Read()
            {
                if (!base.Read())
                {
                    // Clear the fields.
                    for (var i = 0; i < _fields.Length; i++)
                        _fields[i] = null;
                    return false;
                }

	            var item = (BulkLoadItem) Current.Item;
				ReadItemCount++;

                if (!HasFieldDependencies && (Current.DependsOnCreate || Current.DependsOnUpdate))
                    HasFieldDependencies = true;

                if (!HasPathExpressions && item.ItemLookupPath != null)
                    HasPathExpressions = true;

                if (!HasBlobFields && Current.IsBlob)
                    HasBlobFields = true;

                BulkLoadAction fieldAction = BulkLoadAction.AddOnly;
     //           switch (item.LoadAction)
     //           {
     //               case BulkLoadAction.AddOnly:
     //                   fieldAction = BulkLoadAction.AddOnly;
     //                   break;
					//case BulkLoadAction.AddItemOnly:
					//	fieldAction = BulkLoadAction.AddItemOnly;
					//	break;
					//case BulkLoadAction.Update:
     //                   fieldAction = BulkLoadAction.Update;
     //                   break;
     //               case BulkLoadAction.UpdateExistingItem:
     //                   fieldAction = BulkLoadAction.Update;
     //                   break;
     //               case BulkLoadAction.Revert:
     //                   fieldAction = BulkLoadAction.Update;
     //                   break;
     //               case BulkLoadAction.RevertTree:
     //                   fieldAction = BulkLoadAction.Update;
     //                   break;
     //               default:
     //                   fieldAction = BulkLoadAction.Update;
     //                   break;
     //           }

                var unversioned = Current as UnversionedBulkField;
                var versioned = Current as VersionedBulkField;

                _fields[0] = Current.Item.Id;
                _fields[1] = string.IsNullOrEmpty(Current.Item.Name) ? DBNull.Value : (object)Current.Item.Name;
                _fields[2] = Current.Item.TemplateId != Guid.Empty ? (object)Current.Item.TemplateId : DBNull.Value;
                _fields[3] = (object)item.TemplateName ?? DBNull.Value;
                _fields[4] = Current.Item.MasterId;
                _fields[5] = Current.Item.ParentId;
                _fields[6] = (object)item.ItemPath ?? DBNull.Value;
                _fields[7] = (object)item.ItemLookupPath ?? DBNull.Value;
                _fields[8] = (object)item.DependsOnItemCreation ?? DBNull.Value;
                _fields[9] = Current.Item.Id;

                _fields[10] = item.LoadAction.ToString();
                _fields[11] = item.SourceInfo ?? DBNull.Value;

                _fields[12] = Current.Id;
                _fields[13] = (object)Current.Name ?? DBNull.Value;
                _fields[14] = unversioned != null ? (object)unversioned.Language : DBNull.Value;
                _fields[15] = versioned != null ? (object)versioned.Version : DBNull.Value;
                _fields[16] = Current.Value != null ? (object)Current.Value : DBNull.Value;

                // Data for blob will be fetched through GetStream method.
                _fields[17] = Current.Blob != null ? (object)null : DBNull.Value;
                _fields[18] = Current.IsBlob;

                _fields[19] = fieldAction.ToString();

                _fields[20] = Current.DependsOnCreate;
                _fields[21] = Current.DependsOnUpdate;

                // Will be used to remove duplicates after LookupExpressions have been processed
                _fields[22] = item.Deduplicate;

                // Will be detected in database, don't allow mistakes!
                //_fields[23] = unversioned == null; // IsShared
                //_fields[24] = unversioned != null && versioned == null; // IsUnversioned

                return true;
            }

            public override object GetValue(int i)
            {
                return _fields[i];
            }

            public override Stream GetStream(int ordinal)
            {
                if (ordinal != 17) return base.GetStream(ordinal);

                if (Current.Blob == null) return new MemoryStream();

                var stream = Current.Blob();
                if (stream != null) return stream;

                return new MemoryStream();
            }

            public override DataTable GetSchemaTable()
            {
                throw new NotSupportedException();
            }
        }

        protected class FieldRulesReader : AbstractEnumeratorReader<FieldRule>
        {
            private readonly object[] _fields;

            public override int FieldCount => 4;

            [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Not an issue in this case.")]
            public FieldRulesReader(Func<IEnumerator<FieldRule>> enumerator)
                : base(enumerator)
            {
                _fields = new object[FieldCount];
            }

            public override bool Read()
            {
                if (!base.Read())
                {
                    // Clear the fields.
                    for (var i = 0; i < _fields.Length; i++)
                        _fields[i] = null;
                    return false;
                }

                _fields[0] = Current.ItemId;
                _fields[1] = Current.SkipOnCreate;
                _fields[2] = Current.SkipOnUpdate;
                _fields[3] = Current.SkipOnDelete;

                return true;
            }

            public override object GetValue(int i)
            {
                return _fields[i];
            }
        }

        #endregion
    }
}
