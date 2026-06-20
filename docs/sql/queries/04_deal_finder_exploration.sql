/*
P2W Deal Finder - Exploration Queries

These are planned read-only queries for the new dealfinder schema.
Run them after EF migrations or SQL table creation exists.
*/

/* 1. Catalog overview by game/brand and product type */
SELECT
    GameOrBrand,
    ProductType,
    COUNT(*) AS ProductCount,
    SUM(CASE WHEN ImageUrl IS NULL OR ImageUrl = '' THEN 1 ELSE 0 END) AS MissingImages,
    SUM(CASE WHEN Description IS NULL OR Description = '' THEN 1 ELSE 0 END) AS MissingDescriptions
FROM CatalogProducts
WHERE IsActive = 1
GROUP BY GameOrBrand, ProductType
ORDER BY GameOrBrand, ProductType;

/* 2. Set-level Pokemon coverage */
SELECT
    SetName,
    SetCode,
    COUNT(*) AS ProductCount,
    MIN(CardNumber) AS FirstCardNumber,
    MAX(CardNumber) AS LastCardNumber,
    SUM(CASE WHEN Rarity IS NULL OR Rarity = '' THEN 1 ELSE 0 END) AS MissingRarity,
    SUM(CASE WHEN ImageUrl IS NULL OR ImageUrl = '' THEN 1 ELSE 0 END) AS MissingImages
FROM CatalogProducts
WHERE GameOrBrand = 'Pokemon'
  AND IsActive = 1
GROUP BY SetName, SetCode
ORDER BY SetName;

/* 3. Provider identity mapping coverage */
SELECT
    p.GameOrBrand,
    p.SetName,
    i.SourceName,
    COUNT(DISTINCT p.Id) AS ProductsMapped,
    AVG(i.IdentityConfidence) AS AvgIdentityConfidence,
    SUM(CASE WHEN i.MatchStatus = 2 THEN 1 ELSE 0 END) AS NeedsReviewCount
FROM CatalogProducts AS p
JOIN ProductIdentifiers AS i ON i.CatalogProductId = p.Id
WHERE p.IsActive = 1
GROUP BY p.GameOrBrand, p.SetName, i.SourceName
ORDER BY p.GameOrBrand, p.SetName, i.SourceName;

/* 4. Provider coverage bottlenecks */
SELECT TOP (250)
    p.GameOrBrand,
    p.SetName,
    p.CardNumber,
    p.Name,
    c.SourceName,
    c.IdentityResolved,
    c.MetadataComplete,
    c.ReferencePricePresent,
    c.ActiveListingsPresent,
    c.SoldCompsPresent,
    c.LastAttemptUtc,
    c.LastSuccessUtc,
    c.LastNoDataReason,
    c.LastError
FROM CatalogProductProviderCoverage AS c
JOIN CatalogProducts AS p ON p.Id = c.CatalogProductId
ORDER BY c.LastAttemptUtc DESC, p.GameOrBrand, p.SetName, p.CardNumber;

/* 5. Latest provider observations and no-data reasons */
SELECT TOP (300)
    o.SourceName,
    o.WorkloadType,
    o.Status,
    o.RecordsReturned,
    o.RecordsAccepted,
    o.RecordsRejected,
    o.QueryText,
    o.NoDataReason,
    o.ErrorMessage,
    o.StartedUtc,
    o.FinishedUtc
FROM ProviderObservations AS o
ORDER BY o.StartedUtc DESC;

/* 6. Products with active listings but no sold comps */
SELECT TOP (300)
    p.GameOrBrand,
    p.SetName,
    p.CardNumber,
    p.Name,
    COUNT(DISTINCT l.Id) AS ActiveListings,
    COUNT(DISTINCT s.Id) AS SoldComps,
    MIN(l.EffectiveBuyPrice) AS LowestActiveBuyPrice,
    MAX(l.LastSeenUtc) AS LastListingSeenUtc
FROM CatalogProducts AS p
LEFT JOIN ActiveListings AS l ON l.CatalogProductId = p.Id AND l.IsExcluded = 0
LEFT JOIN SoldComps AS s ON s.CatalogProductId = p.Id AND s.IsExcluded = 0
WHERE p.IsActive = 1
GROUP BY p.GameOrBrand, p.SetName, p.CardNumber, p.Name
HAVING COUNT(DISTINCT l.Id) > 0
   AND COUNT(DISTINCT s.Id) = 0
ORDER BY ActiveListings DESC, LowestActiveBuyPrice ASC;

/* 7. Latest market snapshots by confidence */
WITH LatestSnapshot AS
(
    SELECT
        ms.*,
        ROW_NUMBER() OVER (PARTITION BY ms.CatalogProductId, ms.ProductVariantId, ms.Condition, ms.Currency ORDER BY ms.CapturedAtUtc DESC) AS rn
    FROM MarketSnapshots AS ms
)
SELECT TOP (250)
    p.GameOrBrand,
    p.SetName,
    p.CardNumber,
    p.Name,
    ls.MarketValueBasis,
    ls.ExpectedMarketValue,
    ls.ActiveListingCount,
    ls.SoldCompCount,
    ls.MarketConfidence,
    ls.LiquidityScore,
    ls.CapturedAtUtc
FROM LatestSnapshot AS ls
JOIN CatalogProducts AS p ON p.Id = ls.CatalogProductId
WHERE ls.rn = 1
ORDER BY ls.MarketConfidence ASC, ls.CapturedAtUtc DESC;

/* 8. Deal candidates above margin threshold */
SELECT TOP (250)
    p.GameOrBrand,
    p.SetName,
    p.CardNumber,
    p.Name,
    d.SourceName,
    d.ListingTitle,
    d.EffectiveBuyPrice,
    d.ExpectedMarketValue,
    d.MarketValueBasis,
    d.EstimatedNetProfit,
    d.EstimatedNetMarginPercent,
    d.EstimatedRoiPercent,
    d.IdentityConfidence,
    d.MarketConfidence,
    d.LiquidityScore,
    d.Status,
    d.Explanation,
    d.RiskFlags,
    d.ScoredAtUtc,
    d.ListingUrl
FROM DealCandidates AS d
JOIN CatalogProducts AS p ON p.Id = d.CatalogProductId
WHERE d.EstimatedNetMarginPercent >= 0.10
ORDER BY d.EstimatedNetProfit DESC, d.EstimatedRoiPercent DESC;

/* 9. Candidates blocked by missing sold comps */
SELECT TOP (250)
    p.GameOrBrand,
    p.SetName,
    p.CardNumber,
    p.Name,
    d.ListingTitle,
    d.EffectiveBuyPrice,
    d.ExpectedMarketValue,
    d.EstimatedNetProfit,
    d.EstimatedNetMarginPercent,
    d.MarketConfidence,
    d.Explanation,
    d.RiskFlags,
    d.ScoredAtUtc
FROM DealCandidates AS d
JOIN CatalogProducts AS p ON p.Id = d.CatalogProductId
WHERE d.RiskFlags LIKE '%no sold comps%'
ORDER BY d.ScoredAtUtc DESC;

/* 10. Decision history audit */
SELECT TOP (250)
    h.DecidedAtUtc,
    h.Decision,
    h.DecidedBy,
    h.ManualOfferPrice,
    h.Notes,
    d.ListingTitle,
    d.EffectiveBuyPrice,
    d.ExpectedMarketValue,
    d.EstimatedNetProfit,
    d.ListingUrl
FROM DealDecisionHistory AS h
JOIN DealCandidates AS d ON d.Id = h.DealCandidateId
ORDER BY h.DecidedAtUtc DESC;
