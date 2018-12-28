using System;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Read
{
    /// <summary>
    /// Describes an item without a specific version.
    /// </summary>
    public class ItemHeader
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ItemPath { get; set; }
        public Guid TemplateId { get; set; }
        public Guid MasterId { get; set; }
        public Guid ParentId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public ItemHeader(Guid id, string name, string itemPath,
            Guid templateId, Guid masterId, Guid parentId,
            DateTime created, DateTime updated)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            this.Id = id;
            this.Name = name;
            this.ItemPath = itemPath;
            this.TemplateId = templateId;
            this.MasterId = masterId;
            this.ParentId = parentId;
            this.Created = created;
            this.Updated = updated;
        }

        #region Equality members

        protected bool Equals(ItemHeader other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ItemHeader;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}