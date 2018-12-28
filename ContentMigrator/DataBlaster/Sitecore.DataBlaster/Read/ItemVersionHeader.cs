using System;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Read
{
    /// <summary>
    /// Describes a specific version of an item.
    /// </summary>
    public class ItemVersionHeader : ItemHeader
    {
        public string Language { get; set; }
        public int Version { get; set; }

        public ItemVersionHeader(Guid id, string name, string itemPath,
            Guid templateId, Guid masterId, Guid parentId,
            string language, int version,
            DateTime created, DateTime updated)
            : base(id, name, itemPath, templateId, masterId, parentId, created, updated)
        {
            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));

            this.Language = language;
            this.Version = version;
        }

        #region Equality members

        protected bool Equals(ItemVersionHeader other)
        {
            return base.Equals(other) && string.Equals(Language, other.Language, StringComparison.InvariantCultureIgnoreCase) && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ItemVersionHeader;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.InvariantCultureIgnoreCase.GetHashCode(Language);
                hashCode = (hashCode * 397) ^ Version;
                return hashCode;
            }
        }

        #endregion
    }
}