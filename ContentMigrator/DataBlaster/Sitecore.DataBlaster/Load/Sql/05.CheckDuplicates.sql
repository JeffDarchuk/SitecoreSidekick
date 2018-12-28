PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Finding duplicates...'

SELECT 
	si.Id, si.Name, si.ItemPath, si.SourceInfo
FROM 
	#SyncItems si
WHERE
	si.Id IN
	(
        -- Find items with duplicate fields.
		SELECT DISTINCT dbl.ItemId
		FROM #BulkItemsAndFields dbl
		GROUP BY dbl.ItemId, dbl.FieldId, dbl.Language, dbl.Version
		HAVING COUNT(*) > 1
	)

UNION ALL

SELECT 
	si.Id, si.Name, si.ItemPath, si.SourceInfo
FROM 
	#SyncItems si
WHERE
	si.ItemPath IN
	(
        -- Find items with duplicate paths.
		SELECT DISTINCT dbl.ItemPath
		FROM #SyncItems dbl
		GROUP BY dbl.ItemPath
		HAVING COUNT(*) > 1
	)

ORDER BY
    si.ItemPath