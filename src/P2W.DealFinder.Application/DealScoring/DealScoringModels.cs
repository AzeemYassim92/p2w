using P2W.DealFinder.Domain.Shared;

namespace P2W.DealFinder.Application.DealScoring;

public sealed record FeeAssumptions(
    decimal MarketplaceFeePercent = 0.1325m,
    decimal PaymentFeePercent = 0.00m,
    decimal FixedSaleFee = 0.30m,
    decimal OutboundShippingCost = 4.50m,
    decimal PackingCost = 0.75m,
    decimal MiscBufferPercent = 0.05m);

public sealed record DealSearchThresholds(
    decimal MinimumBuyPrice = 25m,
    decimal MaximumBuyPrice = 250m,
    decimal MinimumNetMarginPercent = 0.10m,
    decimal MinimumRoiPercent = 0.10m,
    decimal MinimumNetProfit = 10m,
    decimal MinimumIdentityConfidence = 0.90m,
    decimal MinimumMarketConfidence = 0.65m,
    decimal MinimumLiquidityScore = 0.50m,
    bool RequireSoldCompsForAction = true);

public sealed record DealScoringInput(
    Guid ProductId,
    string ProductName,
    decimal ListingPrice,
    decimal InboundShippingPrice,
    decimal ExpectedMarketValue,
    MarketValueBasis MarketValueBasis,
    decimal IdentityConfidence,
    decimal MarketConfidence,
    decimal LiquidityScore,
    int ActiveListingCount,
    int SoldCompCount,
    FeeAssumptions Fees,
    DealSearchThresholds Thresholds,
    DateTime? EvidenceCapturedAtUtc = null,
    bool IsExcluded = false,
    string? ExclusionReason = null);

public sealed record DealScoreResult(
    bool IsActionable,
    decimal SortScore,
    decimal ListingPrice,
    decimal InboundShippingPrice,
    decimal EffectiveBuyPrice,
    decimal EstimatedSaleFees,
    decimal EstimatedTotalCost,
    decimal NetProfit,
    decimal NetMarginPercent,
    decimal RoiPercent,
    decimal AdjustedMarketConfidence,
    IReadOnlyList<string> HardFilterFailures,
    IReadOnlyList<string> RiskFlags)
{
    public string Summary => IsActionable
        ? $"Actionable: {NetProfit:C} net, {NetMarginPercent:P1} margin, {RoiPercent:P1} ROI, sort {SortScore:N1}."
        : $"Not actionable: {string.Join("; ", HardFilterFailures)}";
}
