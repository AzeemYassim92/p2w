using Microsoft.EntityFrameworkCore;
using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Data;

public sealed partial class CardsDbContext
{
    public static readonly Guid EbayMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000001");
    public static readonly Guid PokemonTcgMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000002");
    public static readonly Guid PriceChartingMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000003");
    public static readonly Guid TcgPlayerMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000004");
    public static readonly Guid CardmarketMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000005");
    public static readonly Guid P2WInternalMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000006");
    public static readonly Guid MockMarketMarketplaceSourceId = Guid.Parse("56000000-0000-0000-0000-000000000007");

    private static void ConfigureMarket(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketplaceSource>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(120);
            e.Property(x => x.DefaultCurrency).HasMaxLength(3);
        });

        modelBuilder.Entity<ExternalMarketplaceSkuMapping>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.MarketplaceSourceId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.MappingStatus);
            e.HasIndex(x => new { x.SourceName, x.ExternalSku }).IsUnique();
            e.Property(x => x.MatchConfidence).HasPrecision(18, 2);
            e.Property(x => x.MappingStatus).HasMaxLength(40).HasDefaultValue("AutoMatched");
        });

        modelBuilder.Entity<CatalogMarketplaceListing>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.MarketplaceSourceId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.CapturedAtUtc);
            e.HasIndex(x => x.LastSeenUtc);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.MatchStatus);
            e.HasIndex(x => x.IsExcludedFromMarketValue);
            e.HasIndex(x => new { x.MarketplaceSourceId, x.ExternalListingId }).IsUnique();
            e.HasIndex(x => new { x.CatalogProductId, x.SourceName, x.IsActive });
            foreach (var property in new[] { "ShippingPrice", "SellerFeedbackScore", "SellerFeedbackPercentage", "MatchConfidence" })
            {
                e.Property<decimal?>(property).HasPrecision(18, 2);
            }
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.EffectivePrice).HasPrecision(18, 2);
            e.Property(x => x.MatchStatus).HasMaxLength(40).HasDefaultValue("Matched");
        });

        modelBuilder.Entity<CatalogMarketplaceSale>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.MarketplaceSourceId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.SoldAtUtc);
            e.HasIndex(x => x.CapturedAtUtc);
            e.HasIndex(x => x.MatchStatus);
            e.HasIndex(x => x.IsExcludedFromMarketValue);
            e.HasIndex(x => new { x.MarketplaceSourceId, x.ExternalSaleId }).IsUnique();
            e.HasIndex(x => new { x.CatalogProductId, x.SourceName, x.SoldAtUtc });
            foreach (var property in new[] { "ShippingPrice", "MatchConfidence" })
            {
                e.Property<decimal?>(property).HasPrecision(18, 2);
            }
            e.Property(x => x.SoldPrice).HasPrecision(18, 2);
            e.Property(x => x.EffectiveSoldPrice).HasPrecision(18, 2);
            e.Property(x => x.MatchStatus).HasMaxLength(40).HasDefaultValue("Matched");
        });

        modelBuilder.Entity<CatalogMarketPriceSnapshot>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.MarketplaceSourceId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.Condition);
            e.HasIndex(x => x.CapturedAtUtc);
            e.HasIndex(x => new { x.CatalogProductId, x.SourceName, x.Condition, x.CapturedAtUtc });
            ConfigureNullableMoney(e);
        });

        modelBuilder.Entity<CatalogMarketMetric>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.Condition);
            e.HasIndex(x => x.Currency);
            e.HasIndex(x => x.WindowName);
            e.HasIndex(x => x.ComputedAtUtc);
            e.HasIndex(x => new { x.CatalogProductId, x.WindowName, x.Condition, x.ComputedAtUtc });
            ConfigureNullableMoney(e);
        });

        modelBuilder.Entity<CatalogProviderIngestionRun>(e =>
        {
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.WorkloadType);
            e.HasIndex(x => x.StartedUtc);
            e.HasIndex(x => x.Status);
            e.HasMany(x => x.Errors).WithOne(x => x.IngestionRun).HasForeignKey(x => x.IngestionRunId);
        });

        modelBuilder.Entity<CatalogProviderIngestionError>(e =>
        {
            e.HasIndex(x => x.IngestionRunId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.WorkloadType);
            e.HasIndex(x => x.ExternalId);
            e.HasIndex(x => x.CatalogProductId);
        });

        modelBuilder.Entity<CatalogAggregationCheckpoint>(e =>
        {
            e.HasIndex(x => new { x.SourceName, x.WorkloadType }).IsUnique();
            e.Property(x => x.SourceName).HasMaxLength(100);
            e.Property(x => x.WorkloadType).HasMaxLength(80);
        });

        modelBuilder.Entity<ProductMarketViewEvent>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.CreatedUtc);
            e.Property(x => x.EventType).HasMaxLength(60);
        });

        modelBuilder.Entity<CatalogWatchlistItem>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => new { x.UserId, x.CatalogProductId, x.ProductVariantId }).IsUnique();
            e.Property(x => x.TargetPrice).HasPrecision(18, 2);
            e.Property(x => x.TargetDiscountPercent).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SellerInventoryItem>(e =>
        {
            e.Property(x => x.CostBasis).HasPrecision(18, 2);
        });
    }

    private static void ConfigureNullableMoney<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        foreach (var property in entity.Metadata.GetProperties().Where(p => p.ClrType == typeof(decimal?)))
        {
            entity.Property<decimal?>(property.Name).HasPrecision(18, 2);
        }
    }

    private static void SeedMarket(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<MarketplaceSource>().HasData(
            Source(EbayMarketplaceSourceId, "eBay", "ebay", "https://www.ebay.com", true, listings: true, soldComps: false, buylist: false, referencePrices: false, bulkCsv: false, 1, now),
            Source(PokemonTcgMarketplaceSourceId, "PokemonTCG", "pokemontcg", "https://pokemontcg.io", true, listings: false, soldComps: false, buylist: false, referencePrices: true, bulkCsv: false, 2, now),
            Source(PriceChartingMarketplaceSourceId, "PriceCharting", "pricecharting", "https://www.pricecharting.com", true, listings: false, soldComps: false, buylist: false, referencePrices: true, bulkCsv: true, 3, now),
            Source(TcgPlayerMarketplaceSourceId, "TCGplayer", "tcgplayer", "https://www.tcgplayer.com", true, listings: false, soldComps: false, buylist: false, referencePrices: true, bulkCsv: false, 4, now),
            Source(CardmarketMarketplaceSourceId, "Cardmarket", "cardmarket", "https://www.cardmarket.com", true, listings: false, soldComps: false, buylist: false, referencePrices: true, bulkCsv: false, 5, now),
            Source(P2WInternalMarketplaceSourceId, "P2W Internal", "p2w-internal", "", false, listings: true, soldComps: true, buylist: false, referencePrices: false, bulkCsv: false, 6, now),
            Source(MockMarketMarketplaceSourceId, "MockMarket", "mockmarket", "https://example.com", true, listings: true, soldComps: true, buylist: false, referencePrices: true, bulkCsv: false, 99, now));
    }

    private static MarketplaceSource Source(Guid id, string name, string slug, string baseUrl, bool active, bool listings, bool soldComps, bool buylist, bool referencePrices, bool bulkCsv, int rank, DateTime now) => new()
    {
        Id = id,
        Name = name,
        Slug = slug,
        BaseUrl = baseUrl,
        IsActive = active,
        SupportsListings = listings,
        SupportsSoldComps = soldComps,
        SupportsBuylist = buylist,
        SupportsReferencePrices = referencePrices,
        SupportsBulkCsv = bulkCsv,
        DefaultCurrency = "USD",
        PriorityRank = rank,
        CreatedUtc = now,
        UpdatedUtc = now
    };
}
