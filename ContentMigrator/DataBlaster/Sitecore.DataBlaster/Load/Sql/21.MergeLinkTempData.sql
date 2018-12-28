PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Updating link database...'

MERGE 
	Links AS target
USING 
    #ItemLinks AS source
ON
	target.SourceDatabase = source.SourceDatabase
    AND target.SourceItemID = source.SourceItemId
    AND target.SourceLanguage = ISNULL(source.SourceLanguage, '')
    AND target.SourceVersion = ISNULL(source.SourceVersion, 0)
    AND target.SourceFieldID = source.SourceFieldId
    AND target.TargetItemID = source.TargetItemId
    AND target.TargetLanguage = ISNULL(source.TargetLanguage, '')
    AND target.TargetVersion = ISNULL(source.TargetVersion, 0)
WHEN MATCHED AND (source.ItemAction = 'Update' OR source.ItemAction = 'Revert') THEN
	UPDATE SET
		target.TargetPath = source.TargetPath
WHEN NOT MATCHED BY TARGET THEN
	INSERT (ID, 
        SourceDatabase, SourceItemID, SourceLanguage, SourceVersion, SourceFieldID,
        TargetDatabase, TargetItemID, TargetLanguage, TargetVersion, TargetPath)
	VALUES (NEWID(), 
        source.SourceDatabase, source.SourceItemId, ISNULL(source.SourceLanguage, ''), ISNULL(source.SourceVersion, 0), source.SourceFieldId,
        source.TargetDatabase, source.TargetItemId, ISNULL(source.TargetLanguage, ''), ISNULL(source.TargetVersion, 0), source.TargetPath)
;

PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting item links...'
DELETE
	l
FROM
    Links l
    CROSS APPLY
    (
        SELECT TOP 1 il.SourceItemId
        FROM #ItemLinks il
        WHERE (il.ItemAction = 'Revert' OR il.ItemAction = 'RevertTree')
            AND il.SourceDatabase = l.SourceDatabase
            AND il.SourceItemId = l.SourceItemID
    ) isItemToRevert
    OUTER APPLY
    (
        SELECT TOP 1 il.SourceItemId
        FROM #ItemLinks il
        WHERE il.SourceDatabase = l.SourceDatabase
            AND il.SourceItemId = l.SourceItemID
            AND ISNULL(il.SourceLanguage, '') = l.SourceLanguage
            AND ISNULL(il.SourceVersion, 0) = l.SourceVersion
            AND il.SourceFieldId = l.SourceFieldID
    ) source
WHERE
    source.SourceItemId IS NULL