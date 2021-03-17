using System;
using System.IO;
using Rainbow.Model;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load;
using Sitecore;
using Convert = System.Convert;

namespace Sidekick.ContentMigrator.DataBlaster
{
	/// <summary>
	/// Maps a Unicorn item to a bulk load item.
	/// </summary>
	public class ItemMapper
	{
		public virtual BulkLoadItem ToBulkLoadItem(IItemData itemData, BulkLoadContext context, BulkLoadAction loadAction)
		{
			if (itemData == null) throw new ArgumentNullException(nameof(itemData));
			if (context == null) throw new ArgumentNullException(nameof(context));

			var bulkItem = new BulkLoadItem(
				loadAction,
				itemData.Id,
				itemData.TemplateId,
				itemData.BranchId,
				itemData.ParentId,
				itemData.Path,
				sourceInfo: itemData.SerializedItemId);

			foreach (var sharedField in itemData.SharedFields)
			{
				AddSyncField(context, bulkItem, sharedField);
			}

			foreach (var languagedFields in itemData.UnversionedFields)
			{
				foreach (var field in languagedFields.Fields)
				{
					AddSyncField(context, bulkItem, field, languagedFields.Language.Name);
				}
			}

			foreach (var versionFields in itemData.Versions)
			{
				foreach (var field in versionFields.Fields)
				{
					AddSyncField(context, bulkItem, field, versionFields.Language.Name, versionFields.VersionNumber);
				}

				AddStatisticsFieldsWhenMissing(bulkItem, versionFields.Language.Name, versionFields.VersionNumber);
			}

			// Serialized items don't contain the original blob id.
			context.LookupBlobIds = true;

			return bulkItem;
		}

		protected virtual void AddSyncField(BulkLoadContext context, BulkLoadItem bulkItem, IItemFieldValue itemField, string language = null, int versionNumber = 1)
		{
			var fieldId = itemField.FieldId;
			var fieldValue = itemField.Value;
			var fieldName = itemField.NameHint;
			var isBlob = itemField.BlobId.HasValue;

			Func<Stream> blob = null;
			if (isBlob)
			{
				byte[] blobBytes;
				try
				{
					blobBytes = Convert.FromBase64String(fieldValue);
				}
				catch (Exception ex)
				{
					blobBytes = new byte[] { };
					context.Log.Error(
						$"Unable to read blob from field '{fieldId}' in item with id '{bulkItem.Id}', " +
						$"item path '{bulkItem.ParentId}' and source info '{bulkItem.SourceInfo}', defaulting to empty value.", ex);
				}
				blob = () => new MemoryStream(blobBytes);

				// Field value needs to be set to the blob id.
				fieldValue = itemField.BlobId.Value.ToString("B").ToUpper();
			}

			if (language == null)
			{
				bulkItem.AddSharedField(fieldId, fieldValue, blob, isBlob, fieldName);
			}
			else
			{
				bulkItem.AddVersionedField(fieldId, language, versionNumber, fieldValue, blob, isBlob, fieldName);
			}
		}

		protected virtual void AddStatisticsFieldsWhenMissing(BulkLoadItem bulkItem, string language, int versionNumber = 1)
		{
			var user = global::Sitecore.Context.User.Name;

			if (bulkItem.GetField(FieldIDs.Created.Guid, language, versionNumber) == null)
				bulkItem.AddVersionedField(FieldIDs.Created.Guid, language, versionNumber, DateUtil.IsoNow, name: "__Created");

			if (bulkItem.GetField(FieldIDs.CreatedBy.Guid, language, versionNumber) == null)
				bulkItem.AddVersionedField(FieldIDs.CreatedBy.Guid, language, versionNumber, user, name: "__Created by");

			if (bulkItem.LoadAction == BulkLoadAction.Update || bulkItem.LoadAction == BulkLoadAction.UpdateExistingItem)
			{
				if (bulkItem.GetField(FieldIDs.UpdatedBy.Guid, language, versionNumber) == null)
					bulkItem.AddVersionedField(FieldIDs.UpdatedBy.Guid, language, versionNumber, user, name: "__Updated");

				if (bulkItem.GetField(FieldIDs.Updated.Guid, language, versionNumber) == null)
					bulkItem.AddVersionedField(FieldIDs.Updated.Guid, language, versionNumber, DateUtil.IsoNowWithTicks, name: "__Updated by");
			}
		}
	}
}