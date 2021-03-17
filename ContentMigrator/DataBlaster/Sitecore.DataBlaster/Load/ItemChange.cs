using System;
using System.Data;
using System.Linq;
using Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Read;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load
{
    public class ItemChange
    {
        private readonly ItemVersionHeader _itemVersionHeader;
        public string ItemPath { get; private set; }
        public int ItemPathLevel { get; private set; }
        public Guid ItemId { get; private set; }
        public Guid OriginalItemId { get; private set; }
        public Guid TemplateId { get; private set; }
        public Guid ParentId { get; private set; }
        public Guid OriginalParentId { get; private set; }
        public string Language { get; private set; }
        public int? Version { get; private set; }
        public bool Created { get; private set; }
        public bool Saved { get; private set; }
        public bool Moved { get; private set; }
        public bool Deleted { get; private set; }
        public object SourceInfo { get; private set; }

        public ItemChange(IDataRecord record)
        {
            ItemPath = record.IsDBNull(0) ? null : record.GetString(0);
            ItemPathLevel = ItemPath == null ? 0 : ItemPath.Count(c => c == '/');
            ItemId = record.GetGuid(1);
            OriginalItemId = record.IsDBNull(2) ? ItemId : record.GetGuid(2);
            TemplateId = record.GetGuid(3);
            ParentId = record.GetGuid(4);
            OriginalParentId = record.GetGuid(5);
            Language = record.IsDBNull(6) ? null : record.GetString(6);
            Version = record.IsDBNull(7) ? (int?)null : record.GetInt32(7);
            Created = record.IsDBNull(8) ? false : record.GetBoolean(8);
            Saved = record.IsDBNull(9) ? false : record.GetBoolean(9);
            Moved = record.IsDBNull(10) ? false : record.GetBoolean(10);
            Deleted = record.IsDBNull(11) ? false : record.GetBoolean(11);
            SourceInfo = record.IsDBNull(12) ? null : record.GetValue(12);
        }

        public ItemChange(ItemVersionHeader itemVersionHeader,
            bool created = false, bool saved = false, bool moved = false, bool deleted = false)
        {
            _itemVersionHeader = itemVersionHeader;
            ItemPath = itemVersionHeader.ItemPath;
            ItemPathLevel = itemVersionHeader.ItemPath.Count(c => c == '/');
            ItemId = itemVersionHeader.Id;
            OriginalItemId = itemVersionHeader.Id;
            TemplateId = itemVersionHeader.TemplateId;
            ParentId = itemVersionHeader.ParentId;
            OriginalParentId = itemVersionHeader.ParentId;
            Language = itemVersionHeader.Language;
            Version = itemVersionHeader.Version;
            Created = created;
            Saved = saved;
            Moved = moved;
            Deleted = deleted;
            SourceInfo = null;
        }
    }
}
