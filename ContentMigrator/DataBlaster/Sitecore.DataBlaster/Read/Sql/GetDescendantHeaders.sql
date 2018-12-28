DECLARE @rootItemId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111'
DECLARE @rootItemPath VARCHAR(MAX) = '/sitecore'

DECLARE @templateId UNIQUEIDENTIFIER = '455A3E98-A627-4B40-8035-E683A0331AC7' -- Template fields
DECLARE @modifiedSince DATETIME = '2010-01-01'

-- BEGIN: Lines before this line will be ignored when reading this embedded sql.

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
)

SELECT 
	cte.Id, 
	i.Name, 
	cte.Path AS ItemPath, 
	i.TemplateID AS TemplateId,
	i.MasterId AS MasterId,
	cte.ParentId,
	i.Created,
	i.Updated
FROM 
	LookupCTE cte
	JOIN Items i ON 
		i.ID = cte.Id
WHERE
	1 = 1 -- Next lines need to be able to be removed.
	AND i.TemplateID = @templateId
	AND i.TemplateID IN (@templateIdsCsv)
    AND i.Updated >= @modifiedSince