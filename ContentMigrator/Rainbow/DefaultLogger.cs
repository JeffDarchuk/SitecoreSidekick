using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace SitecoreSidekick.Rainbow
{
	public class DefaultLogger : IDefaultDeserializerLogger
	{
		public void CreatedNewItem(Item targetItem)
		{
			//Log.Info("Created new item at target: "+targetItem.ID, this);
		}

		public void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			//Log.Info($"Moved Item {movedItem.ID} from {oldParentItem.ID} to {newParentItem.ID}", this);
		}

		public void RemovingOrphanedVersion(Item versionToRemove)
		{
			//Log.Info($"Removing orphaned item {versionToRemove.ID}", this);

		}

		public void RenamedItem(Item targetItem, string oldName)
		{
			//Log.Info($"Renaming item {targetItem.ID} from name {oldName} to {targetItem.Name}", this);

		}

		public void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			//Log.Info($"Branch Template moved for item {targetItem.ID} from {oldBranchId} to {targetItem.BranchId}", this);

		}

		public void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			//Log.Info($"Template changed for item {targetItem.ID} from {oldTemplate.ID} to {targetItem.TemplateID}", this);

		}

		public void AddedNewVersion(Item newVersion)
		{
			//Log.Info($"Added new version for item {newVersion.ID}", this);

		}

		public void WroteBlobStream(Item item, IItemFieldValue field)
		{
			//Log.Info($"Wrote blob stream for item {item.ID}", this);

		}

		public void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue)
		{
			//Log.Info($"Field {field.NameHint} value changed for item {item.ID} from {oldValue} to {item[new ID(field.FieldId)]}", this);

		}

		public void ResetFieldThatDidNotExistInSerialized(Field field)
		{
			//Log.Info($"Reset a field {field.Name} that doesn't exist in serialized item ", this);

		}

		public void SkippedPastingIgnoredField(Item item, IItemFieldValue field)
		{
			//Log.Info($"Skipped an ignored field {field.NameHint} in item {item.ID} ", this);

		}
	}
}
