/*
P2W Cards - Optional Diagnostic Stored Procedures

Run this file in SSMS if you want repeatable parameterized audits.
These procedures are read-only.
*/

CREATE OR ALTER PROCEDURE dbo.usp_P2W_CatalogSetAudit
    @GameSlug nvarchar(140),
    @SetCode nvarchar(450) = NULL,
    @SetName nvarchar(180) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.Name AS Game,
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
        SUM(CASE WHEN m.Id IS NULL AND p.Id IS NOT NULL THEN 1 ELSE 0 END) AS ProductsWithoutMappings
    FROM CardSets AS s
    JOIN Games AS g ON g.Id = s.GameId
    LEFT JOIN CatalogProducts AS p ON p.CardSetId = s.Id AND p.IsActive = 1
    LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
    WHERE g.Slug = @GameSlug
      AND (@SetCode IS NULL OR s.Code = @SetCode)
      AND (@SetName IS NULL OR s.Name LIKE '%' + @SetName + '%')
    GROUP BY g.Name, s.Id, s.Name, s.Code, s.ReleaseDate, s.IsUpcoming
    ORDER BY s.ReleaseDate DESC, s.Name;

    SELECT
        p.Id AS CatalogProductId,
        s.Code AS SetCode,
        p.CardNumber,
        p.Name,
        p.Rarity,
        p.ProductType,
        p.IsSingleCard,
        p.IsSealed,
        COUNT(DISTINCT v.Id) AS VariantCount,
        COUNT(DISTINCT m.Id) AS MappingCount,
        COUNT(DISTINCT l.Id) AS ActiveListingCount,
        COUNT(DISTINCT sale.Id) AS SoldCompCount
    FROM CatalogProducts AS p
    JOIN Games AS g ON g.Id = p.GameId
    LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
    LEFT JOIN ProductVariants AS v ON v.CatalogProductId = p.Id
    LEFT JOIN ExternalProductMappings AS m ON m.CatalogProductId = p.Id
    LEFT JOIN CatalogMarketplaceListings AS l ON l.CatalogProductId = p.Id AND l.IsActive = 1
    LEFT JOIN CatalogMarketplaceSales AS sale ON sale.CatalogProductId = p.Id
    WHERE g.Slug = @GameSlug
      AND (@SetCode IS NULL OR s.Code = @SetCode)
      AND (@SetName IS NULL OR s.Name LIKE '%' + @SetName + '%')
      AND p.IsActive = 1
    GROUP BY p.Id, s.Code, p.CardNumber, p.Name, p.Rarity, p.ProductType, p.IsSingleCard, p.IsSealed
    ORDER BY
        TRY_CONVERT(int, LEFT(ISNULL(p.CardNumber, ''), PATINDEX('%[^0-9]%', ISNULL(p.CardNumber, '') + 'x') - 1)),
        p.CardNumber,
        p.Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_P2W_ProductMarketAudit
    @CatalogProductId uniqueidentifier = NULL,
    @ProductName nvarchar(240) = NULL,
    @GameSlug nvarchar(140) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (100)
        p.Id AS CatalogProductId,
        g.Name AS Game,
        s.Name AS SetName,
        s.Code AS SetCode,
        p.CardNumber,
        p.Name,
        p.Rarity,
        p.ProductType,
        p.ImageUrl,
        p.UpdatedUtc
    FROM CatalogProducts AS p
    JOIN Games AS g ON g.Id = p.GameId
    LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
    WHERE (@CatalogProductId IS NULL OR p.Id = @CatalogProductId)
      AND (@ProductName IS NULL OR p.Name LIKE '%' + @ProductName + '%')
      AND (@GameSlug IS NULL OR g.Slug = @GameSlug)
    ORDER BY g.Name, s.ReleaseDate DESC, p.Name;

    SELECT TOP (100)
        ref.SourceName,
        ref.MarketPrice,
        ref.LowPrice,
        ref.MidPrice,
        ref.HighPrice,
        ref.UngradedPrice,
        ref.Currency,
        ref.CapturedAtUtc
    FROM CatalogPriceReferenceSnapshots AS ref
    JOIN CatalogProducts AS p ON p.Id = ref.CatalogProductId
    JOIN Games AS g ON g.Id = p.GameId
    WHERE (@CatalogProductId IS NULL OR p.Id = @CatalogProductId)
      AND (@ProductName IS NULL OR p.Name LIKE '%' + @ProductName + '%')
      AND (@GameSlug IS NULL OR g.Slug = @GameSlug)
    ORDER BY ref.CapturedAtUtc DESC;

    SELECT TOP (100)
        l.SourceName,
        l.Title,
        l.Condition,
        l.Price,
        l.ShippingPrice,
        l.EffectivePrice,
        l.MatchConfidence,
        l.MatchStatus,
        l.IsExcludedFromMarketValue,
        l.ExclusionReason,
        l.LastSeenUtc,
        l.ListingUrl
    FROM CatalogMarketplaceListings AS l
    JOIN CatalogProducts AS p ON p.Id = l.CatalogProductId
    JOIN Games AS g ON g.Id = p.GameId
    WHERE l.IsActive = 1
      AND (@CatalogProductId IS NULL OR p.Id = @CatalogProductId)
      AND (@ProductName IS NULL OR p.Name LIKE '%' + @ProductName + '%')
      AND (@GameSlug IS NULL OR g.Slug = @GameSlug)
    ORDER BY l.LastSeenUtc DESC, l.EffectivePrice ASC;

    SELECT TOP (100)
        sale.SourceName,
        sale.Title,
        sale.Condition,
        sale.SoldPrice,
        sale.ShippingPrice,
        sale.EffectiveSoldPrice,
        sale.MatchConfidence,
        sale.MatchStatus,
        sale.IsExcludedFromMarketValue,
        sale.ExclusionReason,
        sale.SoldAtUtc
    FROM CatalogMarketplaceSales AS sale
    JOIN CatalogProducts AS p ON p.Id = sale.CatalogProductId
    JOIN Games AS g ON g.Id = p.GameId
    WHERE (@CatalogProductId IS NULL OR p.Id = @CatalogProductId)
      AND (@ProductName IS NULL OR p.Name LIKE '%' + @ProductName + '%')
      AND (@GameSlug IS NULL OR g.Slug = @GameSlug)
    ORDER BY sale.SoldAtUtc DESC;

    SELECT TOP (20)
        metric.WindowName,
        metric.Condition,
        metric.CurrentMarketPrice,
        metric.PreviousMarketPrice,
        metric.PriceChangePercent,
        metric.ListingCount,
        metric.SoldCount,
        metric.SalesVolume,
        metric.ConfidenceScore,
        metric.EstimatedNetMargin,
        metric.EstimatedRoiPercent,
        metric.ComputedAtUtc
    FROM CatalogMarketMetrics AS metric
    JOIN CatalogProducts AS p ON p.Id = metric.CatalogProductId
    JOIN Games AS g ON g.Id = p.GameId
    WHERE (@CatalogProductId IS NULL OR p.Id = @CatalogProductId)
      AND (@ProductName IS NULL OR p.Name LIKE '%' + @ProductName + '%')
      AND (@GameSlug IS NULL OR g.Slug = @GameSlug)
    ORDER BY metric.ComputedAtUtc DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_P2W_ImportRunAudit
    @Take int = 50,
    @SourceName nvarchar(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take)
        r.SourceName,
        r.ImportType,
        r.Status,
        r.StartedUtc,
        r.FinishedUtc,
        DATEDIFF(second, r.StartedUtc, ISNULL(r.FinishedUtc, SYSUTCDATETIME())) AS DurationSeconds,
        r.RecordsProcessed,
        r.RecordsCreated,
        r.RecordsUpdated,
        r.RecordsSkipped,
        r.ErrorCount,
        r.Notes
    FROM CatalogImportRuns AS r
    WHERE (@SourceName IS NULL OR r.SourceName = @SourceName)
    ORDER BY r.StartedUtc DESC;

    SELECT TOP (@Take)
        e.CreatedUtc,
        e.SourceName,
        r.ImportType,
        e.ExternalId,
        e.ErrorMessage,
        LEFT(e.RawSourceJson, 500) AS RawSourcePreview
    FROM CatalogImportErrors AS e
    LEFT JOIN CatalogImportRuns AS r ON r.Id = e.CatalogImportRunId
    WHERE (@SourceName IS NULL OR e.SourceName = @SourceName)
    ORDER BY e.CreatedUtc DESC;

    SELECT
        SourceName,
        ImportType,
        CheckpointValue,
        UpdatedUtc
    FROM CatalogImportCheckpoints
    WHERE (@SourceName IS NULL OR SourceName = @SourceName)
    ORDER BY SourceName, ImportType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_P2W_TableRowCounts
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.name AS SchemaName,
        t.name AS TableName,
        SUM(p.rows) AS RowCount
    FROM sys.tables AS t
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
    GROUP BY s.name, t.name
    ORDER BY RowCount DESC, t.name;
END;
GO
