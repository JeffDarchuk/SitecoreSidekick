CREATE TABLE #BulkItemsAndFields
(
	ItemId UNIQUEIDENTIFIER NOT NULL,
	ItemName NVARCHAR(256) COLLATE database_default NOT NULL,
	TemplateId UNIQUEIDENTIFIER NOT NULL,
	TemplateName NVARCHAR(256) COLLATE database_default,
	MasterId UNIQUEIDENTIFIER NOT NULL,
	ParentId UNIQUEIDENTIFIER NOT NULL,
	ItemPath NVARCHAR(MAX) COLLATE database_default,
    ItemPathExpression NVARCHAR(MAX) COLLATE database_default,
    WhenItemIdCreated UNIQUEIDENTIFIER, -- Only creates this item when the referenced item is created as well.
    OriginalItemId UNIQUEIDENTIFIER NOT NULL, -- When LookupItems updates IDs, we still have a reference to the original id in code.

	ItemAction VARCHAR(50) COLLATE database_default NOT NULL, -- 'AddOnly', 'AddItemOnly', 'Update', 'UpdateExistingItem', 'Revert', 'RevertTree'.
    SourceInfo SQL_VARIANT,

    FieldId UNIQUEIDENTIFIER NOT NULL,
	FieldName NVARCHAR(256) COLLATE database_default,
	Language NVARCHAR(50) COLLATE database_default, -- NULL for shared fields
	Version INT, -- NULL for unversioned fields
	
    -- Value for the field, contains the blob id (GUID) in case of a blob, leave empty to lookup blob id.
    Value NVARCHAR(MAX) COLLATE database_default, 

    -- In case of same blob for different fields, 
    -- we only store the blob once, check the blob id (value) to find blob data in other record.
    Blob VARBINARY(MAX),
    IsBlob BIT NOT NULL,

    FieldAction VARCHAR(50) COLLATE database_default NOT NULL, -- 'AddOnly, 'Update'
    
    -- If fields only needs to be set when item is created/saved.
    WhenCreated BIT NOT NULL,
    WhenSaved BIT NOT NULL,

    DeduplicateItem BIT NOT NULL,

    IsShared BIT,
    IsUnversioned BIT,

    -- Flags will be set later on and will be used for validation.
    HasItem BIT,
    HasField BIT,
    
    RowId INT NOT NULL IDENTITY (1, 1)
)

CREATE TABLE #FieldRules
(
	FieldId UNIQUEIDENTIFIER NOT NULL,
	SkipOnCreate BIT NOT NULL,
	SkipOnUpdate BIT NOT NULL,
	SkipOnDelete BIT NOT NULL
)