using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster
{
	public class BulkItem
	{
		private string _itemPath;
		private readonly Dictionary<BulkFieldKey, BulkField> _fields = new Dictionary<BulkFieldKey, BulkField>();

		/// <summary>
		/// Item id.
		/// </summary>
		public Guid Id { get; private set; }
		/// <summary>
		/// Item name
		/// </summary>
		public string Name { get; private set; }
		public Guid TemplateId { get; private set; }
		public Guid MasterId { get; private set; }
		public Guid ParentId { get; set; }

		/// <summary>
		/// Sitecore path of the item.
		/// </summary>
		/// <remarks>Could be null.</remarks>
		public string ItemPath
		{
			get { return _itemPath; }
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				_itemPath = value;

				this.Name = _itemPath.Split('/').Last();
			}
		}


		public IEnumerable<BulkField> Fields => _fields.Values;
		public int FieldCount => _fields.Count;

		public BulkItem(Guid id, Guid templateId, Guid masterId, Guid parentId, string itemPath)
		{
			if (id == Guid.Empty)
				throw new ArgumentException("Id of item should not be an empty Guid.", nameof(id));
			if (string.IsNullOrWhiteSpace(itemPath)) throw new ArgumentNullException(nameof(itemPath));

			this.Id = id;
			this.TemplateId = templateId;
			this.MasterId = masterId;
			this.ParentId = parentId;
			this.ItemPath = itemPath;
		}

		protected BulkItem(BulkItem toCopy)
		{
			if (toCopy == null) throw new ArgumentNullException(nameof(toCopy));

			this.Id = toCopy.Id;
			this.TemplateId = toCopy.TemplateId;
			this.MasterId = toCopy.MasterId;
			this.ParentId = toCopy.ParentId;
			this.ItemPath = toCopy.ItemPath;

			_fields = toCopy._fields.ToDictionary(x => x.Key, x => x.Value);
		}

		private void AddField(BulkFieldKey key, string value, Func<Stream> blob = null, bool isBlob = false, string name = null,
			Action<BulkField> postProcessor = null)
		{
			if (value == null && !isBlob) return;

			BulkField field = null;
			if (key.Language == null && !key.Version.HasValue)
			{
				field = new SharedBulkField(this, key.FieldId, value, blob, isBlob, name);
				_fields.Add(key, field);
			}
			else if (key.Language != null && !key.Version.HasValue)
			{
				field = new UnversionedBulkField(this, key.FieldId, key.Language, value, blob, isBlob, name);
				_fields.Add(key, field);
			}
			else if (key.Language == null && key.Version.HasValue)
			{
				throw new ArgumentException("You cannot add a language specific field without a version.");
			}
			else
			{
				field = new VersionedBulkField(this, key.FieldId, key.Language, key.Version.Value, value, blob, isBlob, name);
				_fields.Add(key, field);
			}

			postProcessor?.Invoke(field);
		}

		/// <summary>
		/// Tries to add the field, returns false if the field with name and version already exists.
		/// </summary>
		public bool TryAddField(Guid id, string value, Func<Stream> blob = null, bool isBlob = false,
			string language = null, int? version = null, string name = null,
			Action<BulkField> postProcessor = null)
		{
			if (string.IsNullOrWhiteSpace(language)) language = null;

			var key = new BulkFieldKey(id, language, version);
			if (_fields.ContainsKey(key)) return false;

			AddField(key, value, blob, isBlob, name, postProcessor);
			return true;
		}

		public BulkItem AddField(Guid id, string value, Func<Stream> blob = null, bool isBlob = false,
			string language = null, int? version = null, string name = null,
			Action<BulkField> postProcessor = null)
		{
			if (value == null && !isBlob) return this;
			if (string.IsNullOrWhiteSpace(language)) language = null;
			AddField(new BulkFieldKey(id, language, version), value, blob, isBlob, name, postProcessor);
			return this;
		}

		public BulkItem AddField(TemplateField field, string value, Func<Stream> blob = null, bool isBlob = false,
			string language = null, int? version = null,
			Action<BulkField> postProcessor = null)
		{
			return AddField(field.ID.Guid, value, blob, isBlob, language, version, field.Name, postProcessor);
		}

		public BulkItem AddSharedField(Guid id, string value,
			Func<Stream> blob = null, bool isBlob = false, string name = null,
			Action<BulkField> postProcessor = null)
		{
			return AddField(id, value, blob, isBlob, null, null, name, postProcessor);
		}
		public BulkItem AddUnversionedField(Guid id, string language, string value,
			Func<Stream> blob = null, bool isBlob = false, string name = null,
			Action<BulkField> postProcessor = null)
		{
			return AddField(id, value, blob, isBlob, language, null, name, postProcessor);
		}
		public BulkItem AddVersionedField(Guid id, string language, int version, string value,
			Func<Stream> blob = null, bool isBlob = false, string name = null,
			Action<BulkField> postProcessor = null)
		{
			return AddField(id, value, blob, isBlob, language, version, name, postProcessor);
		}

		public BulkField GetField(Guid id, string language, int? version)
		{
			BulkField field;
			return _fields.TryGetValue(new BulkFieldKey(id, language, version), out field) ? field : null;
		}

		/// <summary>
		/// Statistics fields are necessary for correct working of Sitecore versions.
		/// If not correctly configured, publish might e.g not work.
		/// </summary>
		/// <param name="defaultLanguage">Default language will be added when no language version is present.</param>
		/// <param name="mandatoryLanguages">Language for which a version must be present.</param>
		/// <param name="timestampsOnly">Whether to only ensure created and updated fields.</param>
		/// <param name="forceUpdate">Forces modification date to always be set, not only when data is changed.</param>
		public void EnsureLanguageVersions(string defaultLanguage = null, IEnumerable<string> mandatoryLanguages = null,
			bool timestampsOnly = false, bool forceUpdate = false)
		{
			var user = global::Sitecore.Context.User.Name;
			var now = DateUtil.IsoNow;

			var versionsByLanguage = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

			// Detect versions by language from fields.
			foreach (var field in Fields.OfType<UnversionedBulkField>())
			{
				var versioned = field as VersionedBulkField;
				var version = versioned?.Version ?? 1;

				HashSet<int> versions;
				if (versionsByLanguage.TryGetValue(field.Language, out versions))
					versions.Add(version);
				else
					versionsByLanguage[field.Language] = new HashSet<int> { version };
			}

			// Ensure mandatory languages.
			foreach (var language in mandatoryLanguages ?? Enumerable.Empty<string>())
			{
				HashSet<int> versions;
				if (!versionsByLanguage.TryGetValue(language, out versions))
					versionsByLanguage[language] = new HashSet<int> { 1 };
			}

			// Add default version when no version is present.
			if (versionsByLanguage.Count == 0)
				versionsByLanguage[defaultLanguage ?? LanguageManager.DefaultLanguage.Name] = new HashSet<int> { 1 };

			foreach (var languageVersion in versionsByLanguage
				.SelectMany(pair => pair.Value.Select(x => new { Language = pair.Key, Version = x })))
			{
				TryAddField(FieldIDs.Created.Guid, now,
					language: languageVersion.Language, version: languageVersion.Version,
					name: "__Created", postProcessor: x => x.DependsOnCreate = true);

				TryAddField(FieldIDs.Updated.Guid, now,
					language: languageVersion.Language, version: languageVersion.Version,
					name: "__Updated", postProcessor: x =>
					{
						if (!forceUpdate)
							x.DependsOnCreate = x.DependsOnUpdate = true;
					});

				if (!timestampsOnly)
				{
					TryAddField(FieldIDs.CreatedBy.Guid, user,
						language: languageVersion.Language, version: languageVersion.Version,
						name: "__Created by", postProcessor: x => x.DependsOnCreate = true);

					TryAddField(FieldIDs.UpdatedBy.Guid, user,
						language: languageVersion.Language, version: languageVersion.Version,
						name: "__Updated by", postProcessor: x => x.DependsOnCreate = x.DependsOnUpdate = true);

					TryAddField(FieldIDs.Revision.Guid, Guid.NewGuid().ToString("D"),
						language: languageVersion.Language, version: languageVersion.Version,
						name: "__Revision", postProcessor: x => x.DependsOnCreate = x.DependsOnUpdate = true);
				}
			}
		}

		public string[] GetLanguages()
		{
			return Fields.OfType<UnversionedBulkField>().Select(x => x.Language).Distinct().ToArray();
		}

		public string GetParentPath()
		{
			if (string.IsNullOrWhiteSpace(ItemPath)) return null;
			var idx = ItemPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
			if (idx < 0 || ItemPath.Length == idx + 1) return null;
			return ItemPath.Substring(0, idx);
		}

		private class BulkFieldKey
		{
			public Guid FieldId { get; private set; }
			public string Language { get; private set; }
			public int? Version { get; private set; }

			public BulkFieldKey(Guid fieldId, string language, int? version)
			{
				FieldId = fieldId;
				Language = language;
				Version = version;
			}

			public override string ToString()
			{
				return $"{FieldId} ({Language}#{Version})";
			}

			private bool Equals(BulkFieldKey other)
			{
				return FieldId.Equals(other.FieldId) &&
				       string.Equals(Language, other.Language, StringComparison.OrdinalIgnoreCase) &&
				       Version == other.Version;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((BulkFieldKey)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (FieldId.GetHashCode() * 397) ^
					       (Language == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Language)) ^
					       Version.GetHashCode();
				}
			}
		}
	}
}