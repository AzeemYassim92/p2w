using P2W.Cards.Application.DTOs;
using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbayListingNormalizer
{
    private static readonly string[] ExcludedKeywords = ["proxy", "custom", "digital", "code", "repack", "mystery", "fake", "replica", "oversized", "jumbo", "damaged", "poor", "read"];

    public ExternalMarketplaceListingDto Normalize(EbayItemSummaryDto item, CatalogProduct product)
    {
        var price = ParseMoney(item.Price) ?? 0m;
        decimal? shipping = item.ShippingOptions
            .Select(o => ParseMoney(o.ShippingCost))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0m)
            .Min();
        var effective = price + (shipping ?? 0m);
        var confidence = Score(item.Title, product);
        var exclusion = ExclusionReason(item.Title, product);
        return new ExternalMarketplaceListingDto
        {
            SourceName = "eBay",
            ExternalListingId = item.ItemId,
            ExternalSku = item.ItemId,
            Title = item.Title,
            Price = price,
            ShippingPrice = shipping,
            EffectivePrice = effective,
            Currency = item.Price?.Currency ?? "USD",
            Condition = NormalizeCondition(item.Condition),
            RawCondition = item.Condition,
            SellerName = item.Seller?.Username,
            SellerFeedbackScore = item.Seller?.FeedbackScore,
            SellerFeedbackPercentage = ParsePercent(item.Seller?.FeedbackPercentage),
            SellerLocation = item.ItemLocation?.Country,
            ListingUrl = item.ItemWebUrl,
            ImageUrl = item.Image?.ImageUrl,
            IsAuction = item.BuyingOptions.Contains("AUCTION", StringComparer.OrdinalIgnoreCase),
            AuctionEndsUtc = item.ItemEndDate,
            ListedAtUtc = item.ItemCreationDate,
            CapturedAtUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
            MatchConfidence = confidence,
            MatchStatus = confidence >= 0.90m ? "Matched" : "NeedsReview",
            IsExcludedFromMarketValue = exclusion != null || confidence < 0.90m,
            ExclusionReason = exclusion ?? (confidence < 0.90m ? "Below public market confidence threshold" : null)
        };
    }

    public decimal Score(string title, CatalogProduct product)
    {
        var value = title.ToLowerInvariant();
        var score = 0m;
        if (value.Contains(product.Name.ToLowerInvariant())) score += 0.30m;
        if (!string.IsNullOrWhiteSpace(product.CardNumber) && value.Contains(product.CardNumber.ToLowerInvariant())) score += 0.25m;
        if (product.CardSet?.Name is { Length: > 0 } set && value.Contains(set.ToLowerInvariant())) score += 0.20m;
        if (product.CardSet?.Code is { Length: > 0 } code && value.Contains(code.ToLowerInvariant())) score += 0.20m;
        if (product.Game?.Name.Contains("Pokemon", StringComparison.OrdinalIgnoreCase) == true && value.Contains("pokemon")) score += 0.10m;
        if (product.Game?.Name.Contains("One Piece", StringComparison.OrdinalIgnoreCase) == true && value.Contains("one piece")) score += 0.10m;
        if (!ExcludedKeywords.Any(k => value.Contains(k))) score += 0.05m;
        return Math.Clamp(score, 0m, 1m);
    }

    private static string? ExclusionReason(string title, CatalogProduct product)
    {
        var value = title.ToLowerInvariant();
        var keyword = ExcludedKeywords.FirstOrDefault(value.Contains);
        if (keyword != null) return $"Excluded keyword: {keyword}";
        if (product.IsSingleCard && value.Contains(" lot ")) return "Single-card market value excludes lots";
        if (product.IsSingleCard && (value.Contains("psa") || value.Contains("bgs") || value.Contains("cgc"))) return "Raw market value excludes graded listings";
        if (product.IsSealed && (value.Contains("empty box") || value.Contains("wrapper") || value.Contains("code"))) return "Sealed market value excludes non-product listings";
        return null;
    }

    private static string NormalizeCondition(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unknown";
        var value = raw.ToUpperInvariant();
        if (value.Contains("NEW") || value.Contains("NEAR MINT")) return "NearMint";
        if (value.Contains("LIGHT")) return "LightlyPlayed";
        if (value.Contains("MODERATE")) return "ModeratelyPlayed";
        if (value.Contains("HEAVY")) return "HeavilyPlayed";
        if (value.Contains("DAMAGED")) return "Damaged";
        return "Unknown";
    }

    private static decimal? ParseMoney(EbayMoneyDto? money)
        => decimal.TryParse(money?.Value, out var parsed) ? parsed : null;

    private static decimal? ParsePercent(string? value)
        => decimal.TryParse(value?.TrimEnd('%'), out var parsed) ? parsed : null;
}
