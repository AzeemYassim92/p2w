namespace P2W.Cards.Application.DTOs;

public sealed class MarketplaceSourceDto
{
    public Guid MarketplaceSourceId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public bool SupportsListings { get; set; }
    public bool SupportsSoldComps { get; set; }
    public bool SupportsBuylist { get; set; }
    public bool SupportsReferencePrices { get; set; }
    public bool SupportsBulkCsv { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public int PriorityRank { get; set; }
}

public sealed class ExternalMarketplaceSkuMappingDto
{
    public Guid MappingId { get; set; }
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid MarketplaceSourceId { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalSku { get; set; } = "";
    public string? ExternalProductId { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ExternalTitle { get; set; }
    public string? ExternalCategory { get; set; }
    public decimal? MatchConfidence { get; set; }
    public string MappingStatus { get; set; } = "";
    public string? MappingNotes { get; set; }
}

public sealed class ExternalMarketplaceListingDto
{
    public string SourceName { get; set; } = "";
    public string ExternalListingId { get; set; } = "";
    public string? ExternalSku { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? ShippingPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Condition { get; set; } = "Unknown";
    public string? RawCondition { get; set; }
    public int? Quantity { get; set; }
    public string? SellerName { get; set; }
    public decimal? SellerFeedbackScore { get; set; }
    public decimal? SellerFeedbackPercentage { get; set; }
    public string? SellerLocation { get; set; }
    public string ListingUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsAuction { get; set; }
    public DateTime? AuctionEndsUtc { get; set; }
    public DateTime? ListedAtUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public decimal? MatchConfidence { get; set; }
    public string MatchStatus { get; set; } = "Matched";
    public bool IsExcludedFromMarketValue { get; set; }
    public string? ExclusionReason { get; set; }
    public string? RawSourceJson { get; set; }
}

public sealed class ExternalMarketplaceSaleDto
{
    public string SourceName { get; set; } = "";
    public string ExternalSaleId { get; set; } = "";
    public string? ExternalListingId { get; set; }
    public string? ExternalSku { get; set; }
    public string Title { get; set; } = "";
    public decimal SoldPrice { get; set; }
    public decimal? ShippingPrice { get; set; }
    public decimal EffectiveSoldPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Condition { get; set; } = "Unknown";
    public string? RawCondition { get; set; }
    public int? Quantity { get; set; }
    public string? SellerName { get; set; }
    public DateTime SoldAtUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public string? SaleUrl { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? MatchConfidence { get; set; }
    public string MatchStatus { get; set; } = "Matched";
    public bool IsExcludedFromMarketValue { get; set; }
    public string? ExclusionReason { get; set; }
    public string? RawSourceJson { get; set; }
}

public sealed class ExternalReferencePriceDto
{
    public string SourceName { get; set; } = "";
    public Guid? MarketplaceSourceId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string Condition { get; set; } = "NearMint";
    public decimal? MarketPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? MidPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? UngradedPrice { get; set; }
    public decimal? Grade7Price { get; set; }
    public decimal? Grade8Price { get; set; }
    public decimal? Grade9Price { get; set; }
    public decimal? Grade10Price { get; set; }
    public decimal? BuylistPrice { get; set; }
    public decimal? RetailPrice { get; set; }
    public decimal? SalesVolume { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CapturedAtUtc { get; set; }
    public string? ExternalUrl { get; set; }
    public string? RawSourceJson { get; set; }
}

public sealed class MarketplaceSearchContext
{
    public string GameName { get; set; } = "";
    public string? GameSlug { get; set; }
    public string? SetName { get; set; }
    public string? SetCode { get; set; }
    public string? CardNumber { get; set; }
    public string ProductType { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public string Condition { get; set; } = "NearMint";
    public string Currency { get; set; } = "USD";
    public bool IncludeLowConfidence { get; set; }
}

public sealed class MarketRefreshRequest
{
    public bool Force { get; set; }
    public bool UseMockData { get; set; }
    public string Condition { get; set; } = "NearMint";
    public string Currency { get; set; } = "USD";
    public int MaxProducts { get; set; } = 50;
    public IReadOnlyList<string> SourceNames { get; set; } = Array.Empty<string>();
}

public sealed class MarketAggregationResultDto
{
    public Guid? RunId { get; set; }
    public string Status { get; set; } = "";
    public int ProductsQueued { get; set; }
    public int ProductsProcessed { get; set; }
    public int ProductsSkipped { get; set; }
    public int ListingsCreated { get; set; }
    public int ListingsUpdated { get; set; }
    public int ReferencePricesCreated { get; set; }
    public int SnapshotsCreated { get; set; }
    public int MetricsComputed { get; set; }
    public int Errors { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<MarketDiagnosticEventDto> DiagnosticEvents { get; set; } = Array.Empty<MarketDiagnosticEventDto>();
}

public sealed class MarketDiagnosticEventDto
{
    public DateTime AtUtc { get; set; }
    public string Level { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Data { get; set; }
}

public sealed class ProductMarketSummaryDto
{
    public Guid CatalogProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string GameName { get; set; } = "";
    public string? SetName { get; set; }
    public string? CardNumber { get; set; }
    public string ImageUrl { get; set; } = "";
    public decimal? CurrentMarketPrice { get; set; }
    public decimal? PreviousMarketPrice { get; set; }
    public decimal? PriceChangeAmount { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? SalesVolume { get; set; }
    public decimal? EstimatedGrossMargin { get; set; }
    public decimal? EstimatedNetMargin { get; set; }
    public decimal? EstimatedRoiPercent { get; set; }
    public decimal? DealScore { get; set; }
    public decimal? OpportunityScore { get; set; }
    public string ConfidenceLabel { get; set; } = "";
    public decimal? ConfidenceScore { get; set; }
    public string FreshnessLabel { get; set; } = "";
    public DateTime? LastUpdatedUtc { get; set; }
    public int IncludedComparableCount { get; set; }
    public int ExcludedComparableCount { get; set; }
    public bool HasMarketData { get; set; }
    public string DataStatus { get; set; } = "";
    public string DataQualityMessage { get; set; } = "";
    public bool IsDemoData { get; set; }
}

public sealed class MarketplaceComparisonRequest
{
    public string Condition { get; set; } = "NearMint";
    public string Currency { get; set; } = "USD";
}

public sealed class MarketplaceComparisonDto
{
    public Guid CatalogProductId { get; set; }
    public IReadOnlyList<MarketplaceComparisonRowDto> Rows { get; set; } = Array.Empty<MarketplaceComparisonRowDto>();
}

public sealed class MarketplaceComparisonRowDto
{
    public string SourceName { get; set; } = "";
    public decimal? ReferenceMarketPrice { get; set; }
    public decimal? LowestActiveListing { get; set; }
    public decimal? MedianActiveListing { get; set; }
    public decimal? LastSoldPrice { get; set; }
    public decimal? SalesVolume { get; set; }
    public decimal? SpreadAmount { get; set; }
    public decimal? SpreadPercent { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public string FreshnessLabel { get; set; } = "";
    public string ConfidenceLabel { get; set; } = "";
    public string? ExternalUrl { get; set; }
    public bool IsDemoData { get; set; }
}

public sealed class MarketChartRequest
{
    public string Range { get; set; } = "1y";
    public string Condition { get; set; } = "NearMint";
    public string Currency { get; set; } = "USD";
}

public sealed class MarketChartDto
{
    public Guid CatalogProductId { get; set; }
    public IReadOnlyList<MarketChartPointDto> PriceSeries { get; set; } = Array.Empty<MarketChartPointDto>();
    public IReadOnlyList<MarketVolumePointDto> VolumeSeries { get; set; } = Array.Empty<MarketVolumePointDto>();
    public IReadOnlyList<MarketDistributionBucketDto> DistributionBuckets { get; set; } = Array.Empty<MarketDistributionBucketDto>();
    public IReadOnlyList<MarketPercentileMarkerDto> PercentileMarkers { get; set; } = Array.Empty<MarketPercentileMarkerDto>();
    public bool IsDemoData { get; set; }
}

public sealed class MarketChartPointDto
{
    public DateTime DateUtc { get; set; }
    public decimal? ReferencePrice { get; set; }
    public decimal? MedianActiveListing { get; set; }
    public decimal? LowestActiveListing { get; set; }
    public decimal? MedianSoldPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
}

public sealed class MarketVolumePointDto
{
    public DateTime DateUtc { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? SalesVolume { get; set; }
}

public sealed class MarketDistributionBucketDto
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int Count { get; set; }
}

public sealed class MarketPercentileMarkerDto
{
    public int Percentile { get; set; }
    public decimal Price { get; set; }
}

public sealed class CatalogMarketMetricDto
{
    public Guid CatalogMarketMetricId { get; set; }
    public Guid CatalogProductId { get; set; }
    public string Condition { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public string WindowName { get; set; } = "";
    public decimal? CurrentMarketPrice { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? VolumeScore { get; set; }
    public decimal? TrendScore { get; set; }
    public decimal? VolatilityScore { get; set; }
    public decimal? LiquidityScore { get; set; }
    public decimal? SpreadScore { get; set; }
    public decimal? DealScore { get; set; }
    public decimal? OpportunityScore { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime ComputedAtUtc { get; set; }
}

public sealed class DealOpportunityDto
{
    public Guid? ListingId { get; set; }
    public Guid CatalogProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string GameName { get; set; } = "";
    public string? SetName { get; set; }
    public Guid? CardSetId { get; set; }
    public string? CardNumber { get; set; }
    public string SourceName { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal? ItemPrice { get; set; }
    public decimal? ShippingPrice { get; set; }
    public decimal ListingPrice { get; set; }
    public decimal? TrustedMarketPrice { get; set; }
    public decimal? ExpectedMarketValue { get; set; }
    public decimal? EstimatedFees { get; set; }
    public decimal? EstimatedNetProfit { get; set; }
    public decimal? EstimatedRoiPercent { get; set; }
    public decimal? LiquidityScore { get; set; }
    public decimal? MatchConfidence { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DealScore { get; set; }
    public string DealLabel { get; set; } = "";
    public string Reason { get; set; } = "";
    public string ListingUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsDemoData { get; set; }
}

public sealed class DealScoreDto
{
    public decimal Score { get; set; }
    public string Label { get; set; } = "";
    public decimal? DiscountPercent { get; set; }
    public string? Notes { get; set; }
}

public sealed class DealScanRequest
{
    public string? GameSlug { get; set; }
    public Guid? CardSetId { get; set; }
    public Guid? CatalogProductId { get; set; }
    public string? Query { get; set; }
    public int Take { get; set; } = 50;
    public decimal ThresholdPercent { get; set; } = 15m;
    public decimal? MinMarketValue { get; set; }
    public decimal? MaxListingPrice { get; set; }
    public decimal? MinRoiPercent { get; set; }
    public decimal? MinConfidence { get; set; }
}

public sealed class MarketConfidenceDto
{
    public string Label { get; set; } = "";
    public decimal Score { get; set; }
    public int ActiveListingCount { get; set; }
    public int ReferenceSourceCount { get; set; }
    public int SoldCompCount { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class ProductFreshnessDto
{
    public string Label { get; set; } = "";
    public DateTime? LastUpdatedUtc { get; set; }
    public int AgeHours { get; set; }
}

public sealed class WatchlistIntelligenceDto
{
    public Guid WatchlistItemId { get; set; }
    public Guid CatalogProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string SignalLabel { get; set; } = "";
    public string SignalDetail { get; set; } = "";
    public decimal? CurrentMarketPrice { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? OpportunityScore { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class SetMarketDashboardDto
{
    public Guid CardSetId { get; set; }
    public string SetName { get; set; } = "";
    public string GameName { get; set; } = "";
    public IReadOnlyList<SetMarketDashboardProductDto> Products { get; set; } = Array.Empty<SetMarketDashboardProductDto>();
    public IReadOnlyList<SetMarketDashboardProductDto> TopMovers { get; set; } = Array.Empty<SetMarketDashboardProductDto>();
    public IReadOnlyList<SetMarketDashboardProductDto> HighestVolume { get; set; } = Array.Empty<SetMarketDashboardProductDto>();
    public IReadOnlyList<DealOpportunityDto> BestDeals { get; set; } = Array.Empty<DealOpportunityDto>();
    public IReadOnlyList<SetMarketDashboardProductDto> HighestOpportunity { get; set; } = Array.Empty<SetMarketDashboardProductDto>();
    public IReadOnlyList<SetMarketDashboardProductDto> MostListed { get; set; } = Array.Empty<SetMarketDashboardProductDto>();
    public IReadOnlyList<SetMarketDashboardProductDto> LowestConfidence { get; set; } = Array.Empty<SetMarketDashboardProductDto>();
}

public sealed class SetMarketDashboardProductDto
{
    public Guid CatalogProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string GameName { get; set; } = "";
    public string? SetName { get; set; }
    public string? CardNumber { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? CurrentMarketPrice { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? OpportunityScore { get; set; }
    public decimal? RankingScore { get; set; }
    public string SignalLabel { get; set; } = "";
    public string SignalDetail { get; set; } = "";
    public string ConfidenceLabel { get; set; } = "";
    public bool HasMarketData { get; set; }
    public bool IsDemoData { get; set; }
}

public sealed class ComparableSalesQualityDto
{
    public int IncludedCount { get; set; }
    public int ExcludedCount { get; set; }
    public IReadOnlyList<string> ExclusionReasons { get; set; } = Array.Empty<string>();
    public decimal? AverageMatchConfidence { get; set; }
}

public sealed class ProviderHealthDto
{
    public string SourceName { get; set; } = "";
    public string ProviderType { get; set; } = "";
    public bool IsEnabled { get; set; }
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class CreateCatalogWatchlistItemRequest
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? TargetDiscountPercent { get; set; }
    public bool AlertOnVolumeSpike { get; set; } = true;
    public bool AlertOnPriceDrop { get; set; } = true;
    public bool AlertOnNewDeal { get; set; } = true;
    public bool AlertOnDataRefresh { get; set; }
    public string? Notes { get; set; }
}
