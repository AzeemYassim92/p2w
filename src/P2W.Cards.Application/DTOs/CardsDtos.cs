namespace P2W.Cards.Application.DTOs;

public sealed class CardSearchResultDto
{
    public Guid CardId { get; set; }
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string SetName { get; set; } = "";
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? LowestListingPrice { get; set; }
    public decimal? MarketReferencePrice { get; set; }
}

public sealed class MarketplaceProductDto
{
    public Guid CardId { get; set; }
    public string ProductType { get; set; } = "";
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string SetName { get; set; } = "";
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string SourceName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public sealed class CardDetailDto
{
    public Guid CardId { get; set; }
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string SetName { get; set; } = "";
    public string? SetCode { get; set; }
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? Artist { get; set; }
    public string? ImageUrl { get; set; }
    public IReadOnlyList<CardVariantDto> Variants { get; set; } = Array.Empty<CardVariantDto>();
}

public sealed class CardVariantDto
{
    public Guid CardVariantId { get; set; }
    public string VariantName { get; set; } = "";
    public string? Language { get; set; }
    public bool IsFoil { get; set; }
    public bool IsReverseHolo { get; set; }
    public bool IsFirstEdition { get; set; }
    public bool IsGraded { get; set; }
    public string? GradingCompany { get; set; }
    public decimal? Grade { get; set; }
}

public sealed class ListingDto
{
    public Guid ListingId { get; set; }
    public string MarketplaceName { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? ShippingPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Condition { get; set; } = "";
    public string? RawCondition { get; set; }
    public string ListingUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsAuction { get; set; }
    public DateTime? AuctionEndsUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; }
}

public sealed class PriceSnapshotDto
{
    public Guid CardId { get; set; }
    public string SourceName { get; set; } = "";
    public decimal LowestPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MedianPrice { get; set; }
    public int ListingCount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CapturedAtUtc { get; set; }
}

public sealed class PriceReferenceSnapshotDto
{
    public Guid CardId { get; set; }
    public string SourceName { get; set; } = "";
    public decimal? MarketPrice { get; set; }
    public decimal? UngradedPrice { get; set; }
    public decimal? Grade7Price { get; set; }
    public decimal? Grade8Price { get; set; }
    public decimal? Grade9Price { get; set; }
    public decimal? Grade10Price { get; set; }
    public decimal? BuylistPrice { get; set; }
    public decimal? RetailPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CapturedAtUtc { get; set; }
}

public sealed class WatchlistItemDto
{
    public Guid WatchlistItemId { get; set; }
    public Guid CardId { get; set; }
    public string CardName { get; set; } = "";
    public string Game { get; set; } = "";
    public string SetName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? CurrentLowestPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class PriceAlertDto
{
    public Guid PriceAlertId { get; set; }
    public Guid CardId { get; set; }
    public string CardName { get; set; } = "";
    public decimal TargetPrice { get; set; }
    public bool IsActive { get; set; }
    public bool HasTriggered { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class ProviderInfoDto
{
    public string SourceName { get; set; } = "";
    public string ProviderType { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = "";
}

public sealed class AddWatchlistItemRequest
{
    public Guid CardId { get; set; }
    public Guid? CardVariantId { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreatePriceAlertRequest
{
    public Guid CardId { get; set; }
    public Guid? CardVariantId { get; set; }
    public decimal TargetPrice { get; set; }
}

public sealed class ExternalCardDto
{
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string SetName { get; set; } = "";
    public string? SetCode { get; set; }
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? Artist { get; set; }
    public string? ImageUrl { get; set; }
    public string? ExternalUrl { get; set; }
}

public sealed class MarketplaceListingDto
{
    public string SourceName { get; set; } = "";
    public string ExternalListingId { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? ShippingPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Condition { get; set; } = "Unknown";
    public string ListingUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsAuction { get; set; }
    public DateTime? AuctionEndsUtc { get; set; }
    public DateTime ListedAtUtc { get; set; }
    public string? RawSourceJson { get; set; }
}

public sealed class ExternalPriceReferenceDto
{
    public string SourceName { get; set; } = "";
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
    public string Currency { get; set; } = "USD";
    public string? RawSourceJson { get; set; }
}

public sealed class ExternalCardMappingDto
{
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? ExternalUrl { get; set; }
}

public sealed class CardSearchContext
{
    public Guid CardId { get; set; }
    public string CardName { get; set; } = "";
    public string Game { get; set; } = "";
    public string? SetName { get; set; }
    public string? SetCode { get; set; }
    public string? CardNumber { get; set; }
    public IReadOnlyList<ExternalCardMappingDto> ExternalMappings { get; set; } = Array.Empty<ExternalCardMappingDto>();
}
