using System;
using System.IO;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster
{
    /// <summary>
    /// Field which has specific values per language but shares the values accross versions.
    /// </summary>
    public class UnversionedBulkField : BulkField
    {
        public string Language { get; private set; }

        internal UnversionedBulkField(BulkItem item, Guid id, string language, string value,
            Func<Stream> blob = null, bool isBlob = false, string name = null) 
            : base(item, id, value, blob, isBlob, name)
        {
            if (language == null) throw new ArgumentNullException("language");

            this.Language = language;
        }
    }
}