using System;
using System.IO;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster
{
    /// <summary>
    /// Field which has specific values per language and version.
    /// </summary>
    public class VersionedBulkField : UnversionedBulkField
    {
        public int Version { get; private set; }

        internal VersionedBulkField(BulkItem item, Guid id, string language, int version, string value,
            Func<Stream> blob = null, bool isBlob = false, string name = null) 
            : base(item, id, language, value, blob, isBlob, name)
        {
            if (version <= 0) throw new ArgumentException("Version should be greater than 0.", "version");

            Version = version;
        }
    }
}