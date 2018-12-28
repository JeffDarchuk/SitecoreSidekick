using System;
using Sitecore.Data;
using Sitecore.Links;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Links
{
    public class BulkItemLink : ItemLink
    {
        public BulkLoadAction ItemAction { get; set; }

        public BulkItemLink(string sourceDatabase, ID sourceItemID, ID sourceFieldID, 
            string targetDatabase, ID targetItemID, string targetPath) 
            : base(sourceDatabase, sourceItemID, sourceFieldID, targetDatabase, targetItemID, targetPath)
        {
        }

        public BulkItemLink Map(Guid originalId, Guid newId)
        {
            if (SourceItemID.Guid == originalId)
                return new BulkItemLink(SourceDatabaseName, new ID(newId), SourceFieldID,
                    TargetDatabaseName, TargetItemID, TargetPath);

            if (TargetItemID.Guid == originalId)
                return new BulkItemLink(SourceDatabaseName, SourceItemID, SourceFieldID,
                    TargetDatabaseName, new ID(newId), TargetPath);

            return this;
        }
    }
}