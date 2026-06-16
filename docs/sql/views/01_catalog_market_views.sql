/*
P2W Cards - Optional Views

Run this file in SSMS if you want reusable explorer views.
These statements create or alter helper views only. They do not change product data.
*/

CREATE OR ALTER VIEW dbo.vw_P2W_CatalogProductExplorer
AS
SELECT
    p.Id AS CatalogProductId,
    g.Name AS Game,
    g.Slug AS GameSlug,
    s.Id AS CardSetId,
    s.Name AS SetName,
    s.Code AS SetCode,
    s.ReleaseDate AS SetReleaseDate,
    c.Name AS CategoryName,
    c.Slug AS CategorySlug,
    p.Name AS ProductName,
    p.NormalizedName,
    p.Slug AS ProductSlug,
    p.ProductType,
    p.CardNumber,
    p.Rarity,
    p.Artist,
    p.ImageUrl,
    p.IsSingleCard,
    p.IsSealed,
    p.IsActive,
    p.CreatedUtc,
    p.UpdatedUtc,
    COUNT(DISTINCT v.Id) AS VariantCount,
    COUNT(DISTINCT m.Id) AS ExternalMappingCount
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
LEFT JOIN ProductCategories AS c ON c.Id = p.ProductCategoryId
LEFT JOIN ProductVariants AS v ON v.CatalogProductId = p.Id
LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
GROUP BY
    p.Id, g.Name, g.Slug, s.Id, s.Name, s.Code, s.ReleaseDate,
    c.Name, c.Slug, p.Name, p.NormalizedName, p.Slug, p.ProductType,
    p.CardNumber, p.Rarity, p.Artist, p.ImageUrl, p.IsSingleCard,
    p.IsSealed, p.IsActive, p.CreatedUtc, p.UpdatedUtc;
GO

CREATE OR ALTER VIEW dbo.vw_P2W_SetCoverageSummary
AS
SELECT
    g.Name AS Game,
    g.Slug AS GameSlug,
    s.Id AS CardSetId,
    s.Name AS SetName,
    s.Code AS SetCode,
    s.ReleaseDate,
    s.IsUpcoming,
    COUNT(p.Id) AS ProductCount,
    SUM(CASE WHEN p.IsSingleCard = 1 THEN 1 ELSE 0 END) AS SingleCardCount,
    SUM(CASE WHEN p.IsSealed = 1 THEN 1 ELSE 0 END) AS SealedCount,
    SUM(CASE WHEN p.ImageUrl IS NULL OR p.ImageUrl = '' THEN 1 ELSE 0 END) AS MissingImageCount,
    SUM(CASE WHEN p.Description IS NULL OR p.Description = '' THEN 1 ELSE 0 END) AS MissingDescriptionCount,
    SUM(CASE WHEN p.Rarity IS NULL OR p.Rarity = '' THEN 1 ELSE 0 END) AS MissingRarityCount,
    COUNT(DISTINCT m.Id) AS ExternalMappingCount
FROM CardSets AS s
JOIN Games AS g ON g.Id = s.GameId
LEFT JOIN CatalogProducts AS p ON p.CardSetId = s.Id AND p.IsActive = 1
LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
WHERE s.IsActive = 1
GROUP BY g.Name, g.Slug, s.Id, s.Name, s.Code, s.ReleaseDate, s.IsUpcoming;
GO

CREATE OR ALTER VIEW dbo.vw_P2W_ProductProviderCoverage
AS
SELECT
    p.Id AS CatalogProductId,
    g.Name AS Game,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Name AS ProductName,
    p.IsSingleCard,
    COUNT(DISTINCT epm.Id) AS CatalogMappingCount,
    COUNT(DISTINCT ref.Id) AS ReferencePriceRows,
    COUNT(DISTINCT l.Id) AS ActiveListingRows,
    COUNT(DISTINCT sale.Id) AS SoldCompRows,
    COUNT(DISTINCT snap.Id) AS MarketSnapshotRows,
    COUNT(DISTINCT metric.Id) AS MarketMetricRows,
    MAX(ref.CapturedAtUtc) AS LatestReferencePriceUtc,
    MAX(l.LastSeenUtc) AS LatestListingUtc,
    MAX(sale.SoldAtUtc) AS LatestSoldCompUtc,
    MAX(snap.CapturedAtUtc) AS LatestSnapshotUtc,
    MAX(metric.ComputedAtUtc) AS LatestMetricUtc
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
LEFT JOIN ExternalProductMappings AS epm ON epm.CatalogProductId = p.Id
LEFT JOIN CatalogPriceReferenceSnapshots AS ref ON ref.CatalogProductId = p.Id
LEFT JOIN CatalogMarketplaceListings AS l ON l.CatalogProductId = p.Id AND l.IsActive = 1
LEFT JOIN CatalogMarketplaceSales AS sale ON sale.CatalogProductId = p.Id
LEFT JOIN CatalogMarketPriceSnapshots AS snap ON snap.CatalogProductId = p.Id
LEFT JOIN CatalogMarketMetrics AS metric ON metric.CatalogProductId = p.Id
WHERE p.IsActive = 1
GROUP BY p.Id, g.Name, s.Name, s.Code, p.CardNumber, p.Name, p.IsSingleCard;
GO

CREATE OR ALTER VIEW dbo.vw_P2W_LatestMarketSnapshot
AS
WITH Ranked AS
(
    SELECT
        snap.*,
        ROW_NUMBER() OVER
        (
            PARTITION BY snap.CatalogProductId, snap.ProductVariantId, snap.SourceName, snap.Condition, snap.Currency
            ORDER BY snap.CapturedAtUtc DESC
        ) AS rn
    FROM CatalogMarketPriceSnapshots AS snap
)
SELECT
    r.Id AS SnapshotId,
    r.CatalogProductId,
    r.ProductVariantId,
    r.MarketplaceSourceId,
    r.SourceName,
    r.Condition,
    r.Currency,
    r.LowestListingPrice,
    r.MedianListingPrice,
    r.AverageListingPrice,
    r.HighestListingPrice,
    r.LastSoldPrice,
    r.MedianSoldPrice,
    r.AverageSoldPrice,
    r.LowestSoldPrice,
    r.HighestSoldPrice,
    r.ReferenceMarketPrice,
    r.ReferenceLowPrice,
    r.ReferenceMidPrice,
    r.ReferenceHighPrice,
    r.ListingCount,
    r.SoldCount,
    r.SalesVolume,
    r.CapturedAtUtc
FROM Ranked AS r
WHERE r.rn = 1;
GO

CREATE OR ALTER VIEW dbo.vw_P2W_LatestMarketMetric
AS
WITH Ranked AS
(
    SELECT
        metric.*,
        ROW_NUMBER() OVER
        (
            PARTITION BY metric.CatalogProductId, metric.ProductVariantId, metric.WindowName, metric.Condition, metric.Currency
            ORDER BY metric.ComputedAtUtc DESC
        ) AS rn
    FROM CatalogMarketMetrics AS metric
)
SELECT
    r.Id AS MarketMetricId,
    r.CatalogProductId,
    r.ProductVariantId,
    r.Condition,
    r.Currency,
    r.WindowName,
    r.WindowStartUtc,
    r.WindowEndUtc,
    r.CurrentMarketPrice,
    r.PreviousMarketPrice,
    r.PriceChangeAmount,
    r.PriceChangePercent,
    r.LowPrice,
    r.HighPrice,
    r.ListingCount,
    r.SoldCount,
    r.SalesVolume,
    r.TotalSoldValue,
    r.AverageSoldValue,
    r.VolumeScore,
    r.TrendScore,
    r.VolatilityScore,
    r.LiquidityScore,
    r.SpreadScore,
    r.DealScore,
    r.OpportunityScore,
    r.ConfidenceScore,
    r.EstimatedFeesPercent,
    r.EstimatedShippingCost,
    r.EstimatedGrossMargin,
    r.EstimatedNetMargin,
    r.EstimatedRoiPercent,
    r.ComputedAtUtc
FROM Ranked AS r
WHERE r.rn = 1;
GO
