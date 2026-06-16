/*
P2W Cards - Import Quality and Bottlenecks

Read-only queries for understanding catalog import runs, checkpoints, mappings,
errors, stale seed codes, and importer limitations.
*/

/* 1. Latest catalog import runs */
SELECT TOP (100)
    SourceName,
    ImportType,
    Status,
    StartedUtc,
    FinishedUtc,
    DATEDIFF(second, StartedUtc, ISNULL(FinishedUtc, SYSUTCDATETIME())) AS DurationSeconds,
    RecordsProcessed,
    RecordsCreated,
    RecordsUpdated,
    RecordsSkipped,
    ErrorCount,
    Notes
FROM CatalogImportRuns
ORDER BY StartedUtc DESC;

/* 2. Import run totals by source/type/status */
SELECT
    SourceName,
    ImportType,
    Status,
    COUNT(*) AS RunCount,
    SUM(RecordsProcessed) AS TotalProcessed,
    SUM(RecordsCreated) AS TotalCreated,
    SUM(RecordsUpdated) AS TotalUpdated,
    SUM(RecordsSkipped) AS TotalSkipped,
    SUM(ErrorCount) AS TotalErrors,
    MAX(StartedUtc) AS LatestStartedUtc
FROM CatalogImportRuns
GROUP BY SourceName, ImportType, Status
ORDER BY LatestStartedUtc DESC;

/* 3. Current catalog import checkpoints */
SELECT
    SourceName,
    ImportType,
    CheckpointValue,
    UpdatedUtc
FROM CatalogImportCheckpoints
ORDER BY SourceName, ImportType;

/* 4. Recent import errors with product context when available */
SELECT TOP (250)
    e.CreatedUtc,
    e.SourceName,
    r.ImportType,
    r.Status AS RunStatus,
    e.ExternalId,
    e.ErrorMessage,
    LEFT(e.RawSourceJson, 500) AS RawSourcePreview
FROM CatalogImportErrors AS e
LEFT JOIN CatalogImportRuns AS r ON r.Id = e.CatalogImportRunId
ORDER BY e.CreatedUtc DESC;

/* 5. Mapping status needing human review */
SELECT TOP (250)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name AS ProductName,
    m.SourceName,
    m.ExternalId,
    m.ConfidenceScore,
    m.MappingStatus,
    m.ExternalUrl,
    m.LastVerifiedUtc
FROM ExternalProductMappings AS m
JOIN CatalogProducts AS p ON p.Id = m.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE m.MappingStatus <> 'AutoMatched'
   OR m.ConfidenceScore < 0.85
ORDER BY m.LastVerifiedUtc DESC, g.Name, s.Name, p.Name;

/* 6. Stale seed-code candidates.
   These are sets where products exist by set name but code may not match provider expectations.
   Run this before/after catalog sync to see if the importer repaired provider codes.
*/
SELECT
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS CurrentSetCode,
    COUNT(p.Id) AS ProductCount,
    COUNT(CASE WHEN p.IsSingleCard = 1 THEN 1 END) AS SingleCardCount,
    MAX(p.UpdatedUtc) AS LatestProductUpdatedUtc,
    SUM(CASE WHEN p.Name LIKE '%Sample%' THEN 1 ELSE 0 END) AS SampleProductCount,
    CASE
        WHEN g.Slug = 'pokemon' AND s.Code IN ('ASH', 'MEG', 'CHR', 'DRI', 'PRE') THEN 'Pokemon seed code likely stale'
        WHEN g.Slug = 'one-piece' AND SUM(CASE WHEN p.Name LIKE '%Sample%' THEN 1 ELSE 0 END) > 0 THEN 'One Piece seeded sample data present'
        ELSE 'Review if provider validation reports this set'
    END AS ReviewReason
FROM CardSets AS s
JOIN Games AS g ON g.Id = s.GameId
LEFT JOIN CatalogProducts AS p ON p.CardSetId = s.Id AND p.IsActive = 1
WHERE g.Slug IN ('pokemon', 'one-piece')
GROUP BY g.Name, g.Slug, s.Name, s.Code
HAVING s.Code IN ('ASH', 'MEG', 'CHR', 'DRI', 'PRE')
    OR SUM(CASE WHEN p.Name LIKE '%Sample%' THEN 1 ELSE 0 END) > 0
ORDER BY g.Name, s.Name;

/* 7. Products whose external mapping points to a different source id pattern than expected */
SELECT TOP (250)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    m.SourceName,
    m.ExternalId,
    CASE
        WHEN m.SourceName = 'PokemonTCG' AND m.ExternalId NOT LIKE '%-%' THEN 'PokemonTCG id does not look like set-number'
        WHEN m.SourceName = 'OnePieceOfficial' AND m.ExternalId NOT LIKE 'OP%' AND m.ExternalId NOT LIKE 'ST%' AND m.ExternalId NOT LIKE 'EB%' AND m.ExternalId NOT LIKE 'PRB%' THEN 'One Piece external id pattern unusual'
        ELSE 'Pattern OK'
    END AS PatternCheck
FROM ExternalProductMappings AS m
JOIN CatalogProducts AS p ON p.Id = m.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE (m.SourceName = 'PokemonTCG' AND m.ExternalId NOT LIKE '%-%')
   OR (m.SourceName = 'OnePieceOfficial' AND m.ExternalId NOT LIKE 'OP%' AND m.ExternalId NOT LIKE 'ST%' AND m.ExternalId NOT LIKE 'EB%' AND m.ExternalId NOT LIKE 'PRB%')
ORDER BY g.Name, s.Name, p.Name;

/* 8. Catalog products updated by import recently */
DECLARE @SinceUtc datetime2 = DATEADD(day, -2, SYSUTCDATETIME());

SELECT TOP (300)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    p.Rarity,
    p.UpdatedUtc,
    p.ImageUrl,
    LEFT(p.Description, 200) AS DescriptionPreview
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE p.UpdatedUtc >= @SinceUtc
ORDER BY p.UpdatedUtc DESC;

/* 9. Sets with products but no source mappings */
SELECT
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    COUNT(p.Id) AS ProductCount,
    SUM(CASE WHEN m.Id IS NULL THEN 1 ELSE 0 END) AS ProductsWithoutMappings
FROM CardSets AS s
JOIN Games AS g ON g.Id = s.GameId
JOIN CatalogProducts AS p ON p.CardSetId = s.Id AND p.IsActive = 1
LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
GROUP BY g.Name, s.Name, s.Code
HAVING SUM(CASE WHEN m.Id IS NULL THEN 1 ELSE 0 END) > 0
ORDER BY ProductsWithoutMappings DESC, g.Name, s.Name;

/* 10. Import error rate by run */
SELECT TOP (100)
    r.SourceName,
    r.ImportType,
    r.StartedUtc,
    r.Status,
    r.RecordsProcessed,
    r.ErrorCount,
    CAST(100.0 * r.ErrorCount / NULLIF(r.RecordsProcessed + r.ErrorCount, 0) AS decimal(9, 2)) AS ErrorPercent,
    r.Notes
FROM CatalogImportRuns AS r
WHERE r.RecordsProcessed > 0 OR r.ErrorCount > 0
ORDER BY r.StartedUtc DESC;
