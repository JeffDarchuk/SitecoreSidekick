using System;
using System.IO;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster
{
    /// <summary>
    /// Field which has the same value for all languages and all versions.
    /// </summary>
    public class SharedBulkField : BulkField
    {
        internal SharedBulkField(BulkItem item, Guid id, string value,
            Func<Stream> blob = null, bool isBlob = false, string name = null) 
            : base(item, id, value, blob, isBlob, name)
        {
        }
    }
}