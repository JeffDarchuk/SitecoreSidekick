using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Web;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;

namespace ScsContentMigrator.CMRainbow
{
	public class DefaultLogger : IDefaultDeserializerLogger
	{
		public List<dynamic> Lines = new List<dynamic>();
		public ConcurrentDictionary<string, dynamic> LinesSupport { get; } = new ConcurrentDictionary<string, dynamic>();
		public List<string> LoggerOutput { get; } = new List<string>();
		private readonly object _listLocker = new object();
		public void BeginEvent(Item data, string status, string icon, bool keepOpen)
		{
			BeginEvent(data.Name, data.ID.ToString(), data.Paths.FullPath, status, icon, data.Database.Name, keepOpen);
		}
		public void BeginEvent(IItemData data, string status, string icon, bool keepOpen)
		{
			BeginEvent(data.Name, data.Id.ToString(), data.Path, status, icon, data.DatabaseName, keepOpen);
		}

		public void BeginEvent(string name, string id, string path, string status, string icon, string database, bool keepOpen)
		{
			dynamic cur = new ExpandoObject();
			cur.Name = name;
			cur.Id = id;
			cur.Path = path;
			cur.Icon = icon;
			cur.Operation = status.Trim().Replace(" ", "_");
			cur.DatabaseName = database;
			lock (_listLocker)
			{
				if (LinesSupport.ContainsKey(id))
					cur.Events = LinesSupport[id].Events;
				else
					cur.Events = new Dictionary<string, List<Tuple<string, string>>>();
			}
			LinesSupport[id] = cur;
			if (!keepOpen)
				CompleteEvent(id);
		}

		public void CompleteEvent(string id)
		{
			Lines.Add(LinesSupport[id]);
		}
		public string GetSrc(string imgTag)
		{
			int i1 = imgTag.IndexOf("src=\"", StringComparison.Ordinal) + 5;
			int i2 = imgTag.IndexOf("\"", i1, StringComparison.Ordinal);
			return imgTag.Substring(i1, i2 - i1);
		}
		public void CreatedNewItem(Item targetItem)
		{
			BeginEvent(targetItem, "Created", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			LoggerOutput.Add("Created new item at target: " +targetItem.ID);
		}

		public void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			BeginEvent(movedItem, "Moved", GetSrc(ThemeManager.GetIconImage(movedItem, 32, 32, "", "")), false);
			LoggerOutput.Add($"Moved Item {movedItem.ID} from {oldParentItem.ID} to {newParentItem.ID}");
		}

		public void RemovingOrphanedVersion(Item versionToRemove)
		{
			BeginEvent(versionToRemove, "Removed", GetSrc(ThemeManager.GetIconImage(versionToRemove, 32, 32, "", "")), false);
			LoggerOutput.Add($"Removing orphaned item {versionToRemove.ID}");

		}

		public void RenamedItem(Item targetItem, string oldName)
		{
			BeginEvent(targetItem, "Renamed", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			LoggerOutput.Add($"Renaming item {targetItem.ID} from name {oldName} to {targetItem.Name}");
		}

		public void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			BeginEvent(targetItem, "Branch Change", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			LoggerOutput.Add($"Branch Template moved for item {targetItem.ID} from {oldBranchId} to {targetItem.BranchId}");
		}

		public void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			BeginEvent(targetItem, "Template Change", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			LoggerOutput.Add($"Template changed for item {targetItem.ID} from {oldTemplate.ID} to {targetItem.TemplateID}");
		}

		public void AddedNewVersion(Item newVersion)
		{
			BeginEvent(newVersion, "New Version", GetSrc(ThemeManager.GetIconImage(newVersion, 32, 32, "", "")), false);
			LoggerOutput.Add($"Added new version for item {newVersion.ID}");
		}

		public void WroteBlobStream(Item item, IItemFieldValue field)
		{
			BeginEvent(item, "Wrote Blob", GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")), false);
			LoggerOutput.Add($"Wrote blob stream for item {item.ID}");
		}

		public void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue)
		{
			
			LoggerOutput.Add($"Field {field.NameHint} value changed for item {item.ID} from {oldValue} to {item[new ID(field.FieldId)]}");
			if (!LinesSupport.ContainsKey(item.ID.Guid.ToString()))
				LinesSupport[item.ID.Guid.ToString()] = new {Events = new Dictionary<string, List<Tuple<string, string>>>()};
			if (!LinesSupport[item.ID.Guid.ToString()].Events.ContainsKey(item.Language.Name))
				LinesSupport[item.ID.Guid.ToString()].Events[item.Language.Name] = new List<Tuple<string, string>>();
			LinesSupport[item.ID.Guid.ToString()].Events[item.Language.Name].Add(new Tuple<string, string>(field.NameHint,
				HtmlDiff.HtmlDiff.Execute(HttpUtility.HtmlEncode(oldValue), HttpUtility.HtmlEncode(item[new ID(field.FieldId)]))));
		}

		public void ResetFieldThatDidNotExistInSerialized(Field field)
		{
			LoggerOutput.Add($"Reset a field {field.Name} that doesn't exist in serialized item ");
		}

		public void SkippedPastingIgnoredField(Item item, IItemFieldValue field)
		{
			LoggerOutput.Add($"Skipped an ignored field {field.NameHint} in item {item.ID} ");
		}
	}
}
