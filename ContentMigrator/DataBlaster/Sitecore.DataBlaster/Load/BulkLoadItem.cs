using System;
using Sitecore.Buckets.Util;
using Sitecore.Data.Items;
using Sitecore.Data.Templates;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
	public class BulkLoadItem : BulkItem
	{
		private string _itemLookupPath;

		/// <summary>
		/// Load action to perform on this item.
		/// </summary>
		public BulkLoadAction LoadAction { get; set; }

		/// <summary>
		/// Item path to use for lookups.
		/// Can contain /* as wildcard for single item path part.
		/// Can contain /** as greedy wildcard for multiple item path parts.
		/// Can contain /.. to navigate to parent.
		/// E.g.: /sitecore/content/sites/SiteName/Products/**/Aricle U12345/..
		/// </summary>
		/// <remarks>To support wildcards, will need the SqlClr data blaster module.</remarks>
		public string ItemLookupPath
		{
			get { return _itemLookupPath; }
			set
			{
				if (value != null)
				{
					if (value.Contains("*/*"))
						throw new ArgumentException("Path expression cannot contain consecutive wildcards.");
					if (value.Contains("*/.."))
						throw new ArgumentException("Path expression cannot contain parent navigation immediately after wildcard.");
					if (value.EndsWith("/*") || value.EndsWith("/**"))
						throw new ArgumentException("Path expression cannot end with wildcard.");
				}
				_itemLookupPath = value;
			}
		}

		/// <summary>
		/// Whether to deduplicate this item based on its path, after lookupexpressions have been processed.
		/// </summary>
		public bool Deduplicate { get; set; }

		/// <summary>
		/// Id of item which must be created for this item to be created as well.
		/// When other item is only updated, this entire item will be skipped.
		/// </summary>
		public Guid? DependsOnItemCreation { get; set; }

		/// <summary>
		/// Whether or not this item has already been bucketed.
		/// </summary>
		public bool Bucketed { get; set; }

		/// <summary>
		/// Templatename, only used for diagnostics.
		/// </summary>
		public string TemplateName { get; set; }

		/// <summary>
		/// Diagnostic source information for this item.
		/// </summary>
		public object SourceInfo { get; set; }

		public BulkLoadItem(BulkLoadAction loadAction, Guid id, Guid templateId, Guid masterId, Guid parentId,
			string itemPath, string templateName = null, object sourceInfo = null)
			: base(id, templateId, masterId, parentId, itemPath)
		{
			if (templateId == Guid.Empty && loadAction != BulkLoadAction.UpdateExistingItem)
				throw new ArgumentException("Template id of item should not be an empty Guid.", nameof(id));

			this.LoadAction = loadAction;
			this.TemplateName = templateName;
			this.SourceInfo = sourceInfo;

			// When creating bucket folder items, we don't want those items to be bucketed again.
			if (BucketConfigurationSettings.BucketTemplateId.Guid.Equals(templateId))
				Bucketed = true;
		}

		public BulkLoadItem(BulkLoadAction loadAction, BulkItem item)
			: base(item)
		{
			this.LoadAction = loadAction;

			// When creating bucket folder items, we don't want those items to be bucketed again.
			if (BucketConfigurationSettings.BucketTemplateId.Guid.Equals(TemplateId))
				Bucketed = true;
		}

		public BulkLoadItem(BulkLoadAction loadAction, Guid templateId, string itemPath,
			string templateName = null, object sourceInfo = null)
			: this(loadAction, Guid.NewGuid(), templateId, Guid.Empty, Guid.NewGuid(),
				itemPath, templateName, sourceInfo)
		{
		}

		public BulkLoadItem(BulkLoadAction loadAction, Guid templateId, string itemPath, Guid id,
			string templateName = null, object sourceInfo = null)
			: this(loadAction, id, templateId, Guid.Empty, Guid.NewGuid(),
				itemPath, templateName, sourceInfo)
		{
		}

		public BulkLoadItem(BulkLoadAction loadAction, Template template, Item parent, string name)
			: this(loadAction, Guid.NewGuid(), template.ID.Guid, Guid.Empty, parent.ID.Guid,
				parent.Paths.Path + "/" + name, template.Name)
		{
		}

		/// <summary>
		/// Method to help chaining.
		/// </summary>
		/// <param name="action">Action to perform.</param>
		/// <returns>This object.</returns>
		public BulkLoadItem Do(Action<BulkLoadItem> action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			action(this);
			return this;
		}
	}
}