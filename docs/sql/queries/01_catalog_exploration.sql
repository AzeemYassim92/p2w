/*
P2W Cards - Catalog Exploration

Read-only queries for understanding Games -> Sets -> Products -> Variants -> Mappings.
*/

/* 1. Game-level catalog overview */
SELECT
    g.Name AS Game,
    g.Slug AS GameSlug,
    COUNT(DISTINCT s.Id) AS SetCount,
    COUNT(DISTINCT p.Id) AS ProductCount,
    SUM(CASE WHEN p.IsSingleCard = 1 THEN 1 ELSE 0 END) AS SingleCardCount,
    SUM(CASE WHEN p.IsSealed = 1 THEN 1 ELSE 0 END) AS SealedCount,
    SUM(CASE WHEN p.ImageUrl IS NULL OR p.ImageUrl = '' THEN 1 ELSE 0 END) AS MissingImageCount,
    SUM(CASE WHEN p.Description IS NULL OR p.Description = '' THEN 1 ELSE 0 END) AS MissingDescriptionCount,
    COUNT(DISTINCT m.Id) AS ExternalMappingCount
FROM Games AS g
LEFT JOIN CardSets AS s ON s.GameId = g.Id AND s.IsActive = 1
LEFT JOIN CatalogProducts AS p ON p.GameId = g.Id AND p.IsActive = 1
LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
GROUP BY g.Name, g.Slug
ORDER BY g.DisplayOrder, g.Name;

/* 2. Set coverage, sorted by game and release date */
SELECT
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    s.ReleaseDate,
    s.IsUpcoming,
    COUNT(p.Id) AS ProductCount,
    SUM(CASE WHEN p.IsSingleCard = 1 THEN 1 ELSE 0 END) AS SingleCardCount,
    SUM(CASE WHEN p.IsSealed = 1 THEN 1 ELSE 0 END) AS SealedCount,
    SUM(CASE WHEN p.ImageUrl IS NULL OR p.ImageUrl = '' THEN 1 ELSE 0 END) AS MissingImageCount,
    SUM(CASE WHEN p.Rarity IS NULL OR p.Rarity = '' THEN 1 ELSE 0 END) AS MissingRarityCount,
    COUNT(DISTINCT m.Id) AS ExternalMappingCount
FROM CardSets AS s
JOIN Games AS g ON g.Id = s.GameId
LEFT JOIN CatalogProducts AS p ON p.CardSetId = s.Id AND p.IsActive = 1
LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
WHERE g.Slug IN ('pokemon', 'one-piece', 'magic-the-gathering')
GROUP BY g.Name, s.Name, s.Code, s.ReleaseDate, s.IsUpcoming
ORDER BY g.Name, s.ReleaseDate DESC, s.Name;

/* 3. Products for one set: change @GameSlug and @SetCode */
DECLARE @GameSlug nvarchar(140) = 'pokemon';
DECLARE @SetCode nvarchar(450) = 'ME2';

SELECT
    p.Id AS CatalogProductId,
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    p.Rarity,
    p.ProductType,
    p.IsSingleCard,
    p.IsSealed,
    p.ImageUrl,
    p.Description,
    COUNT(DISTINCT v.Id) AS VariantCount,
    COUNT(DISTINCT m.Id) AS MappingCount
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
LEFT JOIN ProductVariants AS v ON v.CatalogProductId = p.Id
LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
WHERE g.Slug = @GameSlug
  AND (@SetCode IS NULL OR s.Code = @SetCode)
  AND p.IsActive = 1
GROUP BY
    p.Id, g.Name, s.Name, s.Code, p.CardNumber, p.Name, p.Rarity,
    p.ProductType, p.IsSingleCard, p.IsSealed, p.ImageUrl, p.Description
ORDER BY
    TRY_CONVERT(int, LEFT(ISNULL(p.CardNumber, ''), PATINDEX('%[^0-9]%', ISNULL(p.CardNumber, '') + 'x') - 1)),
    p.CardNumber,
    p.Name;

/* 4. Possible duplicate catalog products inside the same set */
SELECT
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.NormalizedName,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CONVERT(varchar(36), p.Id), ', ') AS ProductIds
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE p.IsActive = 1
  AND p.IsSingleCard = 1
GROUP BY g.Name, s.Name, s.Code, p.CardNumber, p.NormalizedName
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC, g.Name, s.Name, p.CardNumber;

/* 5. Same card name across many sets: legitimate reprints, not necessarily duplicates */
SELECT TOP (200)
    g.Name AS Game,
    p.Name,
    COUNT(DISTINCT s.Id) AS SetCount,
    COUNT(*) AS ProductRows,
    STRING_AGG(CONCAT(s.Code, ':', ISNULL(p.CardNumber, '?')), ', ') AS Printings
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE p.IsActive = 1
  AND p.IsSingleCard = 1
GROUP BY g.Name, p.Name
HAVING COUNT(DISTINCT s.Id) > 1
ORDER BY SetCount DESC, ProductRows DESC, p.Name;

/* 6. Products missing important display fields */
SELECT TOP (250)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    p.Rarity,
    CASE WHEN p.ImageUrl IS NULL OR p.ImageUrl = '' THEN 1 ELSE 0 END AS MissingImage,
    CASE WHEN p.Description IS NULL OR p.Description = '' THEN 1 ELSE 0 END AS MissingDescription,
    CASE WHEN p.CardNumber IS NULL OR p.CardNumber = '' THEN 1 ELSE 0 END AS MissingCardNumber,
    CASE WHEN p.Rarity IS NULL OR p.Rarity = '' THEN 1 ELSE 0 END AS MissingRarity
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE p.IsActive = 1
  AND p.IsSingleCard = 1
  AND
  (
      p.ImageUrl IS NULL OR p.ImageUrl = ''
      OR p.Description IS NULL OR p.Description = ''
      OR p.CardNumber IS NULL OR p.CardNumber = ''
      OR p.Rarity IS NULL OR p.Rarity = ''
  )
ORDER BY g.Name, s.ReleaseDate DESC, s.Name, p.Name;

/* 7. External mapping coverage by provider */
SELECT
    g.Name AS Game,
    m.SourceName,
    m.MappingStatus,
    COUNT(*) AS MappingCount,
    AVG(m.ConfidenceScore) AS AvgConfidence,
    MIN(m.LastVerifiedUtc) AS OldestVerifiedUtc,
    MAX(m.LastVerifiedUtc) AS NewestVerifiedUtc
FROM ExternalProductMappings AS m
JOIN CatalogProducts AS p ON p.Id = m.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
GROUP BY g.Name, m.SourceName, m.MappingStatus
ORDER BY g.Name, m.SourceName, m.MappingStatus;

/* 8. Products without any external mapping */
SELECT TOP (250)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    p.ProductType,
    p.CreatedUtc,
    p.UpdatedUtc
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE p.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM ExternalProductMappings AS m
      WHERE m.CatalogProductId = p.Id
  )
ORDER BY g.Name, s.ReleaseDate DESC, s.Name, p.Name;

/* 9. Variant distribution */
SELECT
    g.Name AS Game,
    v.VariantName,
    COUNT(*) AS VariantRows,
    SUM(CASE WHEN v.IsFoil = 1 THEN 1 ELSE 0 END) AS FoilRows,
    SUM(CASE WHEN v.IsReverseHolo = 1 THEN 1 ELSE 0 END) AS ReverseHoloRows,
    SUM(CASE WHEN v.IsPromo = 1 THEN 1 ELSE 0 END) AS PromoRows
FROM ProductVariants AS v
JOIN CatalogProducts AS p ON p.Id = v.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
GROUP BY g.Name, v.VariantName
ORDER BY g.Name, VariantRows DESC;

/* 10. Seed or placeholder-looking products that should not pollute card-count validation */
SELECT
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.Name,
    p.ProductType,
    p.IsSingleCard,
    p.IsSealed,
    p.ImageUrl
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE p.Name LIKE '%Sample%'
   OR p.ImageUrl LIKE 'https://placehold.co/%'
ORDER BY g.Name, s.Name, p.Name;
