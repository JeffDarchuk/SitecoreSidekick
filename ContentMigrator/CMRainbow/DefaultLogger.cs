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
using Sitecore.Diagnostics;

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
			string status = $"{DateTime.Now:h:mm:ss tt} [CREATED] Created new item {targetItem.DisplayName} - {targetItem.ID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			BeginEvent(movedItem, "Moved", GetSrc(ThemeManager.GetIconImage(movedItem, 32, 32, "", "")), false);
			string status = $"{DateTime.Now:h:mm:ss tt} [MOVED] Moved Item {movedItem.ID} from {oldParentItem.ID} to {newParentItem.ID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void RemovingOrphanedVersion(Item versionToRemove)
		{
			BeginEvent(versionToRemove.DisplayName + " v"+versionToRemove.Version.Number, versionToRemove.ID.ToString(), versionToRemove.Paths.FullPath , "Removed Version", GetSrc(ThemeManager.GetIconImage(versionToRemove, 32, 32, "", "")), versionToRemove.Database.Name, false);
			string status = $"{DateTime.Now:h:mm:ss tt} [REMOVED VERSION] Removing orphaned item version {versionToRemove.DisplayName} v{versionToRemove.Version.Number} - {versionToRemove.ID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);

		}

		public void RenamedItem(Item targetItem, string oldName)
		{
			BeginEvent(targetItem, "Renamed", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			string status = $"{DateTime.Now:h:mm:ss tt} [RENAMED] Renaming item {targetItem.DisplayName} - {targetItem.ID} from name {oldName} to {targetItem.Name}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			BeginEvent(targetItem, "Branch Change", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			string status = $"{DateTime.Now:h:mm:ss tt} [BRANCH MOVED] Branch Template moved for item {targetItem.DisplayName} - {targetItem.ID} from {oldBranchId} to {targetItem.BranchId}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			BeginEvent(targetItem, "Template Change", GetSrc(ThemeManager.GetIconImage(targetItem, 32, 32, "", "")), false);
			string status = $"{DateTime.Now:h:mm:ss tt} [TEMPLATE CHANGED] Template changed for item {targetItem.DisplayName} - {targetItem.ID} from {oldTemplate.ID} to {targetItem.TemplateID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void AddedNewVersion(Item newVersion)
		{
			BeginEvent(newVersion, "New Version", GetSrc(ThemeManager.GetIconImage(newVersion, 32, 32, "", "")), false);
			string status = $"{DateTime.Now:h:mm:ss tt} [ADDED VERSION] Added new version for item {newVersion.DisplayName} - {newVersion.ID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void WroteBlobStream(Item item, IItemFieldValue field)
		{
			BeginEvent(item, "Wrote Blob", GetSrc(ThemeManager.GetIconImage(item, 32, 32, "", "")), false);
			string status = $"{DateTime.Now:h:mm:ss tt} [WROTE BLOB] Wrote blob stream for item {item.DisplayName} - {item.ID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue)
		{
			string status = $"{DateTime.Now:h:mm:ss tt} [FIELD CHANGED] Field {field.NameHint} value changed for item {item.DisplayName} - {item.ID} from {oldValue} to {item[new ID(field.FieldId)]}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
			if (!LinesSupport.ContainsKey(item.ID.Guid.ToString()))
				LinesSupport[item.ID.Guid.ToString()] = new {Events = new Dictionary<string, List<Tuple<string, string>>>()};
			if (!LinesSupport[item.ID.Guid.ToString()].Events.ContainsKey(item.Language.Name + " v" + item.Version.Number))
				LinesSupport[item.ID.Guid.ToString()].Events[item.Language.Name + " v" + item.Version.Number] = new List<Tuple<string, string>>();
			LinesSupport[item.ID.Guid.ToString()].Events[item.Language.Name + " v" + item.Version.Number].Add(new Tuple<string, string>(field.NameHint,
				HtmlDiff.HtmlDiff.Execute(HttpUtility.HtmlEncode(oldValue), HttpUtility.HtmlEncode(item[new ID(field.FieldId)]))));
		}

		public void ResetFieldThatDidNotExistInSerialized(Field field)
		{
			string status = $"{DateTime.Now:h:mm:ss tt} [FIELD RESET] Reset a field {field.Name} that doesn't exist in item {field.Item.DisplayName} - {field.Item.ID}";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}

		public void SkippedPastingIgnoredField(Item item, IItemFieldValue field)
		{
			string status = $"{DateTime.Now:h:mm:ss tt} [SKIPPED] Skipped an ignored field {field.NameHint} in item {item.DisplayName} - {item.ID} ";
			LoggerOutput.Add(status);
			Log.Info(status, this);
		}
	}
}
