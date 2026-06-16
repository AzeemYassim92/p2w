/*
P2W Cards - Market Aggregation Exploration

Read-only queries for active listings, sold comps, snapshots, market metrics,
confidence, provider coverage, and deal-signal sanity checks.
*/

/* 1. Marketplace source capability map */
SELECT
    Name,
    Slug,
    IsActive,
    SupportsListings,
    SupportsSoldComps,
    SupportsBuylist,
    SupportsReferencePrices,
    SupportsBulkCsv,
    DefaultCurrency,
    PriorityRank
FROM MarketplaceSources
ORDER BY PriorityRank, Name;

/* 2. Coverage by game: products with reference prices, active listings, sold comps, snapshots, metrics */
SELECT
    g.Name AS Game,
    COUNT(DISTINCT p.Id) AS ProductCount,
    COUNT(DISTINCT CASE WHEN ref.Id IS NOT NULL THEN p.Id END) AS ProductsWithReferencePrices,
    COUNT(DISTINCT CASE WHEN l.Id IS NOT NULL THEN p.Id END) AS ProductsWithActiveListings,
    COUNT(DISTINCT CASE WHEN sale.Id IS NOT NULL THEN p.Id END) AS ProductsWithSoldComps,
    COUNT(DISTINCT CASE WHEN snap.Id IS NOT NULL THEN p.Id END) AS ProductsWithMarketSnapshots,
    COUNT(DISTINCT CASE WHEN metric.Id IS NOT NULL THEN p.Id END) AS ProductsWithMarketMetrics
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CatalogPriceReferenceSnapshots AS ref ON ref.CatalogProductId = p.Id
LEFT JOIN CatalogMarketplaceListings AS l ON l.CatalogProductId = p.Id AND l.IsActive = 1 AND l.IsExcludedFromMarketValue = 0
LEFT JOIN CatalogMarketplaceSales AS sale ON sale.CatalogProductId = p.Id AND sale.IsExcludedFromMarketValue = 0
LEFT JOIN CatalogMarketPriceSnapshots AS snap ON snap.CatalogProductId = p.Id
LEFT JOIN CatalogMarketMetrics AS metric ON metric.CatalogProductId = p.Id
WHERE p.IsActive = 1
  AND p.IsSingleCard = 1
GROUP BY g.Name
ORDER BY g.Name;

/* 3. Latest market snapshot per product */
WITH LatestSnapshot AS
(
    SELECT
        snap.*,
        ROW_NUMBER() OVER (PARTITION BY snap.CatalogProductId, snap.Condition, snap.Currency ORDER BY snap.CapturedAtUtc DESC) AS rn
    FROM CatalogMarketPriceSnapshots AS snap
)
SELECT TOP (250)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    ls.Condition,
    ls.Currency,
    ls.ReferenceMarketPrice,
    ls.MedianListingPrice,
    ls.AverageListingPrice,
    ls.LowestListingPrice,
    ls.HighestListingPrice,
    ls.LastSoldPrice,
    ls.MedianSoldPrice,
    ls.ListingCount,
    ls.SoldCount,
    ls.SalesVolume,
    ls.CapturedAtUtc
FROM LatestSnapshot AS ls
JOIN CatalogProducts AS p ON p.Id = ls.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE ls.rn = 1
ORDER BY ls.CapturedAtUtc DESC, g.Name, p.Name;

/* 4. Latest market metrics and confidence sanity */
WITH LatestMetric AS
(
    SELECT
        metric.*,
        ROW_NUMBER() OVER (PARTITION BY metric.CatalogProductId, metric.Condition, metric.Currency, metric.WindowName ORDER BY metric.ComputedAtUtc DESC) AS rn
    FROM CatalogMarketMetrics AS metric
)
SELECT TOP (250)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    lm.WindowName,
    lm.Condition,
    lm.CurrentMarketPrice,
    lm.PreviousMarketPrice,
    lm.PriceChangePercent,
    lm.ListingCount,
    lm.SoldCount,
    lm.SalesVolume,
    lm.ConfidenceScore,
    lm.LiquidityScore,
    lm.OpportunityScore,
    lm.EstimatedNetMargin,
    lm.EstimatedRoiPercent,
    lm.ComputedAtUtc
FROM LatestMetric AS lm
JOIN CatalogProducts AS p ON p.Id = lm.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE lm.rn = 1
ORDER BY lm.ComputedAtUtc DESC, lm.OpportunityScore DESC, lm.ConfidenceScore ASC;

/* 5. Products where confidence may be inflated because sold comps are missing */
WITH LatestMetric AS
(
    SELECT
        metric.*,
        ROW_NUMBER() OVER (PARTITION BY metric.CatalogProductId, metric.Condition, metric.Currency, metric.WindowName ORDER BY metric.ComputedAtUtc DESC) AS rn
    FROM CatalogMarketMetrics AS metric
)
SELECT TOP (200)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    lm.CurrentMarketPrice,
    lm.ListingCount,
    lm.SoldCount,
    lm.ConfidenceScore,
    lm.ComputedAtUtc
FROM LatestMetric AS lm
JOIN CatalogProducts AS p ON p.Id = lm.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE lm.rn = 1
  AND lm.ListingCount > 0
  AND lm.SoldCount = 0
  AND ISNULL(lm.ConfidenceScore, 0) >= 0.50
ORDER BY lm.ConfidenceScore DESC, lm.ListingCount DESC;

/* 6. Active listings: inspect match quality and outliers */
SELECT TOP (300)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name AS ProductName,
    l.SourceName,
    l.Title,
    l.Condition,
    l.Price,
    l.ShippingPrice,
    l.EffectivePrice,
    l.Quantity,
    l.SellerName,
    l.MatchConfidence,
    l.MatchStatus,
    l.IsExcludedFromMarketValue,
    l.ExclusionReason,
    l.LastSeenUtc,
    l.ListingUrl
FROM CatalogMarketplaceListings AS l
JOIN CatalogProducts AS p ON p.Id = l.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE l.IsActive = 1
ORDER BY l.LastSeenUtc DESC, l.EffectivePrice ASC;

/* 7. Sold comps: should be sparse until a real sold-comps source exists */
SELECT TOP (300)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name AS ProductName,
    sale.SourceName,
    sale.Title,
    sale.Condition,
    sale.SoldPrice,
    sale.ShippingPrice,
    sale.EffectiveSoldPrice,
    sale.Quantity,
    sale.SellerName,
    sale.SoldAtUtc,
    sale.MatchConfidence,
    sale.MatchStatus,
    sale.IsExcludedFromMarketValue,
    sale.ExclusionReason
FROM CatalogMarketplaceSales AS sale
JOIN CatalogProducts AS p ON p.Id = sale.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
ORDER BY sale.SoldAtUtc DESC;

/* 8. Listing outliers per product */
WITH ListingStats AS
(
    SELECT
        CatalogProductId,
        COUNT(*) AS ListingCount,
        MIN(EffectivePrice) AS MinEffectivePrice,
        AVG(EffectivePrice) AS AvgEffectivePrice,
        MAX(EffectivePrice) AS MaxEffectivePrice
    FROM CatalogMarketplaceListings
    WHERE IsActive = 1
      AND IsExcludedFromMarketValue = 0
    GROUP BY CatalogProductId
)
SELECT TOP (200)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    ls.ListingCount,
    ls.MinEffectivePrice,
    ls.AvgEffectivePrice,
    ls.MaxEffectivePrice,
    CAST(ls.MaxEffectivePrice / NULLIF(ls.MinEffectivePrice, 0) AS decimal(18, 2)) AS MaxToMinRatio
FROM ListingStats AS ls
JOIN CatalogProducts AS p ON p.Id = ls.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE ls.ListingCount >= 5
  AND ls.MinEffectivePrice > 0
  AND ls.MaxEffectivePrice / NULLIF(ls.MinEffectivePrice, 0) >= 10
ORDER BY MaxToMinRatio DESC, ls.ListingCount DESC;

/* 9. Provider ingestion runs and errors */
SELECT TOP (100)
    SourceName,
    WorkloadType,
    Status,
    StartedUtc,
    FinishedUtc,
    DATEDIFF(second, StartedUtc, ISNULL(FinishedUtc, SYSUTCDATETIME())) AS DurationSeconds,
    RecordsProcessed,
    RecordsCreated,
    RecordsUpdated,
    RecordsSkipped,
    ErrorCount,
    CheckpointBefore,
    CheckpointAfter,
    Notes
FROM CatalogProviderIngestionRuns
ORDER BY StartedUtc DESC;

SELECT TOP (200)
    e.CreatedUtc,
    e.SourceName,
    e.WorkloadType,
    e.ExternalId,
    e.CatalogProductId,
    e.ErrorMessage,
    LEFT(e.RawSourceJson, 500) AS RawSourcePreview
FROM CatalogProviderIngestionErrors AS e
ORDER BY e.CreatedUtc DESC;

/* 10. Deal-signal sanity: positive ROI but low confidence should be treated carefully */
WITH LatestMetric AS
(
    SELECT
        metric.*,
        ROW_NUMBER() OVER (PARTITION BY metric.CatalogProductId, metric.Condition, metric.Currency, metric.WindowName ORDER BY metric.ComputedAtUtc DESC) AS rn
    FROM CatalogMarketMetrics AS metric
)
SELECT TOP (200)
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name,
    lm.CurrentMarketPrice,
    lm.EstimatedNetMargin,
    lm.EstimatedRoiPercent,
    lm.OpportunityScore,
    lm.ConfidenceScore,
    lm.ListingCount,
    lm.SoldCount,
    lm.ComputedAtUtc
FROM LatestMetric AS lm
JOIN CatalogProducts AS p ON p.Id = lm.CatalogProductId
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
WHERE lm.rn = 1
  AND ISNULL(lm.EstimatedRoiPercent, 0) > 0
ORDER BY lm.ConfidenceScore ASC, lm.EstimatedRoiPercent DESC;
