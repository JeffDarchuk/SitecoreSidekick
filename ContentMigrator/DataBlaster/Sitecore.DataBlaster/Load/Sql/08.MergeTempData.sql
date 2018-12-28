-- Keep track of actions performed on items, so that we can update history engine and clear caches accordingly.
CREATE TABLE #ItemActions
(
    ItemId UNIQUEIDENTIFIER,
	OldParentId UNIQUEIDENTIFIER,
	ParentId UNIQUEIDENTIFIER,
	TemplateId UNIQUEIDENTIFIER,
    Created BIT,
    Saved BIT,
    Moved BIT,
    Deleted BIT,
    Language VARCHAR(50) COLLATE database_default,
    Version INT, 
    Timestamp DATETIME
)

-- Can be patched by code, see BulkLoader.cs
DECLARE @ProcessDependingFields BIT = 1
DECLARE @CleanupBlobs BIT = 0
DECLARE @AllowTemplateChanges BIT = 0
DECLARE @DefaultLanguage VARCHAR(50) = 'en'

DECLARE @Timestamp DATETIME = GETUTCDATE()

DECLARE @SharedFieldId UNIQUEIDENTIFIER = '{BE351A73-FCB0-4213-93FA-C302D8AB4F51}'
DECLARE @UnversionedFieldId UNIQUEIDENTIFIER = '{39847666-389D-409B-95BD-F2016F11EED5}'

DECLARE @SharedBlobFieldId UNIQUEIDENTIFIER = '{FF8A2D01-8A77-4F1B-A966-65806993CD31}'
DECLARE @UnversionedBlobFieldId UNIQUEIDENTIFIER = '{40E50ED9-BA07-4702-992E-A912738D32DC}'
DECLARE @VersionedBlobFieldId UNIQUEIDENTIFIER = '{DBBE7D99-1388-4357-BB34-AD71EDF18ED3}'


-- Upsert items.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting items...'
DECLARE @ItemChanges TABLE
(
    Action VARCHAR(10),
    ItemId UNIQUEIDENTIFIER,
    OldParentId UNIQUEIDENTIFIER,
    NewParentId UNIQUEIDENTIFIER,
	OldName NVARCHAR(256)
)

-- Handle AddItemOnly as AddOnly for items that don't exist, so that all fields will be added when necessary.
DELETE si FROM #SyncItems si JOIN Items i ON i.ID = si.Id WHERE si.Action = 'AddItemOnly'
UPDATE #SyncItems SET Action = 'AddOnly' WHERE Action = 'AddItemOnly'
DELETE bif FROM #BulkItemsAndFields bif JOIN Items i ON i.ID = bif.ItemId WHERE bif.FieldAction = 'AddItemOnly'
UPDATE #BulkItemsAndFields SET ItemAction = 'AddOnly' WHERE FieldAction = 'AddItemOnly'

MERGE 
	Items AS target
USING 
    #SyncItems AS source
ON
	target.ID = source.Id
WHEN MATCHED AND (source.Action = 'Update' OR source.Action = 'UpdateExistingItem' OR source.Action = 'Revert' OR source.Action = 'RevertTree') AND
	(   -- Make sure we only update records when really necessary.
		target.Name COLLATE SQL_Latin1_General_CP1_CS_AS != source.Name COLLATE SQL_Latin1_General_CP1_CS_AS
		OR target.TemplateID != source.TemplateId
		OR target.MasterID != source.MasterId
		OR target.ParentID != source.ParentId
	) THEN
	UPDATE SET
		Name = ISNULL(source.Name, target.Name),
		TemplateID = ISNULL(source.TemplateId, target.TemplateID),
		MasterID = source.MasterId,
		ParentId = source.ParentId,
		Updated = @Timestamp
WHEN NOT MATCHED AND source.Action != 'UpdateExistingItem' THEN
	INSERT (ID, Name, TemplateID, MasterID, ParentID, Created, Updated)
	VALUES (source.Id, source.Name, source.TemplateId, source.MasterId, source.ParentId, @Timestamp, @Timestamp)
OUTPUT
    $action, INSERTED.ID, DELETED.ParentID, INSERTED.ParentID, DELETED.NAME
INTO
    @ItemChanges
;

-- Remove items from temp tables which should be ignored.
DELETE sf
FROM #BulkItemsAndFields sf
    JOIN #SyncItems si ON si.Id = sf.ItemId
    LEFT JOIN Items i ON i.ID = si.Id
WHERE si.Action = 'UpdateExistingItem'
    AND i.ID IS NULL

DELETE si
FROM #SyncItems si
    LEFT JOIN Items i ON i.ID = si.Id
WHERE si.Action = 'UpdateExistingItem'
    AND i.ID IS NULL


-- Detect created items.
INSERT INTO #ItemActions (ItemId, ParentId, TemplateId, Created, Language, Version, Timestamp)
SELECT ic.ItemId, i.ParentId, i.TemplateId, 1, sf.Language, sf.Version, @Timestamp
FROM @ItemChanges ic 
    JOIN Items i ON i.ID = ic.ItemId
    LEFT JOIN #BulkItemsAndFields sf ON sf.ItemId = ic.ItemId
WHERE i.Created >= @timestamp
GROUP BY ic.ItemId, i.ParentId, i.TemplateId, sf.Language, sf.Version

-- Detect moved items.
INSERT INTO #ItemActions (ItemId, OldParentId, ParentId, TemplateId, Moved, Language, Version, Timestamp)
SELECT ic.ItemId, ic.OldParentId, ic.NewParentId, sf.TemplateId, 1, sf.Language, sf.Version, @Timestamp
FROM @ItemChanges ic
    LEFT JOIN #BulkItemsAndFields sf ON sf.ItemId = ic.ItemId
WHERE ic.OldParentId != ic.NewParentId
GROUP BY ic.ItemId, ic.OldParentId, ic.NewParentId, sf.TemplateId, sf.Language, sf.Version

-- Detect renamed items.
INSERT INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
SELECT ic.ItemId, i.ParentId, i.TemplateId, 1, sf.Language, sf.Version, @Timestamp
FROM @ItemChanges ic
	JOIN Items i ON i.ID = ic.ItemId
    LEFT JOIN #BulkItemsAndFields sf ON sf.ItemId = ic.ItemId
WHERE ic.OldName COLLATE SQL_Latin1_General_CP1_CS_AS != i.Name COLLATE SQL_Latin1_General_CP1_CS_AS
GROUP BY ic.ItemId, i.ParentId, i.TemplateId, sf.Language, sf.Version


-- Delete descendants of moved items.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting descendants of moved items...';
DELETE
    d
FROM
    Descendants d
    JOIN @ItemChanges ic ON
        ic.ItemId = d.Descendant
WHERE
    ic.Action = 'UPDATE'
    AND ic.OldParentId != ic.NewParentId

DELETE
    d
FROM
    Descendants d
    JOIN @ItemChanges ic ON
        ic.ItemId = d.Ancestor
WHERE
    ic.Action = 'UPDATE'
    AND ic.OldParentId != ic.NewParentId


-- Add missing descendants.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Adding missing descendants...';
WITH DescendantsCTE (Ancestor, Descendant)
AS
(
	SELECT 
		si.ParentId AS Ancestor, si.Id AS Descendant
	FROM
		#SyncItems si

	UNION ALL

	-- Get all parents recursively.
	SELECT 
		i.ParentId AS Ancestor, cte.Descendant
	FROM 
		Items i
		JOIN DescendantsCTE cte ON 
			cte.Ancestor = i.ID
	WHERE 
		i.ID != '00000000-0000-0000-0000-000000000000'
)
INSERT INTO 
	Descendants (ID, Ancestor, Descendant)
SELECT 
	NEWID(), d.Ancestor, d.Descendant
FROM
(
	SELECT DISTINCT
		cte.Ancestor, cte.Descendant
	FROM 
		DescendantsCTE cte
		LEFT JOIN Descendants d ON 
			d.Ancestor = cte.Ancestor 
			AND d.Descendant = cte.Descendant
	WHERE
		d.ID IS NULL
) d
OPTION (MAXRECURSION 1000)


-- Upsert blobs.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting blobs...'
DECLARE @BlobChunkSize INT = 1029120 -- Hardcoded value copied from Sitecore SqlServerDataProvider.BlobChunkSize
;WITH BlobCte(BlobId, Idx, Chunk, Remainder) as (
	SELECT 
		CAST(bif.Value AS UNIQUEIDENTIFIER) AS BlobId,
		0, 
		CAST(LEFT(bif.Blob, @BlobChunkSize) AS image), 
		STUFF(bif.Blob, 1, @BlobChunkSize, '')
	FROM
		-- Same blobs can be used by different fields.
		-- Make sure we have distinct set of blobs.
		(
			SELECT DISTINCT bif.Value, bif.Blob
			FROM #BulkItemsAndFields bif
			WHERE bif.Blob IS NOT NULL
		) bif
		OUTER APPLY
		(
			SELECT TOP 1 sf.FieldAction
			FROM #BulkItemsAndFields sf
			WHERE sf.Value = bif.Value
				AND sf.IsBlob = 1
		) rev
	WHERE 
		DATALENGTH(bif.Blob) > 0
        AND rev.FieldAction != 'AddOnly'

	UNION ALL

	SELECT 
		-- Crazy datatype issue!
		CAST(COALESCE(BlobId, NULL) AS UNIQUEIDENTIFIER),
		Idx + 1, 
		CAST(left(remainder, @BlobChunkSize) AS image), 
		STUFF(remainder, 1, @BlobChunkSize, '')
	FROM 
		BlobCte
	WHERE 
		DATALENGTH(remainder) > 0
)
MERGE 
	Blobs AS target
USING 
(
	SELECT BlobId, Idx, Chunk
    FROM BlobCte
) AS source
ON
	target.BlobId = source.BlobId
    AND target.[Index] = source.Idx
WHEN MATCHED AND CAST(target.Data AS VARBINARY(MAX)) != CAST(source.Chunk AS VARBINARY(MAX)) THEN
	UPDATE SET
		Data = source.Chunk
WHEN NOT MATCHED BY TARGET THEN
	INSERT (Id, BlobId, [Index], Data, Created)
	VALUES (NEWID(), source.BlobId, source.Idx, source.Chunk, @Timestamp)
WHEN NOT MATCHED BY SOURCE AND target.BlobId IN (SELECT DISTINCT BlobId FROM BlobCte) THEN 
    DELETE
;


-- Upsert shared fields.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting shared fields...'
MERGE 
	SharedFields AS target
USING 
(
	SELECT bif.ItemId, bif.FieldId, bif.Value, bif.FieldAction, fr.SkipOnUpdate, fr.SkipOnCreate
	FROM #BulkItemsAndFields bif
		LEFT JOIN #FieldRules fr ON bif.FieldId = fr.FieldId
	WHERE bif.IsShared = 1 AND bif.WhenCreated = 0 AND bif.WhenSaved = 0
) AS source
ON
	target.ItemId = source.ItemId
	AND target.FieldId = source.FieldId
WHEN MATCHED AND source.FieldAction != 'AddOnly' AND ISNULL(source.SkipOnUpdate, 0) != 1 AND
	(   -- Make sure we only update records when really necessary.
		target.Value COLLATE SQL_Latin1_General_CP1_CS_AS != source.Value COLLATE SQL_Latin1_General_CP1_CS_AS
	) THEN
	UPDATE SET
		Value = source.Value,
		Updated = @Timestamp
WHEN NOT MATCHED BY TARGET AND ISNULL(source.SkipOnCreate, 0) != 1 THEN
	INSERT (Id, ItemId, FieldId, Value, Created, Updated)
	VALUES (NEWID(), source.ItemId, source.FieldId, source.Value, @Timestamp, @Timestamp)
;


-- Upsert unversioned fields.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting unversioned fields...'
MERGE 
	UnversionedFields AS target
USING 
(
	SELECT bif.ItemId, bif.FieldId, bif.Value, bif.Language, bif.FieldAction, fr.SkipOnUpdate, fr.SkipOnCreate
	FROM #BulkItemsAndFields bif
		LEFT JOIN #FieldRules fr ON bif.FieldId = fr.FieldId
	WHERE bif.IsShared = 0 AND bif.IsUnversioned = 1 AND bif.WhenCreated = 0 AND bif.WhenSaved = 0
) AS source
ON
	target.ItemId = source.ItemId
	AND target.FieldId = source.FieldId
	AND target.Language = source.Language
WHEN MATCHED AND source.FieldAction != 'AddOnly' AND ISNULL(source.SkipOnUpdate, 0) != 1 AND
	(   -- Make sure we only update records when really necessary.
		target.Value COLLATE SQL_Latin1_General_CP1_CS_AS != source.Value COLLATE SQL_Latin1_General_CP1_CS_AS
	) THEN
	UPDATE SET
		Value = source.Value,
		Updated = @Timestamp
WHEN NOT MATCHED AND ISNULL(source.SkipOnCreate, 0) != 1 THEN
	INSERT (Id, ItemId, Language, FieldId, Value, Created, Updated)
	VALUES (NEWID(), source.ItemId, source.Language, source.FieldId, source.Value, @Timestamp, @Timestamp)
;


-- Upsert versioned fields.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting versioned fields...'
MERGE 
	VersionedFields AS target
USING 
(
	SELECT bif.ItemId, bif.FieldId, bif.Value, bif.Language, bif.Version, bif.FieldAction, fr.SkipOnUpdate, fr.SkipOnCreate
	FROM #BulkItemsAndFields bif
		LEFT JOIN #FieldRules fr ON bif.FieldId = fr.FieldId
	WHERE bif.IsShared = 0 AND bif.IsUnversioned = 0 AND bif.WhenCreated = 0 AND bif.WhenSaved = 0
) AS source
ON
	target.ItemId = source.ItemId
	AND target.FieldId = source.FieldId
	AND target.Language = source.Language
	AND target.Version = source.Version
WHEN MATCHED AND source.FieldAction != 'AddOnly' AND ISNULL(source.SkipOnUpdate, 0) != 1 AND
	(   -- Make sure we only update records when really necessary.
		target.Value COLLATE SQL_Latin1_General_CP1_CS_AS != source.Value COLLATE SQL_Latin1_General_CP1_CS_AS
	) THEN
	UPDATE SET
		Value = source.Value,
		Updated = @Timestamp
WHEN NOT MATCHED AND ISNULL(source.SkipOnCreate, 0) != 1 THEN
	INSERT (Id, ItemId, Language, Version, FieldId, Value, Created, Updated)
	VALUES (NEWID(), source.ItemId, source.Language, source.Version, source.FieldId, source.Value, @Timestamp, @Timestamp)
;


-- Detect item save by changed fields.
INSERT INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
SELECT si.Id, sf.ParentId, sf.TemplateId, 1, sf.Language, sf.Version, @Timestamp
FROM #SyncItems si
    JOIN Fields f ON f.ItemId = si.Id
    JOIN #BulkItemsAndFields sf ON sf.ItemId = si.Id AND sf.FieldId = f.FieldId
WHERE f.Updated >= @Timestamp
GROUP BY si.Id, sf.ParentId, sf.TemplateId, sf.Language, sf.Version


IF @ProcessDependingFields = 1
BEGIN
    -- Upsert shared fields that depend on item create/save.
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting depending shared fields...'
    MERGE 
	    SharedFields AS target
    USING 
    (
        SELECT sf.ItemId, sf.FieldId, sf.Value, sf.FieldAction, fr.SkipOnUpdate, fr.SkipOnCreate
        FROM #BulkItemsAndFields sf 
		    CROSS APPLY
		    (
			    SELECT TOP 1 ia.ItemId
			    FROM #ItemActions ia
			    WHERE ia.ItemId = sf.ItemId 
				    AND ia.Language IS NULL
				    AND ia.Version IS NULL
				    AND ((ia.Created = 1 AND sf.WhenCreated = 1) OR (ia.Saved = 1 AND sf.WhenSaved = 1))
		    ) ia
			LEFT JOIN #FieldRules fr ON sf.FieldId = fr.FieldId
        WHERE sf.IsShared = 1
            AND (sf.WhenCreated = 1 OR sf.WhenSaved = 1)
    ) AS source
    ON
	    target.ItemId = source.ItemId
	    AND target.FieldId = source.FieldId
    WHEN MATCHED AND source.FieldAction != 'AddOnly' AND ISNULL(source.SkipOnUpdate, 0) != 1 AND
	    (   -- Make sure we only update records when really necessary.
		    target.Value COLLATE SQL_Latin1_General_CP1_CS_AS != source.Value COLLATE SQL_Latin1_General_CP1_CS_AS
	    ) THEN
	    UPDATE SET
		    Value = source.Value,
		    Updated = @Timestamp
    WHEN NOT MATCHED BY TARGET AND ISNULL(source.SkipOnCreate, 0) != 1 THEN
	    INSERT (Id, ItemId, FieldId, Value, Created, Updated)
	    VALUES (NEWID(), source.ItemId, source.FieldId, source.Value, @Timestamp, @Timestamp)
    ;

    -- Upsert unversioned fields that depend on item create/save.
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting depending unversioned fields...'
    MERGE 
	    UnversionedFields AS target
    USING 
    (
        SELECT sf.ItemId, sf.FieldId, sf.Value, sf.Language, sf.FieldAction, fr.SkipOnUpdate, fr.SkipOnCreate
        FROM #BulkItemsAndFields sf 
		    CROSS APPLY
		    (
			    SELECT TOP 1 ia.ItemId
			    FROM #ItemActions ia
			    WHERE ia.ItemId = sf.ItemId 
				    AND (ia.Language = sf.Language OR ia.Language IS NULL)
				    AND ia.Version IS NULL
				    AND ((ia.Created = 1 AND sf.WhenCreated = 1) OR (ia.Saved = 1 AND sf.WhenSaved = 1))
		    ) ia
			LEFT JOIN #FieldRules fr ON sf.FieldId = fr.FieldId
        WHERE sf.IsShared = 0 
		    AND sf.IsUnversioned = 1 
            AND (sf.WhenCreated = 1 OR sf.WhenSaved = 1)
    ) AS source
    ON
	    target.ItemId = source.ItemId
	    AND target.FieldId = source.FieldId
	    AND target.Language = source.Language
    WHEN MATCHED AND source.FieldAction != 'AddOnly' AND ISNULL(source.SkipOnUpdate, 0) != 1 AND
	    (   -- Make sure we only update records when really necessary.
		    target.Value COLLATE SQL_Latin1_General_CP1_CS_AS != source.Value COLLATE SQL_Latin1_General_CP1_CS_AS
	    ) THEN
	    UPDATE SET
		    Value = source.Value,
		    Updated = @Timestamp
    WHEN NOT MATCHED AND ISNULL(source.SkipOnCreate, 0) != 1 THEN
	    INSERT (Id, ItemId, Language, FieldId, Value, Created, Updated)
	    VALUES (NEWID(), source.ItemId, source.Language, source.FieldId, source.Value, @Timestamp, @Timestamp)
    ;

    -- Upsert versioned fields that depend on item create/save.
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Upserting depending versioned fields...'
    MERGE 
	    VersionedFields AS target
    USING 
    (
	    SELECT sf.ItemId, sf.FieldId, sf.Value, sf.Language, sf.Version, sf.FieldAction, fr.SkipOnUpdate, fr.SkipOnCreate
        FROM #BulkItemsAndFields sf 
		    CROSS APPLY
		    (
			    SELECT TOP 1 ia.ItemId
			    FROM #ItemActions ia
			    WHERE ia.ItemId = sf.ItemId 
				    AND (ia.Language = sf.Language OR ia.Language IS NULL)
				    AND (ia.Version = sf.Version OR ia.Version IS NULL)
				    AND ((ia.Created = 1 AND sf.WhenCreated = 1) OR (ia.Saved = 1 AND sf.WhenSaved = 1))
		    ) ia
			LEFT JOIN #FieldRules fr ON sf.FieldId = fr.FieldId
        WHERE sf.IsShared = 0 
		    AND sf.IsUnversioned = 0 
            AND (sf.WhenCreated = 1 OR sf.WhenSaved = 1)
    ) AS source
    ON
	    target.ItemId = source.ItemId
	    AND target.FieldId = source.FieldId
	    AND target.Language = source.Language
	    AND target.Version = source.Version
    WHEN MATCHED AND source.FieldAction != 'AddOnly' AND ISNULL(source.SkipOnUpdate, 0) != 1 AND
	    (   -- Make sure we only update records when really necessary.
		    target.Value COLLATE SQL_Latin1_General_CP1_CS_AS != source.Value COLLATE SQL_Latin1_General_CP1_CS_AS
	    ) THEN
	    UPDATE SET
		    Value = source.Value,
		    Updated = @Timestamp
    WHEN NOT MATCHED AND ISNULL(source.SkipOnCreate, 0) != 1 THEN
	    INSERT (Id, ItemId, Language, Version, FieldId, Value, Created, Updated)
	    VALUES (NEWID(), source.ItemId, source.Language, source.Version, source.FieldId, source.Value, @Timestamp, @Timestamp)
    ;
END


-- Delete blobs that are not referenced anymore.
IF @CleanupBlobs = 1
BEGIN
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting blobs...'
    DELETE
	    b
    FROM 
	    Blobs b
	    LEFT JOIN Fields f ON
		    f.ItemId NOT IN (@SharedBlobFieldId, @UnversionedBlobFieldId, @VersionedBlobFieldId)
		    AND f.FieldId IN (@SharedBlobFieldId, @UnversionedBlobFieldId, @VersionedBlobFieldId)
		    AND f.Value IS NOT NULL 
		    AND f.Value != ''
		    AND TRY_CAST(f.Value AS UNIQUEIDENTIFIER) = b.BlobId
    WHERE
	    f.FieldId IS NULL
END


-- Delete shared fields when item needs to be reverted and field is not included in temp table and isn't configured to be kept.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting shared fields...'
DELETE
	f
OUTPUT 
    si.Id, si.ParentId, si.TemplateId, 1, NULL, NULL, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
FROM
	SharedFields f
	JOIN #SyncItems si ON
		si.Id = f.ItemId
	LEFT JOIN #BulkItemsAndFields sf ON
		sf.ItemId = f.ItemId
		AND sf.FieldId = f.FieldId
	LEFT JOIN #FieldRules fr ON
		fr.FieldId = f.FieldId
WHERE
	(si.Action = 'Revert' OR si.Action = 'RevertTree')
	AND sf.FieldId IS NULL
	AND ISNULL(fr.SkipOnDelete, 0) != 1
    

-- Delete unversioned fields when item needs to be reverted and field is not included in temp table and isn't configured to be kept.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting unversioned fields...'
DELETE
	f
OUTPUT 
    si.Id, si.ParentId, si.TemplateId, 1, DELETED.Language, NULL, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
FROM
	UnversionedFields f
	JOIN #SyncItems si ON
		si.Id = f.ItemId
	LEFT JOIN #BulkItemsAndFields sf ON
		sf.ItemId = f.ItemId
		AND sf.FieldId = f.FieldId
		AND sf.Language = f.Language
	LEFT JOIN #FieldRules fr ON
		fr.FieldId = f.FieldId
WHERE
	(si.Action = 'Revert' OR si.Action = 'RevertTree')
	AND sf.FieldId IS NULL
	AND ISNULL(fr.SkipOnDelete, 0) != 1


-- Delete versioned fields when item needs to be reverted and field is not included in temp table and isn't configured to be kept.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting versioned fields...'
DELETE
	f
OUTPUT 
    si.Id, si.ParentId, si.TemplateId, 1, DELETED.Language, DELETED.Version, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
FROM
	VersionedFields f
	JOIN #SyncItems si ON
		si.Id = f.ItemId
	LEFT JOIN #BulkItemsAndFields sf ON
		sf.ItemId = f.ItemId
		AND sf.FieldId = f.FieldId
		AND sf.Language = f.Language
		AND sf.Version = f.Version
	LEFT JOIN #FieldRules fr ON
		fr.FieldId = f.FieldId
WHERE
	(si.Action = 'Revert' OR si.Action = 'RevertTree')
	AND sf.FieldId IS NULL
	AND ISNULL(fr.SkipOnDelete, 0) != 1


-- Delete all items that are not included in the temp table and which have an ancestor that reverts the entire tree.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting redundant items...'
SELECT
	toDelete.ID AS Id, toDelete.ParentId, toDelete.TemplateId
INTO
	#ItemsToDelete
FROM
	#SyncItems source
	JOIN Descendants d ON 
		d.Ancestor = source.Id
	JOIN Items toDelete ON
		toDelete.ID = d.Descendant

	-- To delete should not exist in source.
	LEFT JOIN #SyncItems source2 ON
		source2.Id = toDelete.ID
WHERE
	source.Action = 'RevertTree'
	AND source2.Id IS NULL
GROUP BY toDelete.Id, toDelete.ParentId, toDelete.TemplateId

INSERT INTO #ItemActions (ItemId, ParentId, TemplateId, Deleted, Language, Version, Timestamp) 
SELECT *
FROM
    (
        -- Add change for entire item and for each language version.
        SELECT itd.Id, itd.ParentId, itd.TemplateId, 1 AS Deleted, NULL AS Language, NULL AS Version, @Timestamp AS Timestamp
        FROM #ItemsToDelete itd
        UNION
        SELECT itd.Id, itd.ParentId, itd.TemplateId, 1, l.Language, v.Version, @Timestamp
        FROM #ItemsToDelete itd
            OUTER APPLY (SELECT DISTINCT f.Language FROM Fields f WHERE f.ItemId = itd.Id) l
            OUTER APPLY (SELECT DISTINCT vf.Version FROM VersionedFields vf WHERE vf.ItemId = itd.Id) v
    ) x

DELETE f FROM SharedFields f WHERE f.ItemId IN (SELECT Id FROM #ItemsToDelete)
DELETE f FROM UnversionedFields f WHERE f.ItemId IN (SELECT Id FROM #ItemsToDelete)
DELETE f FROM VersionedFields f WHERE f.ItemId IN (SELECT Id FROM #ItemsToDelete)
DELETE i FROM Items i WHERE i.Id IN (SELECT Id FROM #ItemsToDelete)
DELETE d FROM Descendants d WHERE d.Ancestor IN (SELECT Id FROM #ItemsToDelete)
DELETE d FROM Descendants d WHERE d.Descendant IN (SELECT Id FROM #ItemsToDelete)


-- Remove all items from archive that we just imported.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Cleaning up archive...'
DELETE av FROM ArchivedVersions av JOIN #SyncItems si ON si.Id = av.ItemId
DELETE ai FROM ArchivedItems ai JOIN #SyncItems si ON si.Id = ai.ItemId
DELETE af 
FROM ArchivedFields af
	JOIN Archive a ON 
		a.ArchivalId = af.ArchivalId
	JOIN #SyncItems si ON
		si.Id = a.ItemId
DELETE a FROM Archive a JOIN #SyncItems si ON si.Id = a.ItemId


IF @AllowTemplateChanges = 1
BEGIN
    -- Delete items without templates.
    DELETE i
    OUTPUT deleted.ID, deleted.ParentId, deleted.TemplateId, 1, NULL, NULL, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Deleted, Language, Version, Timestamp)
    FROM Items i
	    LEFT JOIN Items ti ON ti.ID = i.TemplateID
    WHERE ti.ID IS NULL


    -- Delete fields without template field items.
    DELETE f
    OUTPUT deleted.ItemId, i.ParentId, i.TemplateId, 1, NULL, NULL, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
    FROM SharedFields f
		JOIN Items i ON i.Id = f.ItemId
	    LEFT JOIN Items tfi ON tfi.ID = f.FieldId
    WHERE tfi.ID IS NULL

    DELETE f
    OUTPUT deleted.ItemId, i.ParentId, i.TemplateId, 1, deleted.Language, NULL, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
    FROM UnversionedFields f
		JOIN Items i ON i.Id = f.ItemId
	    LEFT JOIN Items tfi ON tfi.ID = f.FieldId
    WHERE tfi.ID IS NULL

    DELETE f
    OUTPUT deleted.ItemId, i.ParentId, i.TemplateId, 1, deleted.Language, deleted.Version, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
    FROM VersionedFields f
		JOIN Items i ON i.Id = f.ItemId
	    LEFT JOIN Items tfi ON tfi.ID = f.FieldId
    WHERE tfi.ID IS NULL


    -- Delete shared fields that are not shared anymore.
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting shared fields that are not shared anymore...'
    DELETE
        f
    OUTPUT deleted.ItemId, i.ParentId, i.TemplateId, 1, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Timestamp)
    FROM SharedFields f
		JOIN Items i ON i.Id = f.ItemId
	    -- Find 'Shared' field for template item of the field.
	    -- Shared field is itself shared too.
	    LEFT JOIN SharedFields sf ON
		    sf.ItemId = f.FieldId
		    AND sf.FieldId = @SharedFieldId
    WHERE sf.Id IS NULL OR sf.Value != '1'


    -- Delete unversioned fields that are not unversioned anymore.
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting unversioned fields that are not unversioned anymore...'
    DELETE
        f
    OUTPUT deleted.ItemId, i.ParentId, i.TemplateId, 1, deleted.Language, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Timestamp)
    FROM UnversionedFields f
		JOIN Items i ON i.Id = f.ItemId
	    -- Find 'Unversioned' field for template item of the field.
	    -- Unversioned field is itself shared.
	    LEFT JOIN SharedFields uf ON
		    uf.ItemId = f.FieldId
		    AND uf.FieldId = @UnversionedFieldId
    WHERE uf.Id IS NULL OR uf.Value != '1'


    -- Delete versioned fields that are not versioned anymore.
    PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Deleting versioned fields that are not unversioned anymore...'
    DELETE
        f
    OUTPUT deleted.ItemId, i.ParentId, i.TemplateId, 1, deleted.Language, deleted.Version, @Timestamp 
		INTO #ItemActions (ItemId, ParentId, TemplateId, Saved, Language, Version, Timestamp)
    FROM VersionedFields f
		JOIN Items i ON i.Id = f.ItemId
	    -- Find 'Shared' field for template item of the field.
	    -- Shared field is itself shared too.
	    LEFT JOIN SharedFields sf ON
		    sf.ItemId = f.FieldId
		    AND sf.FieldId = @SharedFieldId
		
	    -- Find 'Unversioned' field for template item of the field.
	    -- Unversioned field is itself shared.
	    LEFT JOIN SharedFields uf ON
		    uf.ItemId = f.FieldId
		    AND uf.FieldId = @UnversionedFieldId
    WHERE sf.Value = '1' OR uf.Value = '1'
END


-- Look for versions that no longer exist after the revert.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Detecting language/versions that no longer exist.'
INSERT INTO #ItemActions (ItemId, ParentId, TemplateId, Deleted, Language, Version, Timestamp)
SELECT ia.ItemId, ia.ParentId, ia.TemplateId, 1, ia.Language, ia.Version, @Timestamp
FROM
	( 
		SELECT DISTINCT ItemId, ParentId, TemplateId, Language, Version 
		FROM #ItemActions
        WHERE Language IS NOT NULL
			AND Version IS NOT NULL
	) ia
	OUTER APPLY
	(
		SELECT TOP 1 vf.ItemId
		FROM VersionedFields vf
		WHERE vf.ItemId = ia.ItemId
			AND	vf.Language = ia.Language
			AND	vf.Version = ia.Version
	) vf
WHERE
	vf.ItemId IS NULL


-- Update item records when field(s) or version(s) were changed / deleted.
UPDATE i
SET i.Updated = @Timestamp
FROM (SELECT DISTINCT ItemId FROM #ItemActions) ia
    JOIN Items i ON i.ID = ia.ItemId
WHERE i.Updated != @Timestamp


-- Select changes.
SELECT si.ItemPath, ia.ItemId, ISNULL(si.OriginalId, ia.ItemId) AS OriginalItemId, 
	ia.TemplateId, ia.ParentId, ISNULL(ia.OldParentId, ia.ParentId) AS OriginalParentId,
    ia.Language AS Language, 
    MAX(ia.Version) AS Version,
    CAST(MAX(CAST(ia.Created AS TINYINT)) AS BIT) AS Created, 
	CAST(MAX(CAST(ia.Saved AS TINYINT)) AS BIT) AS Saved, 
	CAST(MAX(CAST(ia.Moved AS TINYINT)) AS BIT) AS Moved, 
	CAST(MAX(CAST(ia.Deleted AS TINYINT)) AS BIT) AS Deleted,
    si.SourceInfo
FROM #ItemActions ia
    LEFT JOIN #SyncItems si ON 
        si.Id = ia.ItemId
GROUP BY si.ItemPath, ia.ItemId, ISNULL(si.OriginalId, ia.ItemId), ia.TemplateId, ia.ParentId, ISNULL(ia.OldParentId, ia.ParentId), ia.Language, si.SourceInfo
ORDER BY si.SourceInfo, si.ItemPath