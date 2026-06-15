using P2W.Cards.Domain.Enums;

namespace P2W.Cards.Domain.Entities;

public sealed class Game
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrimaryFocus { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CardSet
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Code { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsUpcoming { get; set; }
    public bool IsActive { get; set; }
    public string? LogoUrl { get; set; }
    public string? SymbolUrl { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class ProductCategory
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public ProductCategory? ParentCategory { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CatalogProduct
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    public Guid? CardSetId { get; set; }
    public CardSet? CardSet { get; set; }
    public Guid ProductCategoryId { get; set; }
    public ProductCategory? ProductCategory { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? Artist { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsSealed { get; set; }
    public bool IsSingleCard { get; set; }
    public bool IsGradedEligible { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsTrending { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public string VariantName { get; set; } = "";
    public string? Language { get; set; }
    public bool IsFoil { get; set; }
    public bool IsReverseHolo { get; set; }
    public bool IsFirstEdition { get; set; }
    public bool IsPromo { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsSealedCase { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class SellerInventoryItem
{
    public Guid Id { get; set; }
    public Guid SellerUserId { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public ProductCondition Condition { get; set; }
    public string? RawConditionNotes { get; set; }
    public bool IsGraded { get; set; }
    public string? GradingCompany { get; set; }
    public decimal? Grade { get; set; }
    public string? CertificationNumber { get; set; }
    public int Quantity { get; set; }
    public decimal? AskingPrice { get; set; }
    public decimal? CostBasis { get; set; }
    public DateTime? AcquiredAtUtc { get; set; }
    public string? AcquisitionSource { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsAvailableForSale { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public ICollection<SellerInventoryImage> Images { get; set; } = new List<SellerInventoryImage>();
}

public sealed class SellerInventoryImage
{
    public Guid Id { get; set; }
    public Guid SellerInventoryItemId { get; set; }
    public SellerInventoryItem? SellerInventoryItem { get; set; }
    public string ImageUrl { get; set; } = "";
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class ExternalProductMapping
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? ExternalUrl { get; set; }
    public string? ExternalSlug { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string MappingStatus { get; set; } = "AutoMatched";
    public string? MappingNotes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastVerifiedUtc { get; set; }
}

public sealed class CatalogImportCheckpoint
{
    public Guid Id { get; set; }
    public string SourceName { get; set; } = "";
    public string ImportType { get; set; } = "";
    public string CheckpointValue { get; set; } = "";
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CatalogPriceReferenceSnapshot
{
    public Guid Id { get; set; }
    public Guid CatalogProductId { get; set; }
    public CatalogProduct? CatalogProduct { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
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

public sealed class CatalogImportRun
{
    public Guid Id { get; set; }
    public string SourceName { get; set; } = "";
    public string ImportType { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int ErrorCount { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}

public sealed class CatalogImportError
{
    public Guid Id { get; set; }
    public Guid CatalogImportRunId { get; set; }
    public CatalogImportRun? CatalogImportRun { get; set; }
    public string SourceName { get; set; } = "";
    public string? ExternalId { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string? RawSourceJson { get; set; }
    public DateTime CreatedUtc { get; set; }
}
