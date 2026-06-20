using P2W.DealFinder.Domain.Shared;

namespace P2W.DealFinder.Domain.MarketEvidence;

public sealed class MarketplaceSource : AuditedEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SupportsActiveListings { get; set; }
    public bool SupportsSoldComps { get; set; }
    public bool SupportsReferencePrices { get; set; }
    public bool SupportsBulkImport { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public int PriorityRank { get; set; }
}

public sealed class ProviderObservation : AuditedEntity
{
    public Guid? CatalogProductId { get; set; }
    public string SourceName { get; set; } = "";
    public string WorkloadType { get; set; } = "";
    public ObservationStatus Status { get; set; }
    public int RecordsReturned { get; set; }
    public int RecordsAccepted { get; set; }
    public int RecordsRejected { get; set; }
    public string? QueryText { get; set; }
    public string? NoDataReason { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DiagnosticsJson { get; set; }
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedUtc { get; set; }
}

public sealed class ActiveListing : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalListingId { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal ListingPrice { get; set; }
    public decimal InboundShippingPrice { get; set; }
    public decimal EffectiveBuyPrice => ListingPrice + InboundShippingPrice;
    public string Currency { get; set; } = "USD";
    public ProductCondition Condition { get; set; } = ProductCondition.Unknown;
    public int? Quantity { get; set; }
    public string? SellerName { get; set; }
    public decimal? SellerFeedbackScore { get; set; }
    public decimal? SellerFeedbackPercent { get; set; }
    public string ListingUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal IdentityConfidence { get; set; }
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SoldComp : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalSaleId { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal SoldPrice { get; set; }
    public decimal BuyerShippingPrice { get; set; }
    public decimal EffectiveSoldPrice => SoldPrice + BuyerShippingPrice;
    public string Currency { get; set; } = "USD";
    public ProductCondition Condition { get; set; } = ProductCondition.Unknown;
    public DateTime SoldAtUtc { get; set; }
    public decimal IdentityConfidence { get; set; }
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }
}

public sealed class ReferencePrice : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string SourceName { get; set; } = "";
    public decimal? MarketPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MarketSnapshot : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductCondition Condition { get; set; } = ProductCondition.Unknown;
    public string Currency { get; set; } = "USD";
    public MarketValueBasis MarketValueBasis { get; set; } = MarketValueBasis.Unknown;
    public decimal? ExpectedMarketValue { get; set; }
    public decimal? LowestListingPrice { get; set; }
    public decimal? MedianListingPrice { get; set; }
    public decimal? MedianSoldPrice { get; set; }
    public int ActiveListingCount { get; set; }
    public int SoldCompCount { get; set; }
    public decimal MarketConfidence { get; set; }
    public decimal LiquidityScore { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}
