/*
    Pokemon master catalog exploration queries

    Run after:
      dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv
*/

USE [P2WDealFinderDb];
GO

/* 1. Latest catalog imports */
SELECT TOP (20)
    Id,
    SourceFilePath,
    Status,
    StartedUtc,
    FinishedUtc,
    CsvRowsRead,
    RowsImported,
    Notes
FROM dbo.PokemonMasterCatalogImports
ORDER BY StartedUtc DESC;

/* 2. Catalog row totals by family/language */
SELECT
    ProductFamily,
    Language,
    IsLikelyPokemonTcg,
    COUNT(*) AS Rows,
    SUM(CASE WHEN Psa10Price IS NOT NULL THEN 1 ELSE 0 END) AS RowsWithPsa10,
    SUM(CASE WHEN SalesVolumeYearly IS NOT NULL THEN 1 ELSE 0 END) AS RowsWithVolume
FROM dbo.PokemonMasterCatalog
GROUP BY ProductFamily, Language, IsLikelyPokemonTcg
ORDER BY Rows DESC;

/* 3. Release-date coverage and impossible/null date review */
SELECT
    COUNT(*) AS Rows,
    SUM(CASE WHEN ReleaseDate IS NULL THEN 1 ELSE 0 END) AS MissingOrNormalizedReleaseDate,
    MIN(ReleaseDate) AS EarliestReleaseDate,
    MAX(ReleaseDate) AS LatestReleaseDate
FROM dbo.PokemonMasterCatalog;

SELECT TOP (200)
    PriceChartingProductId,
    PriceChartingProductName,
    PriceChartingConsoleName,
    ProductFamily,
    ReleaseDate,
    Psa10Price,
    SalesVolumeYearly
FROM dbo.PokemonMasterCatalog
WHERE ReleaseDate IS NULL
ORDER BY PriceChartingConsoleName, PriceChartingProductName;

/* 4. Master list ordered by release date */
SELECT TOP (500)
    ReleaseDate,
    ProductFamily,
    SetName,
    CardNumber,
    CardName,
    VariantName,
    PriceChartingProductId,
    TcgPlayerId,
    Psa10Price,
    SalesVolumeYearly,
    PriceChartingProductUrl
FROM dbo.PokemonMasterCatalog
WHERE Language = 'English'
ORDER BY
    CASE WHEN ReleaseDate IS NULL THEN 1 ELSE 0 END,
    ReleaseDate,
    SetName,
    TRY_CONVERT(int, CardNumber),
    CardNumber,
    CardName,
    VariantName;

/* 5. Core deal-finder pool: English likely-TCG PSA 10 cards in the target range */
SELECT TOP (500)
    ReleaseDate,
    SetName,
    CardNumber,
    CardName,
    VariantName,
    PriceChartingProductId,
    Psa10Price,
    UngradedPrice,
    Grade9Price,
    SalesVolumeYearly,
    EstimatedThirtyDayVolume,
    EstimatedNinetyDayVolume,
    EbayPsa10SearchQuery
FROM dbo.PokemonMasterCatalog
WHERE Language = 'English'
  AND IsLikelyPokemonTcg = 1
  AND Psa10Price BETWEEN 25 AND 250
ORDER BY SalesVolumeYearly DESC, Psa10Price ASC;

/* 6. High-volume PSA 10 cards regardless of price */
SELECT TOP (250)
    SetName,
    CardNumber,
    CardName,
    VariantName,
    Psa10Price,
    SalesVolumeYearly,
    EstimatedThirtyDayVolume,
    EstimatedNinetyDayVolume,
    PriceChartingProductUrl
FROM dbo.PokemonMasterCatalog
WHERE Language = 'English'
  AND IsLikelyPokemonTcg = 1
  AND Psa10Price IS NOT NULL
ORDER BY SalesVolumeYearly DESC, Psa10Price ASC;

/* 7. Variant review: cards with multiple product rows */
SELECT TOP (200)
    SetName,
    CardName,
    CardNumber,
    COUNT(*) AS VariantRows,
    STRING_AGG(COALESCE(NULLIF(VariantName, ''), 'Base'), ' | ') AS Variants
FROM dbo.PokemonMasterCatalog
WHERE Language = 'English'
GROUP BY SetName, CardName, CardNumber
HAVING COUNT(*) > 1
ORDER BY VariantRows DESC, SetName, CardName;

/* 8. Provider linking fields coverage */
SELECT
    COUNT(*) AS Rows,
    SUM(CASE WHEN PriceChartingProductId IS NOT NULL AND PriceChartingProductId <> '' THEN 1 ELSE 0 END) AS HasPriceChartingId,
    SUM(CASE WHEN TcgPlayerId IS NOT NULL AND TcgPlayerId <> '' THEN 1 ELSE 0 END) AS HasTcgPlayerId,
    SUM(CASE WHEN Upc IS NOT NULL AND Upc <> '' THEN 1 ELSE 0 END) AS HasUpc,
    SUM(CASE WHEN PriceChartingProductUrl IS NOT NULL AND PriceChartingProductUrl <> '' THEN 1 ELSE 0 END) AS HasPriceChartingUrl,
    SUM(CASE WHEN EbayPsa10SearchQuery IS NOT NULL AND EbayPsa10SearchQuery <> '' THEN 1 ELSE 0 END) AS HasEbaySearchQuery
FROM dbo.PokemonMasterCatalog;

/* 9. Review likely false TCG positives and classifier misses */
SELECT TOP (300)
    ProductFamily,
    IsLikelyPokemonTcg,
    SetName,
    CardName,
    CardNumber,
    VariantName,
    PriceChartingProductId,
    PriceChartingProductName,
    PriceChartingConsoleName
FROM dbo.PokemonMasterCatalog
WHERE Language = 'English'
  AND (
      SetName LIKE '%Topps%'
      OR SetName LIKE '%KFC%'
      OR SetName LIKE '%Burger%'
      OR SetName LIKE '%Artbox%'
      OR SetName LIKE '%Sealdass%'
      OR SetName LIKE '%Sticker%'
      OR PriceChartingProductName LIKE '%Sticker%'
  )
ORDER BY ProductFamily, SetName, CardName;
