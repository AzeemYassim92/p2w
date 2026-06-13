using Microsoft.EntityFrameworkCore;
using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Data;

public sealed class CardsDbContext(DbContextOptions<CardsDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardVariant> CardVariants => Set<CardVariant>();
    public DbSet<ExternalSource> ExternalSources => Set<ExternalSource>();
    public DbSet<ExternalCardMapping> ExternalCardMappings => Set<ExternalCardMapping>();
    public DbSet<Marketplace> Marketplaces => Set<Marketplace>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<PriceReferenceSnapshot> PriceReferenceSnapshots => Set<PriceReferenceSnapshot>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCards(modelBuilder);
        ConfigurePrices(modelBuilder);
        Seed(modelBuilder);
    }

    private static void ConfigureCards(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(e =>
        {
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Game);
            e.HasIndex(x => x.SetName);
            e.HasIndex(x => new { x.Game, x.Name, x.SetName });
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Game).HasMaxLength(40);
            e.Property(x => x.SetName).HasMaxLength(200);
            e.HasMany(x => x.Variants).WithOne(x => x.Card).HasForeignKey(x => x.CardId);
        });

        modelBuilder.Entity<CardVariant>(e =>
        {
            e.Property(x => x.Grade).HasPrecision(18, 2);
            e.HasIndex(x => x.CardId);
        });

        modelBuilder.Entity<ExternalSource>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ExternalCardMapping>(e =>
        {
            e.HasIndex(x => x.CardId);
            e.HasIndex(x => new { x.SourceName, x.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<Marketplace>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
        });
    }

    private static void ConfigurePrices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Listing>(e =>
        {
            e.HasIndex(x => x.CardId);
            e.HasIndex(x => x.MarketplaceId);
            e.HasIndex(x => x.CapturedAtUtc);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => new { x.MarketplaceId, x.ExternalListingId }).IsUnique();
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.ShippingPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PriceSnapshot>(e =>
        {
            e.HasIndex(x => new { x.CardId, x.CardVariantId, x.CapturedAtUtc });
            e.Property(x => x.LowestPrice).HasPrecision(18, 2);
            e.Property(x => x.AveragePrice).HasPrecision(18, 2);
            e.Property(x => x.MedianPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PriceReferenceSnapshot>(e =>
        {
            e.HasIndex(x => x.CardId);
            e.HasIndex(x => x.CardVariantId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.CapturedAtUtc);
            foreach (var property in new[] { "MarketPrice", "LowPrice", "MidPrice", "HighPrice", "UngradedPrice", "Grade7Price", "Grade8Price", "Grade9Price", "Grade10Price", "BuylistPrice", "RetailPrice" })
            {
                e.Property<decimal?>(property).HasPrecision(18, 2);
            }
        });

        modelBuilder.Entity<WatchlistItem>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CardId);
            e.HasIndex(x => new { x.UserId, x.CardId, x.CardVariantId }).IsUnique();
            e.Property(x => x.TargetPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PriceAlert>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CardId);
            e.HasIndex(x => x.IsActive);
            e.Property(x => x.TargetPrice).HasPrecision(18, 2);
        });
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var marketplaceIds = new Dictionary<string, Guid>
        {
            ["MockMarket"] = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            ["eBay"] = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            ["TCGplayer"] = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            ["Card Kingdom"] = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            ["PriceCharting"] = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            ["Cardmarket"] = Guid.Parse("10000000-0000-0000-0000-000000000006")
        };

        modelBuilder.Entity<Marketplace>().HasData(marketplaceIds.Select((x, i) => new Marketplace
        {
            Id = x.Value,
            Name = x.Key,
            BaseUrl = x.Key == "MockMarket" ? "https://example.com" : "",
            IsActive = x.Key is "MockMarket" or "eBay" or "TCGplayer",
            CreatedUtc = now
        }));

        var sources = new[] { "Mock", "TCGplayer", "eBay", "Scryfall", "MTGJSON", "PokemonTCG", "PriceCharting", "CardKingdom", "Cardmarket" };
        modelBuilder.Entity<ExternalSource>().HasData(sources.Select((name, i) => new ExternalSource
        {
            Id = Guid.Parse($"20000000-0000-0000-0000-{(i + 1):000000000000}"),
            Name = name,
            ProviderType = name is "Scryfall" or "PokemonTCG" ? "Catalog" : name is "eBay" or "Cardmarket" ? "MarketplaceListing" : "PriceReference",
            IsActive = name is "Mock" or "Scryfall",
            PriorityRank = i + 1,
            CreatedUtc = now
        }));

        var cards = new List<Card>();
        AddCards(cards, "Pokemon", new[]
        {
            ("Charizard", "Base Set", "4/102", "Rare Holo", "Mitsuhiro Arita"),
            ("Pikachu", "Jungle", "60/64", "Common", "Mitsuhiro Arita"),
            ("Blastoise", "Base Set", "2/102", "Rare Holo", "Ken Sugimori"),
            ("Mewtwo", "Base Set", "10/102", "Rare Holo", "Ken Sugimori"),
            ("Gengar", "Fossil", "5/62", "Rare Holo", "Keiji Kinebuchi"),
            ("Rayquaza", "EX Deoxys", "22/107", "Rare Holo", "Mitsuhiro Arita"),
            ("Lugia", "Neo Genesis", "9/111", "Rare Holo", "Hironobu Yoshida"),
            ("Umbreon", "Neo Discovery", "13/75", "Rare Holo", "Ken Sugimori")
        }, now);
        AddCards(cards, "Magic", new[]
        {
            ("Black Lotus", "Limited Edition Alpha", "232", "Rare", "Christopher Rush"),
            ("Sol Ring", "Commander", "276", "Uncommon", "Mark Tedin"),
            ("Lightning Bolt", "Magic 2011", "146", "Common", "Christopher Moeller"),
            ("Counterspell", "Seventh Edition", "67", "Common", "Hannibal King"),
            ("Mox Diamond", "Stronghold", "138", "Rare", "Dan Frazier"),
            ("Mana Crypt", "Eternal Masters", "225", "Mythic Rare", "Matt Stewart"),
            ("Rhystic Study", "Prophecy", "45", "Common", "Terese Nielsen"),
            ("Dockside Extortionist", "Commander 2019", "24", "Rare", "Forrest Imel")
        }, now);
        modelBuilder.Entity<Card>().HasData(cards);
    }

    private static void AddCards(List<Card> cards, string game, IEnumerable<(string Name, string SetName, string Number, string Rarity, string Artist)> data, DateTime now)
    {
        var prefix = game == "Pokemon" ? "30000000" : "40000000";
        var index = 1;
        foreach (var item in data)
        {
            cards.Add(new Card
            {
                Id = Guid.Parse($"{prefix}-0000-0000-0000-{index:000000000000}"),
                Name = item.Name,
                Game = game,
                SetName = item.SetName,
                SetCode = item.SetName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant(),
                CardNumber = item.Number,
                Rarity = item.Rarity,
                Artist = item.Artist,
                ImageUrl = $"https://placehold.co/245x342?text={Uri.EscapeDataString(item.Name)}",
                CreatedUtc = now,
                UpdatedUtc = now
            });
            index++;
        }
    }
}
