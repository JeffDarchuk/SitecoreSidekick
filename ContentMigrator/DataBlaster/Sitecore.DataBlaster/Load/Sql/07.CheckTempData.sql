PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Checking temp data...'

DECLARE @SharedFieldId UNIQUEIDENTIFIER = '{BE351A73-FCB0-4213-93FA-C302D8AB4F51}'
DECLARE @UnversionedFieldId UNIQUEIDENTIFIER = '{39847666-389D-409B-95BD-F2016F11EED5}'


-- Check if all items have corresponding parents and templates.
-- TODO: check masters?
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Validating parents and templates...'
UPDATE 
	si
SET 
	HasParent = CASE WHEN pi.ID IS NULL AND spi.Id IS NULL THEN 0 ELSE 1 END,
	HasTemplate = CASE WHEN ti.ID IS NULL AND tpi.Id IS NULL AND tpi.Action != 'UpdateExistingItem' THEN 0 ELSE 1 END
FROM #SyncItems si
	LEFT JOIN Items pi ON pi.ID = si.ParentId
	LEFT JOIN #SyncItems spi ON spi.Id = si.ParentId
	LEFT JOIN Items ti ON ti.ID = si.TemplateId
	LEFT JOIN #SyncItems tpi ON tpi.Id = si.TemplateId
WHERE si.ParentID != '00000000-0000-0000-0000-000000000000'

SELECT * FROM #SyncItems WHERE HasParent = 0 OR HasTemplate = 0


-- Check if all fields have corresponding items and field items.
-- Check if fields are shared or unversioned.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Validating fields...'
UPDATE 
	sf
SET 
	HasItem = CASE WHEN i.ID IS NULL AND si.Id IS NULL THEN 0 ELSE 1 END,
	HasField = CASE 
        WHEN fi.ID IS NULL AND sfi.Id IS NULL THEN 0 
        WHEN sharunver.IsUnversioned = 1 AND sf.Language IS NULL THEN 0
        WHEN sharunver.IsShared = 0 AND sharunver.IsUnversioned = 0 AND (sf.Language IS NULL OR sf.Version IS NULL ) THEN 0
        ELSE 1 END,
    IsShared = sharunver.IsShared,
    IsUnversioned = sharunver.IsUnversioned
FROM 
	#BulkItemsAndFields sf
    LEFT JOIN Items i ON i.ID = sf.ItemId
	LEFT JOIN #SyncItems si ON si.Id = sf.ItemId
	LEFT JOIN Items fi ON fi.ID = sf.FieldId
	LEFT JOIN #SyncItems sfi ON sfi.Id = sf.FieldId

    -- Detect whether field is shared or unversioned from provided data or database.
    OUTER APPLY
    (
        SELECT TOP 1 CASE WHEN ft.Value = '1' THEN 1 ELSE 0 END AS IsShared
        FROM Fields ft
        WHERE ft.ItemId = fi.ID AND ft.FieldId = @SharedFieldId
    ) fiShared
    OUTER APPLY
    (
        SELECT TOP 1 CASE WHEN sft.Value = '1' THEN 1 ELSE 0 END AS IsShared
        FROM #BulkItemsAndFields sft 
        WHERE sft.ItemId = sfi.ID AND sft.FieldId = @SharedFieldId
    ) sfiShared
    OUTER APPLY
    (
        SELECT TOP 1 CASE WHEN ft.Value = '1' THEN 1 ELSE 0 END AS IsUnversioned
        FROM Fields ft
        WHERE ft.ItemId = fi.ID AND ft.FieldId = @UnversionedFieldId
    ) fiUnversioned
    OUTER APPLY
    (
        SELECT TOP 1 CASE WHEN sft.Value = '1' THEN 1 ELSE 0 END AS IsUnversioned
        FROM #BulkItemsAndFields sft 
        WHERE sft.ItemId = sfi.ID AND sft.FieldId = @UnversionedFieldId
    ) sfiUnversioned
    OUTER APPLY
    (
        SELECT 
            COALESCE(sfiShared.IsShared, fiShared.IsShared, 0) AS IsShared,
            CASE WHEN COALESCE(sfiShared.IsShared, fiShared.IsShared, 0) = 1 
		        THEN 0 
		        ELSE COALESCE(sfiUnversioned.IsUnversioned, fiUnversioned.IsUnversioned, 0) END AS IsUnversioned
    ) sharunver


-- Remove old versions for unversioned fields.
-- Sitecore's sync item doesn't really have the notion of unversioned, so we get duplicates.
DELETE d
FROM #BulkItemsAndFields d
      JOIN (
            SELECT sf.ItemId, sf.FieldId, sf.Language, MAX(sf.Version) AS MaxVersion
            FROM #BulkItemsAndFields sf
            WHERE sf.IsUnversioned = 1
            GROUP BY sf.ItemId, sf.FieldId, sf.Language
            HAVING COUNT(sf.Version) > 1
      ) sf ON sf.ItemId = d.ItemId
      AND sf.FieldId = d.FieldId
      AND sf.Language = d.Language
      AND sf.MaxVersion > d.Version


SELECT sf.*, si.ItemPath, si.SourceInfo
FROM #BulkItemsAndFields sf 
    JOIN #SyncItems si ON 
        si.Id = sf.ItemId
WHERE sf.HasItem = 0 OR sf.HasField = 0