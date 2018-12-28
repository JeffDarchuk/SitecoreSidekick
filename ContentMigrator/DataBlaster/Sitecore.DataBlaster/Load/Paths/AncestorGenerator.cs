using System;
using System.Collections.Generic;
using System.Linq;
using ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util;
using Sitecore.Data.Templates;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Paths
{
	/// <summary>
	/// Allows generating ancestor in a stream of bulk items.
	/// </summary>
    public class AncestorGenerator
    {
	    private readonly GuidUtility _guidUtility;

	    public AncestorGenerator(GuidUtility guidUtility = null)
	    {
		    _guidUtility = guidUtility ?? new GuidUtility();
	    }

        /// <summary>
        /// Returns ancestor items for every needed level between the item and the root.
        /// </summary>
        /// <param name="item">Item to generate ancestors for.</param>
        /// <param name="root">Root that marks the ancestor that should already exist or be present in the stream.</param>
        /// <param name="ancestorTemplate">Template for the ancestors to generate.</param>
        /// <param name="context">Context to generate the ancestors in.</param>
        /// <returns>Stream of generated ancestors, migh be empty if ancestors were already created, 
        /// or it might be partial if shared ancestors were already created for another item in the same context.</returns>
        public virtual IEnumerable<BulkLoadItem> EnsureAncestorBulkItems(BulkLoadItem item, ItemReference root,
            Template ancestorTemplate, BulkLoadContext context)
        {
            return EnsureAncestorBulkItems(item, root, ancestorTemplate, item.Id, context);
        }

        protected virtual IEnumerable<BulkLoadItem> EnsureAncestorBulkItems(BulkLoadItem item,
            ItemReference root, Template ancestorTemplate, Guid dependsOnItemCreation,
            BulkLoadContext context)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (ancestorTemplate == null) throw new ArgumentNullException(nameof(ancestorTemplate));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!item.ItemPath.StartsWith(root.ItemPath))
                throw new ArgumentException("Bulk item should be a descendant of the root.");

            // Detect all the ancestors to generate.
            var ancestorNames = item.ItemPath.Substring(root.ItemPath.Length)
                .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            ancestorNames = ancestorNames.Take(ancestorNames.Length - 1).ToArray(); // Don't include item.

            // Generate all ancestors.
            var parent = root;
            foreach (var name in ancestorNames)
            {
                var itemPath = $"{parent.ItemPath}/{name}";

                // Maybe we have already generated this path in a previous call to EnsureAncestors within the same context.
                var itemId = context.GetProcessedPath(itemPath);
                if (itemId.HasValue)
                {
                    // Continue with next.
                    parent = new ItemReference(itemId.Value, itemPath);
                    continue;
                }

                // Generate stable guid for this ancestor item within its parent.
                itemId = _guidUtility.Create(parent.ItemId, name);

                // In case of forced updates, also update the child item. This will result in an ItemChange which makes sure the item gets re-published.
                var childLoadAction = context.ForceUpdates ? BulkLoadAction.Update : BulkLoadAction.AddOnly;

                // Create new bulk item.
                var child = new BulkLoadItem(childLoadAction, itemId.Value, ancestorTemplate.ID.Guid,
                    Guid.Empty, parent.ItemId, itemPath,
                    templateName: ancestorTemplate.Name, sourceInfo: item.SourceInfo)
                {
                    // Only create ancestor when child is created, skip ancestor creation when child is updated.
                    DependsOnItemCreation = dependsOnItemCreation
                };

                // Attach asap to context, because import profiles might eagerly bucket.
                context.TrackPathAndTemplateInfo(child);

                yield return child;

                parent = new ItemReference(child.Id, child.ItemPath);
            }

            // Reset parent reference for initial item.
            item.ParentId = parent.ItemId;
        }
    }
}