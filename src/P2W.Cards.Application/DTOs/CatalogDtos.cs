using P2W.Cards.Domain.Enums;

namespace P2W.Cards.Application.DTOs;

public sealed class GameDto
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrimaryFocus { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class CardSetDto
{
    public Guid CardSetId { get; set; }
    public Guid GameId { get; set; }
    public string GameName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Code { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsUpcoming { get; set; }
    public string? LogoUrl { get; set; }
    public string? SymbolUrl { get; set; }
}

public sealed class ProductCategoryDto
{
    public Guid ProductCategoryId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public IReadOnlyList<ProductCategoryDto> Children { get; set; } = Array.Empty<ProductCategoryDto>();
}

public class CatalogProductDto
{
    public Guid CatalogProductId { get; set; }
    public Guid GameId { get; set; }
    public string GameName { get; set; } = "";
    public Guid? CardSetId { get; set; }
    public string? SetName { get; set; }
    public string? SetCode { get; set; }
    public Guid ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? EstimatedMarketPrice { get; set; }
    public string? PrimarySourceName { get; set; }
    public string? PrimarySourceUrl { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsSealed { get; set; }
    public bool IsSingleCard { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsTrending { get; set; }
}

public sealed class CatalogProductDetailDto : CatalogProductDto
{
    public string? Artist { get; set; }
    public string? Description { get; set; }
    public bool IsGradedEligible { get; set; }
    public IReadOnlyList<ProductVariantDto> Variants { get; set; } = Array.Empty<ProductVariantDto>();
    public IReadOnlyList<ExternalProductMappingDto> ExternalMappings { get; set; } = Array.Empty<ExternalProductMappingDto>();
}

public sealed class ProductVariantDto
{
    public Guid ProductVariantId { get; set; }
    public string VariantName { get; set; } = "";
    public string? Language { get; set; }
    public bool IsFoil { get; set; }
    public bool IsReverseHolo { get; set; }
    public bool IsFirstEdition { get; set; }
    public bool IsPromo { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsSealedCase { get; set; }
}

public sealed class ExternalProductMappingDto
{
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? ExternalUrl { get; set; }
    public string? ExternalSlug { get; set; }
    public decimal? ConfidenceScore { get; set; }
}

public sealed class CatalogProductQuery
{
    public Guid? GameId { get; set; }
    public string? GameSlug { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public Guid? CardSetId { get; set; }
    public string? ProductType { get; set; }
    public string? Query { get; set; }
    public int Take { get; set; } = 24;
}

public sealed class MarketplaceHomeDto
{
    public IReadOnlyList<GameDto> PrimaryGames { get; set; } = Array.Empty<GameDto>();
    public IReadOnlyList<ProductCategoryDto> Categories { get; set; } = Array.Empty<ProductCategoryDto>();
    public IReadOnlyList<CatalogProductDto> TrendingProducts { get; set; } = Array.Empty<CatalogProductDto>();
    public IReadOnlyList<CatalogProductDto> FeaturedProducts { get; set; } = Array.Empty<CatalogProductDto>();
    public IReadOnlyList<CardSetDto> LatestSets { get; set; } = Array.Empty<CardSetDto>();
    public IReadOnlyList<CardSetDto> UpcomingSets { get; set; } = Array.Empty<CardSetDto>();
    public IReadOnlyList<ProviderCapabilityDto> ProviderCapabilities { get; set; } = Array.Empty<ProviderCapabilityDto>();
}

public sealed class ProviderCapabilityDto
{
    public string SourceName { get; set; } = "";
    public bool SupportsMagic { get; set; }
    public bool SupportsPokemon { get; set; }
    public bool SupportsOnePiece { get; set; }
    public bool SupportsCatalogSearch { get; set; }
    public bool SupportsMarketplaceListings { get; set; }
    public bool SupportsPriceReference { get; set; }
    public bool IsConfigured { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class SellerInventoryItemDto
{
    public Guid SellerInventoryItemId { get; set; }
    public Guid SellerUserId { get; set; }
    public Guid CatalogProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string GameName { get; set; } = "";
    public string? SetName { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? VariantName { get; set; }
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
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CreateSellerInventoryItemRequest
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductCondition Condition { get; set; }
    public string? RawConditionNotes { get; set; }
    public bool IsGraded { get; set; }
    public string? GradingCompany { get; set; }
    public decimal? Grade { get; set; }
    public string? CertificationNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? AskingPrice { get; set; }
    public decimal? CostBasis { get; set; }
    public DateTime? AcquiredAtUtc { get; set; }
    public string? AcquisitionSource { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsAvailableForSale { get; set; } = true;
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
}
