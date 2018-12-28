PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Creating indexes...'

CREATE INDEX IX_BulkItemsAndFields ON #BulkItemsAndFields (ItemId, FieldId, Language, Version)
CREATE INDEX IX_BulkItemsAndFields_IsShared_IsUnversioned ON #BulkItemsAndFields (IsShared, IsUnversioned)

CREATE UNIQUE CLUSTERED INDEX IX_SyncItems_Id ON #SyncItems (Id)

CREATE UNIQUE CLUSTERED INDEX IX_FieldRules_FieldId ON #FieldRules (FieldId)