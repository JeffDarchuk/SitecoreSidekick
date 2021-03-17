using System;
using System.IO;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster
{
    public abstract class BulkField
    {
        public BulkItem Item { get; private set; }

        /// <summary>
        /// Field id.
        /// </summary>
        public Guid Id { get; private set; }
        /// <summary>
        /// Fieldname, only used for diagnostics.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Field value.
        /// </summary>
        public string Value { get; set; }

        public Func<Stream> Blob { get; set; }

        /// <summary>
        /// Field maybe a blob field although it may not have a blob stream.
        /// Can occur in case of same blob for different fields.
        /// </summary>
        public bool IsBlob { get; private set; }

        /// <summary>
        /// When set, this field is only processed when item is created in database.
        /// </summary>
        public bool DependsOnCreate { get; set; }

        /// <summary>
        /// When set, this field is only processed when item is updated in database.
        /// </summary>
        public bool DependsOnUpdate { get; set; }

        protected BulkField(BulkItem item, Guid id, string value, Func<Stream> blob = null, bool isBlob = false, string name = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id of field should not be an empty Guid.", nameof(id));
            if (blob != null && !isBlob)
                throw new ArgumentException("You cannot provide a blob for a non-blob field.");

            this.Item = item;
            this.Id = id;
            this.Name = name;
            this.Value = value;
            this.Blob = blob;
            this.IsBlob = isBlob;
        }

        public override string ToString()
        {
            return $"{Name ?? Id.ToString()}={Value}";
        }
    }
}