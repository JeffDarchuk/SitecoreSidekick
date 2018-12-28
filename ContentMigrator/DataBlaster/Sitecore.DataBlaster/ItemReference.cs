using System;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster
{
    /// <summary>
    /// Reference to an item by id and path.
    /// </summary>
    public class ItemReference
    {
        public Guid ItemId { get; private set; }
        public string ItemPath { get; private set; }

        public ItemReference(Guid itemId, string itemPath)
        {
            if (String.IsNullOrWhiteSpace(itemPath)) throw new ArgumentNullException(nameof(itemPath));

            ItemId = itemId;
            ItemPath = itemPath;
        }
    }
}