using System;
using System.Collections.Generic;
using System.Dynamic;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;

namespace ScsContentMigrator.CMRainbow
{
	public class DefaultLogger : IDefaultDeserializerLogger
	{
		public List<dynamic> Lines { get; } = new List<dynamic>();
		public List<string> LoggerOutput { get; } = new List<string>();
		private readonly object _listLocker = new object();
		public void RecordEvent(Item data, string status, string icon)
		{
			RecordEvent(data.Name, data.ID.ToString(), data.Paths.FullPath, status, icon, data.Database.Name);
		}
		public void RecordEvent(IItemData data, string status, string icon)
		{
			RecordEvent(data.Name, data.Id.ToString(), data.Path, status, icon, data.DatabaseName);
		}

		public void RecordEvent(string name, string id, string path, string status, string icon, string database)
		{
			dynamic cur = new ExpandoObject();
			cur.Name = name;
			cur.Id = id;
			cur.Path = path;
			cur.Icon = icon;
			cur.Operation = status.Trim().Replace(" ", "_");
			cur.DatabaseName = database;
			lock (_listLocker)
				Lines.Add(cur);
		}
		public string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
		public void CreatedNewItem(Item targetItem)
		{
			RecordEvent(targetItem, "Created", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")));
			LoggerOutput.Add("Created new item at target: " +targetItem.ID);
		}

		public void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			RecordEvent(movedItem, "Moved", GetSrc(ThemeManager.GetIconImage(movedItem, 32, 32, "", "")));
			LoggerOutput.Add($"Moved Item {movedItem.ID} from {oldParentItem.ID} to {newParentItem.ID}");
		}

		public void RemovingOrphanedVersion(Item versionToRemove)
		{
			RecordEvent(versionToRemove, "Removed", GetSrc(ThemeManager.GetIconImage(versionToRemove, 32, 32, "", "")));
			LoggerOutput.Add($"Removing orphaned item {versionToRemove.ID}");

		}

		public void RenamedItem(Item targetItem, string oldName)
		{
			RecordEvent(targetItem, "Renamed", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")));
			LoggerOutput.Add($"Renaming item {targetItem.ID} from name {oldName} to {targetItem.Name}");
		}

		public void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			RecordEvent(targetItem, "Branch Change", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")));
			LoggerOutput.Add($"Branch Template moved for item {targetItem.ID} from {oldBranchId} to {targetItem.BranchId}");
		}

		public void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			RecordEvent(targetItem, "Template Change", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")));
			LoggerOutput.Add($"Template changed for item {targetItem.ID} from {oldTemplate.ID} to {targetItem.TemplateID}");
		}

		public void AddedNewVersion(Item newVersion)
		{
			RecordEvent(newVersion, "New Version", GetSrc(ThemeManager.GetIconImage(newVersion, 32, 32, "", "")));
			LoggerOutput.Add($"Added new version for item {newVersion.ID}");
		}

		public void WroteBlobStream(Item item, IItemFieldValue field)
		{
			RecordEvent(item, "Wrote Blob", GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")));
			LoggerOutput.Add($"Wrote blob stream for item {item.ID}");
		}

		public void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue)
		{
			//LoggerOutput.Add($"Field {field.NameHint} value changed for item {item.ID} from {oldValue} to {item[new ID(field.FieldId)]}");
		}

		public void ResetFieldThatDidNotExistInSerialized(Field field)
		{

			//LoggerOutput.Add($"Reset a field {field.Name} that doesn't exist in serialized item ");

		}

		public void SkippedPastingIgnoredField(Item item, IItemFieldValue field)
		{
			//LoggerOutput.Add($"Skipped an ignored field {field.NameHint} in item {item.ID} ");

		}
	}
}
