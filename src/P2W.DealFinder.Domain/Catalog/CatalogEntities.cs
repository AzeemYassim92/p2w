using P2W.DealFinder.Domain.Shared;

namespace P2W.DealFinder.Domain.Catalog;

public sealed class CatalogProduct : AuditedEntity
{
    public string ProductType { get; set; } = "SingleCard";
    public string Category { get; set; } = "TradingCard";
    public string GameOrBrand { get; set; } = "";
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string? SetName { get; set; }
    public string? SetCode { get; set; }
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsSingleCard { get; set; } = true;
    public bool IsSealed { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductVariant> Variants { get; set; } = [];
    public List<ProductIdentifier> Identifiers { get; set; } = [];
}

public sealed class ProductVariant : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public string VariantName { get; set; } = "Normal";
    public string? Language { get; set; }
    public string? Printing { get; set; }
    public bool IsFoil { get; set; }
    public bool IsReverseHolo { get; set; }
    public bool IsFirstEdition { get; set; }
    public bool IsGraded { get; set; }
    public decimal? Grade { get; set; }
}

public sealed class ProductIdentifier : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? ExternalSku { get; set; }
    public string? ExternalUrl { get; set; }
    public decimal IdentityConfidence { get; set; }
    public IdentityMatchStatus MatchStatus { get; set; } = IdentityMatchStatus.AutoMatched;
    public string? MatchNotes { get; set; }
    public DateTime? LastVerifiedUtc { get; set; }
}

public sealed class CatalogProductProviderCoverage : AuditedEntity
{
    public Guid CatalogProductId { get; set; }
    public string SourceName { get; set; } = "";
    public bool IdentityResolved { get; set; }
    public bool MetadataComplete { get; set; }
    public bool ReferencePricePresent { get; set; }
    public bool ActiveListingsPresent { get; set; }
    public bool SoldCompsPresent { get; set; }
    public DateTime? LastSuccessUtc { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public string? LastNoDataReason { get; set; }
    public string? LastError { get; set; }
}
