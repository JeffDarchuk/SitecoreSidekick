PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Splitting temp table...'

SELECT DISTINCT 
    ItemId AS Id,
    ItemName AS Name,
    TemplateId,
    TemplateName,
    MasterId,
    ParentId,
    ItemPath,
    OriginalItemId AS OriginalId,
    ItemAction AS Action,
    SourceInfo,
    CAST(NULL AS BIT) AS HasParent,
    CAST(NULL AS BIT) AS HasTemplate
INTO 
    #SyncItems
FROM
    #BulkItemsAndFields

-- We don't create a seperate table for the fields anymore, 
-- because that would basically just be copying data.