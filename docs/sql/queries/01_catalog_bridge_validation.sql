/*
P2W Deal Finder - Catalog Bridge Validation

Run against: P2WDealFinderDb
Purpose: validate what was imported from the old ecompt2 catalog into the new dealfinder catalog slice.

Current first-pass set:
- Game: Pokemon
- Set: Chaos Rising
- Code: CHR
*/

/* 1. Imported product rows for one set */
SELECT
    CardNumber,
    Name,
    SetName,
    SetCode,
    Rarity,
    ImageUrl,
    CASE WHEN Description IS NULL OR Description = '' THEN 1 ELSE 0 END AS MissingDescription,
    SourceCatalogProductId
FROM CatalogProducts
WHERE GameOrBrand = 'Pokemon'
  AND SetName = 'Chaos Rising'
ORDER BY
    TRY_CONVERT(int, CardNumber),
    CardNumber,
    Name;

/* 2. Set-level catalog completeness counts */
SELECT
    p.GameOrBrand,
    p.SetName,
    p.SetCode,
    COUNT(*) AS Products,
    SUM(CASE WHEN p.ImageUrl IS NULL OR p.ImageUrl = '' THEN 1 ELSE 0 END) AS MissingImages,
    SUM(CASE WHEN p.Description IS NULL OR p.Description = '' THEN 1 ELSE 0 END) AS MissingDescriptions,
    SUM(CASE WHEN p.Rarity IS NULL OR p.Rarity = '' THEN 1 ELSE 0 END) AS MissingRarity,
    SUM(CASE WHEN p.CardNumber IS NULL OR p.CardNumber = '' THEN 1 ELSE 0 END) AS MissingCardNumber
FROM CatalogProducts AS p
WHERE p.GameOrBrand = 'Pokemon'
GROUP BY p.GameOrBrand, p.SetName, p.SetCode
ORDER BY p.GameOrBrand, p.SetName;

/* 3. Provider identifier coverage */
SELECT
    i.SourceName,
    COUNT(*) AS IdentifierCount,
    AVG(i.IdentityConfidence) AS AvgIdentityConfidence,
    MIN(i.IdentityConfidence) AS MinIdentityConfidence,
    MAX(i.IdentityConfidence) AS MaxIdentityConfidence
FROM ProductIdentifiers AS i
JOIN CatalogProducts AS p ON p.Id = i.CatalogProductId
WHERE p.GameOrBrand = 'Pokemon'
  AND p.SetName = 'Chaos Rising'
GROUP BY i.SourceName
ORDER BY i.SourceName;

/* 4. Product rows with identifiers */
SELECT
    p.CardNumber,
    p.Name,
    p.Rarity,
    i.SourceName,
    i.ExternalId,
    i.ExternalSku,
    i.IdentityConfidence,
    i.MatchStatus,
    i.LastVerifiedUtc
FROM CatalogProducts AS p
LEFT JOIN ProductIdentifiers AS i ON i.CatalogProductId = p.Id
WHERE p.GameOrBrand = 'Pokemon'
  AND p.SetName = 'Chaos Rising'
ORDER BY
    TRY_CONVERT(int, p.CardNumber),
    p.CardNumber,
    p.Name,
    i.SourceName;

/* 5. Variant coverage by product */
SELECT
    p.CardNumber,
    p.Name,
    COUNT(v.Id) AS VariantCount,
    STRING_AGG(v.VariantName, ', ') AS Variants
FROM CatalogProducts AS p
LEFT JOIN ProductVariants AS v ON v.CatalogProductId = p.Id
WHERE p.GameOrBrand = 'Pokemon'
  AND p.SetName = 'Chaos Rising'
GROUP BY p.CardNumber, p.Name
ORDER BY
    TRY_CONVERT(int, p.CardNumber),
    p.CardNumber,
    p.Name;

/* 6. Provider coverage rows, including evidence fields that should still be empty at this phase */
SELECT
    c.SourceName,
    COUNT(*) AS CoverageRows,
    SUM(CASE WHEN c.IdentityResolved = 1 THEN 1 ELSE 0 END) AS IdentityResolved,
    SUM(CASE WHEN c.MetadataComplete = 1 THEN 1 ELSE 0 END) AS MetadataComplete,
    SUM(CASE WHEN c.ReferencePricePresent = 1 THEN 1 ELSE 0 END) AS ReferencePricePresent,
    SUM(CASE WHEN c.ActiveListingsPresent = 1 THEN 1 ELSE 0 END) AS ActiveListingsPresent,
    SUM(CASE WHEN c.SoldCompsPresent = 1 THEN 1 ELSE 0 END) AS SoldCompsPresent,
    MAX(c.LastSuccessUtc) AS LastSuccessUtc,
    MAX(c.LastAttemptUtc) AS LastAttemptUtc
FROM CatalogProductProviderCoverage AS c
JOIN CatalogProducts AS p ON p.Id = c.CatalogProductId
WHERE p.GameOrBrand = 'Pokemon'
  AND p.SetName = 'Chaos Rising'
GROUP BY c.SourceName
ORDER BY c.SourceName;

/* 7. Products missing descriptions, useful for backfill planning */
SELECT
    CardNumber,
    Name,
    Rarity,
    ImageUrl,
    SourceCatalogProductId
FROM CatalogProducts
WHERE GameOrBrand = 'Pokemon'
  AND SetName = 'Chaos Rising'
  AND (Description IS NULL OR Description = '')
ORDER BY
    TRY_CONVERT(int, CardNumber),
    CardNumber,
    Name;

/* 8. Possible duplicate product identities inside the imported set */
SELECT
    SetName,
    SetCode,
    CardNumber,
    Name,
    COUNT(*) AS DuplicateCount
FROM CatalogProducts
WHERE GameOrBrand = 'Pokemon'
  AND SetName = 'Chaos Rising'
GROUP BY SetName, SetCode, CardNumber, Name
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC, CardNumber, Name;

/* 9. Sanity check: source catalog ids should be unique */
SELECT
    SourceCatalogProductId,
    COUNT(*) AS RowCount
FROM CatalogProducts
GROUP BY SourceCatalogProductId
HAVING COUNT(*) > 1
ORDER BY RowCount DESC;

/* 10. Quick import summary */
SELECT 'Products' AS RowType, COUNT(*) AS [Count]
FROM CatalogProducts
WHERE GameOrBrand = 'Pokemon' AND SetName = 'Chaos Rising'
UNION ALL
SELECT 'Variants', COUNT(*)
FROM ProductVariants AS v
JOIN CatalogProducts AS p ON p.Id = v.CatalogProductId
WHERE p.GameOrBrand = 'Pokemon' AND p.SetName = 'Chaos Rising'
UNION ALL
SELECT 'Identifiers', COUNT(*)
FROM ProductIdentifiers AS i
JOIN CatalogProducts AS p ON p.Id = i.CatalogProductId
WHERE p.GameOrBrand = 'Pokemon' AND p.SetName = 'Chaos Rising'
UNION ALL
SELECT 'CoverageRows', COUNT(*)
FROM CatalogProductProviderCoverage AS c
JOIN CatalogProducts AS p ON p.Id = c.CatalogProductId
WHERE p.GameOrBrand = 'Pokemon' AND p.SetName = 'Chaos Rising';
