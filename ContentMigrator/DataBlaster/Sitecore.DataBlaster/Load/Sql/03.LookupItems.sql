PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Looking up items...'

DECLARE @destinationPath VARCHAR(900) = '/sitecore/content'
DECLARE @destinationId UNIQUEIDENTIFIER = '0DE95AE4-41AB-4D01-9EB0-67441B7C2450'
DECLARE @delim VARCHAR(10) = '|'

-- Build item paths of all items in destination.
;WITH LookupCTE (Id, Path, ParentId)
AS
(
	SELECT i.ID AS Id, @destinationPath AS Path, i.ParentID
	FROM Items i
	WHERE i.ID = @destinationId

	UNION ALL

	SELECT i.ID, CAST(cte.Path + '/' + i.Name AS VARCHAR(900)), i.ParentID
	FROM Items i
		JOIN LookupCTE cte ON
			cte.Id = i.ParentID
)

SELECT *
INTO #Paths
FROM LookupCTE

CREATE CLUSTERED INDEX IX_Paths_Path ON #Paths(Path)


-- Create temp table with all paths mapped to an id including parent paths and ids.
-- Id can either be the already existing id or the new id for the unexisting item.
SELECT *
INTO #PathIdMapping
FROM
	(
		SELECT 
			new.ItemId AS NewItemId,
			ISNULL(existing.Id, new.ItemId) AS ItemId,
			ISNULL(existing.Path, new.ItemPath) AS ItemPath,
			new.ParentId AS NewParentId,
			ISNULL(existingParent.Id, parent.Id) AS ParentId
		FROM
			(
				SELECT DISTINCT ItemId, ItemPath, ParentId
				FROM #BulkItemsAndFields
				WHERE ItemPath IS NOT NULL 
                    AND ItemPathExpression IS NULL
			) new
			LEFT JOIN #Paths existing ON
				existing.Path = new.ItemPath
			CROSS APPLY 
			(
				SELECT 
					CASE WHEN (existing.Id IS NOT NULL)
						THEN existing.ParentId
						ELSE NULL
					END AS Id,
					CASE WHEN (existing.Id IS NOT NULL)
						THEN LEFT(existing.Path, LEN(existing.Path) - CHARINDEX('/', REVERSE(existing.Path)))
						ELSE LEFT(new.ItemPath, LEN(new.ItemPath) - CHARINDEX('/', REVERSE(new.ItemPath)))
					END AS Path
			) parent
			LEFT JOIN #Paths existingParent ON
				existingParent.Path = parent.Path
	) x

CREATE CLUSTERED INDEX IX_PathIdMapping_Id ON #PathIdMapping (NewItemId)


-- Don't mis out on parents that are refering to new items that don't exist in db yet.
UPDATE map
SET map.ParentId = map2.ItemId
FROM #PathIdMapping map
	JOIN #PathIdMapping map2 ON
		map2.ItemPath = LEFT(map.ItemPath, LEN(map.ItemPath) - CHARINDEX('/', REVERSE(map.ItemPath)))
WHERE map.ParentId IS NULL


-- Update item ids, paths and parents.
UPDATE bif
SET bif.ItemId = map.ItemId,
	bif.ItemPath = map.ItemPath,
	bif.ParentId = map.ParentId
FROM #BulkItemsAndFields bif
	JOIN #PathIdMapping map ON
		map.NewItemId = bif.ItemId
WHERE bif.ItemId != map.ItemId
	OR bif.ItemPath != map.ItemPath
	OR bif.ParentId != map.parentId


-- Update fields that contain item ids to newly imported items.
-- See e.g. LookupNameColumnMapping
WHILE (1 = 1) -- We can only update a row 1 time in a single update statement.
BEGIN
    UPDATE 
        bif
    SET 
        bif.Value = REPLACE(bif.Value, ref.NewItemStrId, ref.ItemStrId)
	FROM
		#BulkItemsAndFields bif
		CROSS APPLY
		(
			SELECT TOP 1 
				'{' + CONVERT(NVARCHAR(36), map.NewItemId) + '}' AS NewItemStrId,
				'{' + CONVERT(NVARCHAR(36), map.ItemId) + '}' AS ItemStrId
			FROM
				(
					SELECT LTRIM(RTRIM(SUBSTRING(bif.Value, number,
						CHARINDEX(@delim, bif.Value + @delim, number) - number))) AS Value
					FROM
						(SELECT ROW_NUMBER() OVER (ORDER BY name) AS number FROM sys.all_objects) nr
					WHERE
						number <= LEN(bif.Value)
						AND SUBSTRING(@delim + bif.Value, number, LEN(@delim)) = @delim
				) split
				CROSS APPLY
				(
					SELECT TRY_CAST(split.Value AS UNIQUEIDENTIFIER) refId
				) ref
				LEFT JOIN Items i ON
					i.ID = ref.refId 
				LEFT JOIN #PathIdMapping map ON
					i.ID IS NULL -- Only join when item doesn't exist.
					AND map.NewItemId = ref.refId
            WHERE
                map.ItemId IS NOT NULL
		) ref
	WHERE
		LEFT(LTRIM(bif.Value), 1) = '{' -- Perf optimization.
		AND bif.Value != REPLACE(bif.Value, ref.NewItemStrId, ref.ItemStrId)

    IF (@@ROWCOUNT = 0)
		BREAK
END