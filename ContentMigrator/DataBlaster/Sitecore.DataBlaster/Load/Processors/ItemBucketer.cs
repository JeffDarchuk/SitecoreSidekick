using System;
using System.Collections.Generic;
using System.Linq;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Paths;
using Sitecore;
using Sitecore.Buckets.Extensions;
using Sitecore.Buckets.Util;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Managers;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Processors
{
    /// <summary>
    /// Generates bucket structure for bulk items.
    /// Assumes that provided bulk items are supplied as direct children of the bucket item.
    /// </summary>
    public class ItemBucketer : IItemProcessor
    {
	    private static readonly Guid BucketFolderTemplate = Guid.Parse(global::Sitecore.Buckets.Util.Constants.BucketFolder);

		private readonly AncestorGenerator _ancestorGenerator;

	    public ItemBucketer(AncestorGenerator ancestorGenerator = null)
	    {
		    _ancestorGenerator = ancestorGenerator ?? new AncestorGenerator();
	    }

        public IEnumerable<BulkLoadItem> Process(BulkLoadContext context, IEnumerable<BulkLoadItem> items)
        {
            return !context.BucketIfNeeded 
                ? items 
                : items.SelectMany(item => Bucket(item, context, true));
        }

        protected virtual IEnumerable<BulkLoadItem> Bucket(BulkLoadItem item, BulkLoadContext context, bool skipIfNotBucket)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (item.Bucketed)
            {
                yield return item;
                yield break;
            }

            var db = Factory.GetDatabase(context.Database);
            var bucketItem = db.GetItem(new ID(item.ParentId));
            if (bucketItem == null && !skipIfNotBucket)
                throw new ArgumentException(
                    $"Unable to bucket item because parent with id '{item.ParentId}' doesn't exist.");
            if (bucketItem == null)
            {
                yield return item;
                yield break;
            }

            if (!bucketItem.IsABucket())
            {
                if (skipIfNotBucket)
                {
                    yield return item;
                    yield break;
                }
                throw new InvalidOperationException(
                    $"Item with path '{bucketItem.Paths.Path}' is not bucket.");
            }

            // Get template for ancestors.
            var bucketFolderTemplate = TemplateManager.GetTemplate(new ID(BucketFolderTemplate), db);

            // Default to configured bucket folder generation.
            if (context.BucketFolderPath == null)
                context.BucketFolderPath = new BucketFolderPathResolver();

            // Try to find out when item was created.
            var createdField = item.Fields.FirstOrDefault(
                x => x.Id == FieldIDs.Created.Guid && !string.IsNullOrWhiteSpace(x.Value));
            var created = createdField == null ? DateTime.UtcNow : DateUtil.IsoDateToDateTime(createdField.Value);

            var bucketFolderPath = context.BucketFolderPath.GetFolderPath(db,
                item.Name.Replace(' ', '0'), // Sitecore's name based bucket folder generation doesn't handle spaces. 
                new ID(item.TemplateId), new ID(item.Id), bucketItem.ID, created);
            item.ItemPath = bucketItem.Paths.Path + "/" + bucketFolderPath + "/" + item.Name;
            item.Bucketed = true;

            // Check if bucket folder path depends on creation date.
            if (context.GetState<bool?>("Load.BucketByDate") == null)
            {
                created = new DateTime(2000, 01, 01, 01, 01, 01);
                var testBucketFolderPath = context.BucketFolderPath.GetFolderPath(db,
                    item.Name, new ID(item.TemplateId), new ID(item.Id), bucketItem.ID, created);
                if (!bucketFolderPath.Equals(testBucketFolderPath, StringComparison.OrdinalIgnoreCase))
                {
                    context.Log.Warn(
                        "Bucket strategy is based on creation date, this will affect import performance, " +
                        "but might also not be repeatable.");
                    context.GetOrAddState<bool?>("Load.BucketByDate", () => true);
                }
            }

            // If bucketing depends on date, lookup item by name with wildcard.
            var dependsOnDate = context.GetState<bool?>("Load.BucketsByDate", false);
            if (dependsOnDate.GetValueOrDefault(false))
            {
                context.LookupItemIds = true;
                item.ItemLookupPath = bucketItem.Paths.Path + "/**/" + item.Name;
            }

            foreach (var ancestor in _ancestorGenerator.EnsureAncestorBulkItems(item,
                new ItemReference(bucketItem.ID.Guid, bucketItem.Paths.Path), bucketFolderTemplate, context))
            {
                // Make sure ancestor doesn't get re-bucketed.
                ancestor.Bucketed = true;
                yield return ancestor;
            }

            yield return item;
        }
    }
}