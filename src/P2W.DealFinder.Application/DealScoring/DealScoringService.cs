using P2W.DealFinder.Domain.Shared;

namespace P2W.DealFinder.Application.DealScoring;

public interface IDealScoringService
{
    DealScoreResult Score(DealScoringInput input);
}

public sealed class DealScoringService : IDealScoringService
{
    public DealScoreResult Score(DealScoringInput input)
    {
        var listingPrice = RoundMoney(input.ListingPrice);
        var inboundShipping = RoundMoney(input.InboundShippingPrice);
        var effectiveBuyPrice = RoundMoney(listingPrice + inboundShipping);

        if (input.ExpectedMarketValue <= 0m)
        {
            return new DealScoreResult(false, 0m, listingPrice, inboundShipping, effectiveBuyPrice, 0m, 0m, 0m, 0m, 0m, 0m, ["missing expected market value"], ["no market value basis"]);
        }

        var saleFeePercent = input.Fees.MarketplaceFeePercent + input.Fees.PaymentFeePercent;
        var estimatedSaleFees = input.ExpectedMarketValue * saleFeePercent + input.Fees.FixedSaleFee;
        var miscBuffer = input.ExpectedMarketValue * input.Fees.MiscBufferPercent;
        var estimatedTotalCost = effectiveBuyPrice + estimatedSaleFees + input.Fees.OutboundShippingCost + input.Fees.PackingCost + miscBuffer;
        var netProfit = input.ExpectedMarketValue - estimatedTotalCost;
        var netMarginPercent = SafeDivide(netProfit, input.ExpectedMarketValue);
        var roiPercent = SafeDivide(netProfit, effectiveBuyPrice);
        var adjustedMarketConfidence = AdjustMarketConfidence(input);

        var failures = new List<string>();
        var risks = new List<string>();

        if (input.IsExcluded)
        {
            failures.Add(string.IsNullOrWhiteSpace(input.ExclusionReason) ? "listing is excluded" : $"listing is excluded: {input.ExclusionReason}");
        }

        if (effectiveBuyPrice < input.Thresholds.MinimumBuyPrice)
        {
            failures.Add($"effective buy price {effectiveBuyPrice:C} is below profile minimum {input.Thresholds.MinimumBuyPrice:C}");
        }

        if (effectiveBuyPrice > input.Thresholds.MaximumBuyPrice)
        {
            failures.Add($"effective buy price {effectiveBuyPrice:C} is above profile maximum {input.Thresholds.MaximumBuyPrice:C}");
        }

        if (netProfit < input.Thresholds.MinimumNetProfit)
        {
            failures.Add($"net profit {netProfit:C} is below required {input.Thresholds.MinimumNetProfit:C}");
        }

        if (netMarginPercent < input.Thresholds.MinimumNetMarginPercent)
        {
            failures.Add($"net margin {netMarginPercent:P1} is below required {input.Thresholds.MinimumNetMarginPercent:P1}");
        }

        if (roiPercent < input.Thresholds.MinimumRoiPercent)
        {
            failures.Add($"ROI {roiPercent:P1} is below required {input.Thresholds.MinimumRoiPercent:P1}");
        }

        if (input.IdentityConfidence < input.Thresholds.MinimumIdentityConfidence)
        {
            failures.Add($"identity confidence {input.IdentityConfidence:P0} is below required {input.Thresholds.MinimumIdentityConfidence:P0}");
        }

        if (adjustedMarketConfidence < input.Thresholds.MinimumMarketConfidence)
        {
            failures.Add($"market confidence {adjustedMarketConfidence:P0} is below required {input.Thresholds.MinimumMarketConfidence:P0}");
        }

        if (input.LiquidityScore < input.Thresholds.MinimumLiquidityScore)
        {
            failures.Add($"liquidity score {input.LiquidityScore:P0} is below required {input.Thresholds.MinimumLiquidityScore:P0}");
        }

        if (input.Thresholds.RequireSoldCompsForAction && input.SoldCompCount <= 0)
        {
            failures.Add("sold comps are required before this candidate can be actionable");
        }

        if (input.SoldCompCount <= 0)
        {
            risks.Add("no sold comps captured");
        }

        if (input.MarketValueBasis == MarketValueBasis.ActiveListingMedian)
        {
            risks.Add("expected value is listing-based, not trade-backed");
        }

        if (input.MarketValueBasis == MarketValueBasis.ReferencePrice)
        {
            risks.Add("expected value is reference-price based");
        }

        if (input.ActiveListingCount < 3)
        {
            risks.Add("thin active-listing sample");
        }

        if (!input.EvidenceCapturedAtUtc.HasValue)
        {
            risks.Add("evidence freshness unknown");
        }

        var sortScore = CalculateSortScore(netProfit, roiPercent, input.LiquidityScore, adjustedMarketConfidence, input.EvidenceCapturedAtUtc, risks.Count);

        return new DealScoreResult(
            failures.Count == 0,
            sortScore,
            listingPrice,
            inboundShipping,
            effectiveBuyPrice,
            RoundMoney(estimatedSaleFees),
            RoundMoney(estimatedTotalCost),
            RoundMoney(netProfit),
            Math.Round(netMarginPercent, 4),
            Math.Round(roiPercent, 4),
            Math.Round(adjustedMarketConfidence, 4),
            failures,
            risks);
    }

    private static decimal AdjustMarketConfidence(DealScoringInput input)
    {
        var confidence = input.MarketConfidence;
        if (input.SoldCompCount <= 0 && input.MarketValueBasis == MarketValueBasis.ActiveListingMedian)
        {
            confidence = Math.Min(confidence, 0.40m);
        }
        else if (input.SoldCompCount <= 0 && input.MarketValueBasis == MarketValueBasis.ReferencePrice)
        {
            confidence = Math.Min(confidence, 0.60m);
        }

        return Math.Clamp(confidence, 0m, 1m);
    }

    private static decimal CalculateSortScore(decimal netProfit, decimal roiPercent, decimal liquidityScore, decimal confidence, DateTime? evidenceCapturedAtUtc, int riskCount)
    {
        var profitScore = Math.Clamp(netProfit / 50m, 0m, 1m);
        var roiScore = Math.Clamp(roiPercent / 0.50m, 0m, 1m);
        var freshnessScore = FreshnessScore(evidenceCapturedAtUtc);
        var riskPenalty = Math.Min(riskCount * 0.06m, 0.30m);

        var weighted = (profitScore * 0.30m)
            + (roiScore * 0.25m)
            + (Math.Clamp(liquidityScore, 0m, 1m) * 0.20m)
            + (Math.Clamp(confidence, 0m, 1m) * 0.15m)
            + (freshnessScore * 0.10m)
            - riskPenalty;

        return Math.Round(Math.Clamp(weighted, 0m, 1m) * 100m, 1);
    }

    private static decimal FreshnessScore(DateTime? capturedAtUtc)
    {
        if (!capturedAtUtc.HasValue) return 0.25m;
        var age = DateTime.UtcNow - capturedAtUtc.Value;
        if (age <= TimeSpan.FromDays(1)) return 1m;
        if (age <= TimeSpan.FromDays(7)) return 0.75m;
        if (age <= TimeSpan.FromDays(30)) return 0.50m;
        return 0.20m;
    }

    private static decimal SafeDivide(decimal numerator, decimal denominator)
        => denominator == 0m ? 0m : numerator / denominator;

    private static decimal RoundMoney(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
