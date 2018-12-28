CREATE TABLE #ItemLinks
(
	SourceDatabase NVARCHAR(150) COLLATE database_default NOT NULL,
    SourceItemId UNIQUEIDENTIFIER NOT NULL,
    SourceLanguage NVARCHAR(50) COLLATE database_default,
    SourceVersion INT,
    SourceFieldId UNIQUEIDENTIFIER NOT NULL,
    
    TargetDatabase NVARCHAR(150) COLLATE database_default NOT NULL,
    TargetItemId UNIQUEIDENTIFIER NOT NULL,
    TargetLanguage NVARCHAR(50) COLLATE database_default,
    TargetVersion INT,
    TargetPath NTEXT COLLATE database_default NOT NULL,

    ItemAction VARCHAR(50) COLLATE database_default NOT NULL, -- 'AddOnly', 'Revert'.
)