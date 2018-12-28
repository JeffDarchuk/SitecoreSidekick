DECLARE @rootItemId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111'
DECLARE @rootItemPath VARCHAR(MAX) = '/sitecore'

DECLARE @templateId UNIQUEIDENTIFIER = '455A3E98-A627-4B40-8035-E683A0331AC7' -- Template fields
DECLARE @modifiedSince DATETIME = '2010-01-01'
DECLARE @neverPublishFieldId UNIQUEIDENTIFIER = '9135200A-5626-4DD8-AB9D-D665B8C11748'
DECLARE @neverPublish NVARCHAR(MAX) = '1'

-- BEGIN: Lines before this line will be ignored when reading this embedded sql.

DECLARE @NonPublishableItems TABLE (Id UNIQUEIDENTIFIER)
INSERT INTO @NonPublishableItems (Id)
SELECT DISTINCT np.ItemId
FROM Descendants d
	JOIN SharedFields np ON np.ItemId = d.Descendant
WHERE d.Ancestor = @rootItemId
	AND np.FieldId = @neverPublishFieldId
	AND np.Value = '1'

;WITH LookupCTE (Id, Path, ParentId)
AS
(
	SELECT i.ID AS Id, CAST(@rootItemPath AS VARCHAR(MAX)) AS Path, i.ParentID AS ParentId	
	FROM Items i
	WHERE i.ID = @rootItemId

	UNION ALL

	SELECT i.ID, CAST(cte.Path + '/' + i.Name AS VARCHAR(MAX)), i.ParentID
	FROM Items i
		JOIN LookupCTE cte ON
			cte.Id = i.ParentID
	WHERE @neverPublish = '1' AND i.ID NOT IN (SELECT Id FROM @NonPublishableItems)
)

SELECT
	cte.Id AS ItemId, 
	i.Name AS ItemName, 
	cte.Path AS ItemPath, 
	i.TemplateID AS TemplateId,
	i.MasterId AS MasterId,
	cte.ParentId,
	i.Created AS ItemCreated,
	i.Updated AS ItemUpdated,
	f.FieldId, 
	f.Value AS FieldValue,
	f.Language,
	f.Version
FROM 
	LookupCTE cte
	JOIN Items i ON 
		i.ID = cte.Id
	CROSS APPLY
	(
		SELECT sf.FieldId, sf.Value AS Value, NULL AS Language, NULL AS Version
		FROM dbo.SharedFields sf
		WHERE sf.ItemId = i.ID
			AND sf.Updated >= @modifiedSince

		UNION ALL

		SELECT uf.FieldId, uf.Value AS Value, uf.Language AS Language, NULL AS Version
		FROM dbo.UnversionedFields uf
		WHERE uf.ItemId = i.ID
			AND uf.Updated >= @modifiedSince

		UNION ALL

		SELECT vf.FieldId, vf.Value AS Value, vf.Language AS Language, vf.Version
		FROM dbo.VersionedFields vf
		WHERE vf.ItemId = i.ID
			AND vf.Updated >= @modifiedSince
	) f
    LEFT JOIN dbo.SharedFields np ON np.ItemId = i.ID AND np.FieldId = @neverPublishFieldId
WHERE
	1 = 1 -- Next lines need to be able to be removed.
	AND i.TemplateID = @templateId
	AND i.TemplateID IN (@templateIdsCsv)
	AND i.Updated >= @modifiedSince
    AND (np.Id IS NULL OR np.Value != @neverPublish)