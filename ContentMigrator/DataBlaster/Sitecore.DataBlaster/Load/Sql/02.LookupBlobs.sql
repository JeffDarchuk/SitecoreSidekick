-- Lookup missing blob ids.
PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Looking up blobs...'
UPDATE 
	bif
SET 
	-- Value should contain BlobId in case of blobs.
	Value = CONVERT(VARCHAR(50), blobId.Value)
FROM 
	#BulkItemsAndFields bif
    
	-- Find current BlobId for this field.
	OUTER APPLY
	(
        SELECT TOP 1 
            b.BlobId
        FROM
            (
                SELECT f.Value 
                FROM SharedFields f
                WHERE f.ItemId = bif.ItemId
                    AND f.FieldId = bif.FieldId
                    AND f.Value IS NOT NULL AND f.Value != '' -- We can not convert this to GUID.

                UNION ALL

                -- Blobs cannot be unversioned, it's either shared or versioned.
                --SELECT f.Value 
                --FROM UnversionedFields f
                --WHERE f.ItemId = bif.ItemId
                --    AND f.FieldId = bif.FieldId
                --    AND f.Language = bif.Language
                --    AND f.Value IS NOT NULL AND f.Value != '' -- We can not convert this to GUID.

                --UNION ALL

                SELECT f.Value 
                FROM VersionedFields f
                WHERE f.ItemId = bif.ItemId
                    AND f.FieldId = bif.FieldId
                    AND f.Language = bif.Language
                    AND f.Version = bif.Version
                    AND f.Value IS NOT NULL AND f.Value != '' -- We can not convert this to GUID.
            ) f
            JOIN Blobs b ON 
                b.BlobId = TRY_CAST(f.Value AS UNIQUEIDENTIFIER)
	) b

    -- Generate new id for new blobs, will be used later on to add new blob.
    CROSS APPLY
    (
        SELECT ISNULL(b.BlobId, NEWID()) Value
    ) blobId
WHERE
    bif.isBlob = 1
    AND bif.Value IS NULL
