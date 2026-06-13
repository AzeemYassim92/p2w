namespace P2W.Cards.Domain.Entities;

public sealed class Card
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string SetName { get; set; } = "";
    public string? SetCode { get; set; }
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? Artist { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public ICollection<CardVariant> Variants { get; set; } = new List<CardVariant>();
}

public sealed class CardVariant
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public string VariantName { get; set; } = "";
    public string? Language { get; set; }
    public bool IsFoil { get; set; }
    public bool IsReverseHolo { get; set; }
    public bool IsFirstEdition { get; set; }
    public bool IsGraded { get; set; }
    public string? GradingCompany { get; set; }
    public decimal? Grade { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class ExternalSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ProviderType { get; set; } = "";
    public bool IsActive { get; set; }
    public int PriorityRank { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class ExternalCardMapping
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? ExternalUrl { get; set; }
    public string? ExternalSlug { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastVerifiedUtc { get; set; }
}

public sealed class Marketplace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class Listing
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public Guid? CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }
    public Guid MarketplaceId { get; set; }
    public Marketplace? Marketplace { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalListingId { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? ShippingPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Condition { get; set; } = "Unknown";
    public string? RawCondition { get; set; }
    public string ListingUrl { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsAuction { get; set; }
    public DateTime? AuctionEndsUtc { get; set; }
    public DateTime ListedAtUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? RawSourceJson { get; set; }
}

public sealed class PriceSnapshot
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public Guid? CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }
    public Guid MarketplaceId { get; set; }
    public Marketplace? Marketplace { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MedianPrice { get; set; }
    public int ListingCount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CapturedAtUtc { get; set; }
}

public sealed class PriceReferenceSnapshot
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public Guid? CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }
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
    public DateTime CapturedAtUtc { get; set; }
}

public sealed class WatchlistItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public Guid? CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }
    public decimal? TargetPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class PriceAlert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public Guid? CardVariantId { get; set; }
    public CardVariant? CardVariant { get; set; }
    public decimal TargetPrice { get; set; }
    public bool IsActive { get; set; }
    public bool HasTriggered { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}
