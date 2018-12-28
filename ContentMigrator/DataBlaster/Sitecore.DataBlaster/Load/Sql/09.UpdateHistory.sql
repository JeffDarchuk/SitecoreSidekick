PRINT CONVERT(VARCHAR(12), GETDATE(), 114) + ': Updating history engine...'

DECLARE @Category VARCHAR(50) = 'Item'
--DECLARE @UserName NVARCHAR(250) = 'default\Anonymous'
DECLARE @TaskStack VARCHAR(MAX) = '[none]'
DECLARE @AdditionalInfo NVARCHAR(MAX) = ''

-- Missing:
--  master/template change without changed fields (very unlikely)
--  blob change without field change
--  added/removed version

DECLARE @Actions TABLE
(
    Action VARCHAR(50),
    Created BIT,
    Saved BIT,
    Moved BIT,
    Deleted BIT
)
INSERT INTO @Actions (Action, Created) VALUES ('Created', 1)
INSERT INTO @Actions (Action, Saved) VALUES ('Saved', 1)
INSERT INTO @Actions (Action, Moved) VALUES ('Moved', 1)
INSERT INTO @Actions (Action, Deleted) VALUES ('Deleted', 1)


INSERT INTO 
    History (Id, Category, Action, ItemId, ItemLanguage, ItemVersion, ItemPath, UserName, TaskStack, AdditionalInfo, Created)
SELECT
    NEWID(), @Category, a.Action, 
    ia.ItemId, ISNULL(ia.Language, ''), ISNULL(ia.Version, 0), ISNULL(ia.ItemPath, ''), @UserName, @TaskStack, @AdditionalInfo, ia.Timestamp
FROM
    (
        SELECT si.ItemPath, ia.ItemId,
            CAST(MAX(CAST(ia.Created AS INT)) AS BIT) AS Created, 
	        CAST(MAX(CAST(ia.Saved AS INT)) AS BIT) AS Saved, 
	        CAST(MAX(CAST(ia.Moved AS INT)) AS BIT) AS Moved, 
	        CAST(MAX(CAST(ia.Deleted AS INT)) AS BIT) AS Deleted, 
            ia.Language,
            ia.Version,
            ia.Timestamp
        FROM #ItemActions ia
            JOIN #SyncItems si ON 
                si.Id = ia.ItemId
        GROUP BY si.ItemPath, ia.ItemId, ia.Language, ia.Version, ia.Timestamp
    ) ia
    JOIN @Actions a ON
        a.Created = ia.Created 
        OR a.Saved = ia.Saved
        OR a.Moved = ia.Moved
        OR a.Deleted = ia.Deleted
ORDER BY
    -- Parents need to be inserted first.
    -- Don't use CLR for now.
    --dbo.ItemPathLevel(ItemPath)
    (LEN(ItemPath) - LEN(REPLACE(ItemPath, '/', '')))