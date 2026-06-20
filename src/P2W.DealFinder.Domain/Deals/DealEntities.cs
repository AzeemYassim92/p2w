using P2W.DealFinder.Domain.Shared;

namespace P2W.DealFinder.Domain.Deals;

public sealed class FeeProfile : AuditedEntity
{
    public string Name { get; set; } = "Default";
    public decimal MarketplaceFeePercent { get; set; } = 0.1325m;
    public decimal PaymentFeePercent { get; set; } = 0.00m;
    public decimal FixedSaleFee { get; set; } = 0.30m;
    public decimal OutboundShippingCost { get; set; } = 4.50m;
    public decimal PackingCost { get; set; } = 0.75m;
    public decimal MiscBufferPercent { get; set; } = 0.05m;
    public string Currency { get; set; } = "USD";
    public bool IsDefault { get; set; }
}

public sealed class DealSearchProfile : AuditedEntity
{
    public string Name { get; set; } = "Default Pokemon Singles";
    public string? GameOrBrand { get; set; } = "Pokemon";
    public decimal MinimumBuyPrice { get; set; } = 25m;
    public decimal MaximumBuyPrice { get; set; } = 250m;
    public decimal MinimumNetMarginPercent { get; set; } = 0.10m;
    public decimal MinimumRoiPercent { get; set; } = 0.10m;
    public decimal MinimumNetProfit { get; set; } = 10m;
    public decimal MinimumIdentityConfidence { get; set; } = 0.90m;
    public decimal MinimumMarketConfidence { get; set; } = 0.65m;
    public decimal MinimumLiquidityScore { get; set; } = 0.50m;
    public bool RequireSoldCompsForAction { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class DealCandidate : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid? ActiveListingId { get; set; }
    public Guid DealSearchProfileId { get; set; }
    public string SourceName { get; set; } = "";
    public string ListingTitle { get; set; } = "";
    public string ListingUrl { get; set; } = "";
    public decimal ListingPrice { get; set; }
    public decimal InboundShippingPrice { get; set; }
    public decimal EffectiveBuyPrice { get; set; }
    public decimal? ExpectedMarketValue { get; set; }
    public MarketValueBasis MarketValueBasis { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public decimal EstimatedNetProfit { get; set; }
    public decimal EstimatedNetMarginPercent { get; set; }
    public decimal EstimatedRoiPercent { get; set; }
    public decimal IdentityConfidence { get; set; }
    public decimal MarketConfidence { get; set; }
    public decimal LiquidityScore { get; set; }
    public DealCandidateStatus Status { get; set; } = DealCandidateStatus.New;
    public string Explanation { get; set; } = "";
    public string? RiskFlags { get; set; }
    public DateTime ScoredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class DealDecisionHistory : AuditedEntity
{
    public Guid DealCandidateId { get; set; }
    public DealDecision Decision { get; set; }
    public string? Notes { get; set; }
    public decimal? ManualOfferPrice { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
}
