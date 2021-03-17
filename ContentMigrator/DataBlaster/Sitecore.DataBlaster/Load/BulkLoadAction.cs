namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
    public enum BulkLoadAction
    {
        /// <summary>
        /// Adds items and adds missing fields, but doesn't update any fields.
        /// </summary>
        AddOnly = 0,

		/// <summary>
		/// Only adds items that don't exist yet, does NOT add missing fields to existing items.
		/// </summary>
		AddItemOnly = 6, // Keep original numbering for backwards compatibility.

        /// <summary>
        /// Adds items, missing fields to existing items and updates/overwrites fields for which the data is different.
        /// </summary>
        Update = 1,

        /// <summary>
        /// Adds and updates fields for existing items only.
        /// </summary>
        UpdateExistingItem = 2,

        /// <summary>
        /// Reverts items to the provided state, removing redundant fields as well.
        /// Does NOT remove children that are not provided in the dataset.
        /// </summary>
        Revert = 3,

		/// <summary>
		/// Reverts items to the provided state, removing redundant fields as well.
		/// Removes descendants that are not provided in the dataset.
		/// </summary>
		RevertTree = 4

        //Delete (todo)
    }
}