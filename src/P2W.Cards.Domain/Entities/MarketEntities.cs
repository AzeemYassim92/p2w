namespace P2W.Cards.Domain.Entities;

public sealed class MarketplaceSource
{
    public Guid Id { get; set; }
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
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class ExternalMarketplaceSkuMapping
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Guid MarketplaceSourceId { get; set; }
    public MarketplaceSource? MarketplaceSource { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalSku { get; set; } = "";
    public string? ExternalProductId { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ExternalTitle { get; set; }
    public string? ExternalCategory { get; set; }
    public decimal? MatchConfidence { get; set; }
    public string MappingStatus { get; set; } = "AutoMatched";
    public string? MappingNotes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastVerifiedUtc { get; set; }
}

public sealed class CatalogMarketplaceListing
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Guid MarketplaceSourceId { get; set; }
    public MarketplaceSource? MarketplaceSource { get; set; }
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
    public bool IsActive { get; set; }
    public decimal? MatchConfidence { get; set; }
    public string MatchStatus { get; set; } = "Matched";
    public bool IsExcludedFromMarketValue { get; set; }
    public string? ExclusionReason { get; set; }
    public string? RawSourceJson { get; set; }
}

public sealed class CatalogMarketplaceSale
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Guid MarketplaceSourceId { get; set; }
    public MarketplaceSource? MarketplaceSource { get; set; }
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

public sealed class CatalogMarketPriceSnapshot
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Guid? MarketplaceSourceId { get; set; }
    public MarketplaceSource? MarketplaceSource { get; set; }
    public string SourceName { get; set; } = "";
    public string Condition { get; set; } = "Unknown";
    public string Currency { get; set; } = "USD";
    public decimal? LowestListingPrice { get; set; }
    public decimal? MedianListingPrice { get; set; }
    public decimal? AverageListingPrice { get; set; }
    public decimal? HighestListingPrice { get; set; }
    public decimal? LastSoldPrice { get; set; }
    public decimal? MedianSoldPrice { get; set; }
    public decimal? AverageSoldPrice { get; set; }
    public decimal? LowestSoldPrice { get; set; }
    public decimal? HighestSoldPrice { get; set; }
    public decimal? ReferenceMarketPrice { get; set; }
    public decimal? ReferenceLowPrice { get; set; }
    public decimal? ReferenceMidPrice { get; set; }
    public decimal? ReferenceHighPrice { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? SalesVolume { get; set; }
    public DateTime CapturedAtUtc { get; set; }
}

public sealed class CatalogMarketMetric
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public string Condition { get; set; } = "Unknown";
    public string Currency { get; set; } = "USD";
    public string WindowName { get; set; } = "";
    public DateTime WindowStartUtc { get; set; }
    public DateTime WindowEndUtc { get; set; }
    public decimal? CurrentMarketPrice { get; set; }
    public decimal? PreviousMarketPrice { get; set; }
    public decimal? PriceChangeAmount { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public int ListingCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? SalesVolume { get; set; }
    public decimal? TotalSoldValue { get; set; }
    public decimal? AverageSoldValue { get; set; }
    public decimal? VolumeScore { get; set; }
    public decimal? TrendScore { get; set; }
    public decimal? VolatilityScore { get; set; }
    public decimal? LiquidityScore { get; set; }
    public decimal? SpreadScore { get; set; }
    public decimal? DealScore { get; set; }
    public decimal? OpportunityScore { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public decimal? EstimatedFeesPercent { get; set; }
    public decimal? EstimatedShippingCost { get; set; }
    public decimal? EstimatedGrossMargin { get; set; }
    public decimal? EstimatedNetMargin { get; set; }
    public decimal? EstimatedRoiPercent { get; set; }
    public DateTime ComputedAtUtc { get; set; }
}

public sealed class CatalogProviderIngestionRun
{
    public Guid Id { get; set; }
    public string SourceName { get; set; } = "";
    public string WorkloadType { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public string Status { get; set; } = "";
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int ErrorCount { get; set; }
    public string? CheckpointBefore { get; set; }
    public string? CheckpointAfter { get; set; }
    public string? Notes { get; set; }
    public ICollection<CatalogProviderIngestionError> Errors { get; set; } = new List<CatalogProviderIngestionError>();
}

public sealed class CatalogProviderIngestionError
{
    public Guid Id { get; set; }
    public Guid IngestionRunId { get; set; }
    public CatalogProviderIngestionRun? IngestionRun { get; set; }
    public string SourceName { get; set; } = "";
    public string WorkloadType { get; set; } = "";
    public string? ExternalId { get; set; }
    public Guid? CatalogProductId { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string? RawSourceJson { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class CatalogAggregationCheckpoint
{
    public Guid Id { get; set; }
    public string SourceName { get; set; } = "";
    public string WorkloadType { get; set; } = "";
    public string CheckpointValue { get; set; } = "";
    public DateTime UpdatedUtc { get; set; }
}

public sealed class ProductMarketViewEvent
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = "";
    public string? SourceName { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class CatalogWatchlistItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? TargetDiscountPercent { get; set; }
    public bool AlertOnVolumeSpike { get; set; }
    public bool AlertOnPriceDrop { get; set; }
    public bool AlertOnNewDeal { get; set; }
    public bool AlertOnDataRefresh { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
}
