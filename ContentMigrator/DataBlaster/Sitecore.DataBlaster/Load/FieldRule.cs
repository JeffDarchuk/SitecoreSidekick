using System;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
    public class FieldRule
    {
        public Guid ItemId { get; }
        public bool SkipOnCreate { get; set; }
        public bool SkipOnUpdate { get; set; }
        public bool SkipOnDelete { get; set; }

        public FieldRule(Guid itemId, bool skipOnCreate = false, bool skipOnUpdate = false, bool skipOnDelete = false)
        {
            ItemId = itemId;
            SkipOnCreate = skipOnCreate;
            SkipOnUpdate = skipOnUpdate;
            SkipOnDelete = skipOnDelete;
        }
    }
}