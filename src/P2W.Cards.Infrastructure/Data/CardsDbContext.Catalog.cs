using Microsoft.EntityFrameworkCore;
using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Data;

public sealed partial class CardsDbContext
{
    private static readonly Guid MagicGameId = Guid.Parse("51000000-0000-0000-0000-000000000001");
    private static readonly Guid PokemonGameId = Guid.Parse("51000000-0000-0000-0000-000000000002");
    private static readonly Guid OnePieceGameId = Guid.Parse("51000000-0000-0000-0000-000000000003");

    private static readonly Guid SinglesCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000001");
    private static readonly Guid SealedCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000002");
    private static readonly Guid BoosterPacksCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000003");
    private static readonly Guid BoosterBoxesCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000004");
    private static readonly Guid EliteTrainerBoxesCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000005");
    private static readonly Guid CommanderDecksCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000008");
    private static readonly Guid StarterDecksCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000009");
    private static readonly Guid GradedCardsCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000011");
    private static readonly Guid RawSinglesCategoryId = Guid.Parse("52000000-0000-0000-0000-000000000018");

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Slug).HasMaxLength(140);
        });

        modelBuilder.Entity<CardSet>(e =>
        {
            e.HasIndex(x => x.GameId);
            e.HasIndex(x => x.Slug);
            e.HasIndex(x => new { x.GameId, x.Name }).IsUnique();
            e.HasIndex(x => new { x.GameId, x.Code });
            e.Property(x => x.Name).HasMaxLength(180);
            e.Property(x => x.Slug).HasMaxLength(200);
        });

        modelBuilder.Entity<ProductCategory>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.ParentCategoryId);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Slug).HasMaxLength(140);
        });

        modelBuilder.Entity<CatalogProduct>(e =>
        {
            e.HasIndex(x => x.GameId);
            e.HasIndex(x => x.CardSetId);
            e.HasIndex(x => x.ProductCategoryId);
            e.HasIndex(x => x.Slug);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.ProductType);
            e.HasIndex(x => x.IsFeatured);
            e.HasIndex(x => x.IsTrending);
            e.HasIndex(x => new { x.GameId, x.Name, x.CardSetId });
            e.Property(x => x.Name).HasMaxLength(240);
            e.Property(x => x.Slug).HasMaxLength(260);
            e.Property(x => x.ProductType).HasMaxLength(80);
            e.HasMany(x => x.Variants).WithOne(x => x.CatalogProduct).HasForeignKey(x => x.CatalogProductId);
        });

        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => new { x.CatalogProductId, x.VariantName });
        });

        modelBuilder.Entity<SellerInventoryItem>(e =>
        {
            e.HasIndex(x => x.SellerUserId);
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.Condition);
            e.HasIndex(x => x.IsAvailableForSale);
            e.Property(x => x.AskingPrice).HasPrecision(18, 2);
            e.Property(x => x.Grade).HasPrecision(18, 2);
            e.HasMany(x => x.Images).WithOne(x => x.SellerInventoryItem).HasForeignKey(x => x.SellerInventoryItemId);
        });

        modelBuilder.Entity<SellerInventoryImage>(e => e.HasIndex(x => x.SellerInventoryItemId));

        modelBuilder.Entity<ExternalProductMapping>(e =>
        {
            e.HasIndex(x => x.CatalogProductId);
            e.HasIndex(x => new { x.SourceName, x.ExternalId }).IsUnique();
            e.Property(x => x.ConfidenceScore).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CatalogImportRun>(e =>
        {
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.ImportType);
            e.HasIndex(x => x.StartedUtc);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<CatalogImportError>(e =>
        {
            e.HasIndex(x => x.CatalogImportRunId);
            e.HasIndex(x => x.SourceName);
            e.HasIndex(x => x.ExternalId);
        });
    }

    private static void SeedCatalog(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var games = new[]
        {
            Game(MagicGameId, "Magic: The Gathering", "magic-the-gathering", true, 1, now),
            Game(PokemonGameId, "Pokemon", "pokemon", true, 2, now),
            Game(OnePieceGameId, "One Piece", "one-piece", true, 3, now),
            Game(Guid.Parse("51000000-0000-0000-0000-000000000004"), "Yu-Gi-Oh!", "yu-gi-oh", false, 4, now),
            Game(Guid.Parse("51000000-0000-0000-0000-000000000005"), "Disney Lorcana", "disney-lorcana", false, 5, now),
            Game(Guid.Parse("51000000-0000-0000-0000-000000000006"), "Digimon", "digimon", false, 6, now),
            Game(Guid.Parse("51000000-0000-0000-0000-000000000007"), "Star Wars: Unlimited", "star-wars-unlimited", false, 7, now),
            Game(Guid.Parse("51000000-0000-0000-0000-000000000008"), "Flesh and Blood", "flesh-and-blood", false, 8, now)
        };
        modelBuilder.Entity<Game>().HasData(games);

        var categories = new[]
        {
            Category(SinglesCategoryId, null, "Singles", "singles", 1, now),
            Category(SealedCategoryId, null, "Sealed", "sealed", 2, now),
            Category(BoosterPacksCategoryId, SealedCategoryId, "Booster Packs", "booster-packs", 3, now),
            Category(BoosterBoxesCategoryId, SealedCategoryId, "Booster Boxes", "booster-boxes", 4, now),
            Category(EliteTrainerBoxesCategoryId, SealedCategoryId, "Elite Trainer Boxes", "elite-trainer-boxes", 5, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000006"), SealedCategoryId, "Bundles", "bundles", 6, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000007"), SealedCategoryId, "Starter Decks", "starter-decks", 7, now),
            Category(CommanderDecksCategoryId, SealedCategoryId, "Commander Decks", "commander-decks", 8, now),
            Category(StarterDecksCategoryId, SealedCategoryId, "Battle Decks", "battle-decks", 9, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000010"), SealedCategoryId, "Collection Boxes", "collection-boxes", 10, now),
            Category(GradedCardsCategoryId, SinglesCategoryId, "Graded Cards", "graded-cards", 11, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000012"), null, "Accessories", "accessories", 12, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000013"), null, "Supplies", "supplies", 13, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000014"), null, "Bulk Lots", "bulk-lots", 14, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000015"), null, "Complete Sets", "complete-sets", 15, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000016"), null, "Preorders", "preorders", 16, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000017"), null, "Deals", "deals", 17, now),
            Category(RawSinglesCategoryId, SinglesCategoryId, "Raw Singles", "raw-singles", 18, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000019"), SinglesCategoryId, "Foils", "foils", 19, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000020"), SinglesCategoryId, "Reverse Holos", "reverse-holos", 20, now),
            Category(Guid.Parse("52000000-0000-0000-0000-000000000021"), SinglesCategoryId, "Promos", "promos", 21, now)
        };
        modelBuilder.Entity<ProductCategory>().HasData(categories);

        var sets = BuildSets(now);
        modelBuilder.Entity<CardSet>().HasData(sets);

        var products = BuildCatalogProducts(sets, now);
        modelBuilder.Entity<CatalogProduct>().HasData(products);
        modelBuilder.Entity<ProductVariant>().HasData(products.Take(12).Select((p, i) => new ProductVariant
        {
            Id = Guid.Parse($"55000000-0000-0000-0000-{(i + 1):000000000000}"),
            CatalogProductId = p.Id,
            VariantName = "Normal",
            Language = "English",
            CreatedUtc = now
        }));
    }

    private static Game Game(Guid id, string name, string slug, bool focus, int order, DateTime now) => new()
    {
        Id = id,
        Name = name,
        Slug = slug,
        Description = $"Catalog products for {name}.",
        IsPrimaryFocus = focus,
        IsActive = true,
        DisplayOrder = order,
        CreatedUtc = now,
        UpdatedUtc = now
    };

    private static ProductCategory Category(Guid id, Guid? parentId, string name, string slug, int order, DateTime now) => new()
    {
        Id = id,
        ParentCategoryId = parentId,
        Name = name,
        Slug = slug,
        Description = $"Browse {name}.",
        IsActive = true,
        DisplayOrder = order,
        CreatedUtc = now,
        UpdatedUtc = now
    };

    private static List<CardSet> BuildSets(DateTime now)
    {
        var data = new (Guid Id, Guid GameId, string Name, string Slug, string Code, DateTime Release, bool Upcoming)[]
        {
            (Guid.Parse("53000000-0000-0000-0000-000000000001"), MagicGameId, "Final Fantasy", "final-fantasy", "FIN", new DateTime(2025, 6, 13), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000002"), MagicGameId, "Tarkir: Dragonstorm", "tarkir-dragonstorm", "TDM", new DateTime(2025, 4, 11), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000003"), MagicGameId, "Magic: The Gathering FINAL FANTASY", "magic-the-gathering-final-fantasy", "MFF", new DateTime(2025, 6, 13), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000004"), MagicGameId, "Marvel Super Heroes", "marvel-super-heroes", "MAR", new DateTime(2026, 11, 1), true),
            (Guid.Parse("53000000-0000-0000-0000-000000000005"), PokemonGameId, "Prismatic Evolutions", "prismatic-evolutions", "PRE", new DateTime(2025, 1, 17), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000006"), PokemonGameId, "Destined Rivals", "destined-rivals", "DRI", new DateTime(2025, 5, 30), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000007"), PokemonGameId, "Mega Evolution", "mega-evolution", "MEG", new DateTime(2026, 9, 1), true),
            (Guid.Parse("53000000-0000-0000-0000-000000000008"), PokemonGameId, "Chaos Rising", "chaos-rising", "CHR", new DateTime(2026, 11, 1), true),
            (Guid.Parse("53000000-0000-0000-0000-000000000009"), PokemonGameId, "Ascended Heroes", "ascended-heroes", "ASH", new DateTime(2026, 12, 1), true),
            (Guid.Parse("53000000-0000-0000-0000-000000000010"), OnePieceGameId, "Romance Dawn", "romance-dawn", "OP01", new DateTime(2022, 12, 2), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000011"), OnePieceGameId, "Paramount War", "paramount-war", "OP02", new DateTime(2023, 3, 10), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000012"), OnePieceGameId, "Pillars of Strength", "pillars-of-strength", "OP03", new DateTime(2023, 6, 30), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000013"), OnePieceGameId, "Kingdoms of Intrigue", "kingdoms-of-intrigue", "OP04", new DateTime(2023, 9, 22), false),
            (Guid.Parse("53000000-0000-0000-0000-000000000014"), OnePieceGameId, "500 Years in the Future", "500-years-in-the-future", "OP07", new DateTime(2024, 6, 28), false)
        };

        return data.Select(x => new CardSet
        {
            Id = x.Id,
            GameId = x.GameId,
            Name = x.Name,
            Slug = x.Slug,
            Code = x.Code,
            ReleaseDate = x.Release,
            IsUpcoming = x.Upcoming,
            IsActive = true,
            LogoUrl = $"https://placehold.co/320x120?text={Uri.EscapeDataString(x.Name)}",
            SymbolUrl = $"https://placehold.co/64x64?text={Uri.EscapeDataString(x.Code)}",
            CreatedUtc = now,
            UpdatedUtc = now
        }).ToList();
    }

    private static List<CatalogProduct> BuildCatalogProducts(IReadOnlyList<CardSet> sets, DateTime now)
    {
        CardSet Set(string slug) => sets.Single(x => x.Slug == slug);
        var rows = new List<CatalogProduct>();
        var i = 1;
        void Add(Guid gameId, string setSlug, Guid categoryId, string name, string type, string? number, string? rarity, bool featured, bool trending, bool isSealed = false)
        {
            var set = Set(setSlug);
            rows.Add(new CatalogProduct
            {
                Id = Guid.Parse($"54000000-0000-0000-0000-{i:000000000000}"),
                GameId = gameId,
                CardSetId = set.Id,
                ProductCategoryId = categoryId,
                Name = name,
                Slug = Slug(name),
                ProductType = type,
                CardNumber = number,
                Rarity = rarity,
                Description = $"{name} catalog product.",
                ImageUrl = Image(gameId, name),
                ReleaseDate = set.ReleaseDate,
                IsSealed = isSealed,
                IsSingleCard = type == "SingleCard",
                IsGradedEligible = type == "SingleCard",
                IsActive = true,
                IsFeatured = featured,
                IsTrending = trending,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            i++;
        }

        Add(MagicGameId, "magic-the-gathering-final-fantasy", RawSinglesCategoryId, "Black Lotus", "SingleCard", "232", "Rare", true, true);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Sol Ring", "SingleCard", "276", "Uncommon", true, true);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Lightning Bolt", "SingleCard", "146", "Common", true, true);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Counterspell", "SingleCard", "67", "Common", true, false);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Mox Diamond", "SingleCard", "138", "Rare", true, true);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Mana Crypt", "SingleCard", "225", "Mythic Rare", true, true);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Rhystic Study", "SingleCard", "45", "Common", true, true);
        Add(MagicGameId, "tarkir-dragonstorm", RawSinglesCategoryId, "Dockside Extortionist", "SingleCard", "24", "Rare", true, true);
        Add(MagicGameId, "final-fantasy", BoosterPacksCategoryId, "Final Fantasy Booster Pack", "BoosterPack", null, null, true, true, true);
        Add(MagicGameId, "final-fantasy", BoosterBoxesCategoryId, "Final Fantasy Booster Box", "BoosterBox", null, null, true, true, true);
        Add(MagicGameId, "tarkir-dragonstorm", CommanderDecksCategoryId, "Commander Deck Sample", "CommanderDeck", null, null, true, false, true);

        Add(PokemonGameId, "prismatic-evolutions", RawSinglesCategoryId, "Charizard", "SingleCard", "4/102", "Rare Holo", true, true);
        Add(PokemonGameId, "prismatic-evolutions", RawSinglesCategoryId, "Pikachu", "SingleCard", "60/64", "Common", true, true);
        Add(PokemonGameId, "prismatic-evolutions", RawSinglesCategoryId, "Blastoise", "SingleCard", "2/102", "Rare Holo", true, true);
        Add(PokemonGameId, "prismatic-evolutions", RawSinglesCategoryId, "Mewtwo", "SingleCard", "10/102", "Rare Holo", true, false);
        Add(PokemonGameId, "prismatic-evolutions", RawSinglesCategoryId, "Gengar", "SingleCard", "5/62", "Rare Holo", true, true);
        Add(PokemonGameId, "destined-rivals", RawSinglesCategoryId, "Rayquaza", "SingleCard", "22/107", "Rare Holo", true, true);
        Add(PokemonGameId, "destined-rivals", RawSinglesCategoryId, "Lugia", "SingleCard", "9/111", "Rare Holo", true, true);
        Add(PokemonGameId, "destined-rivals", RawSinglesCategoryId, "Umbreon", "SingleCard", "13/75", "Rare Holo", true, true);
        Add(PokemonGameId, "prismatic-evolutions", BoosterPacksCategoryId, "Prismatic Evolutions Booster Pack", "BoosterPack", null, null, true, true, true);
        Add(PokemonGameId, "prismatic-evolutions", EliteTrainerBoxesCategoryId, "Prismatic Evolutions Elite Trainer Box", "EliteTrainerBox", null, null, true, true, true);
        Add(PokemonGameId, "destined-rivals", BoosterPacksCategoryId, "Destined Rivals Booster Pack", "BoosterPack", null, null, true, false, true);
        Add(PokemonGameId, "chaos-rising", EliteTrainerBoxesCategoryId, "Chaos Rising Elite Trainer Box", "EliteTrainerBox", null, null, true, true, true);

        Add(OnePieceGameId, "romance-dawn", RawSinglesCategoryId, "Monkey D. Luffy Sample Card", "SingleCard", "OP01-001", "Leader", true, true);
        Add(OnePieceGameId, "romance-dawn", RawSinglesCategoryId, "Roronoa Zoro Sample Card", "SingleCard", "OP01-025", "Super Rare", true, true);
        Add(OnePieceGameId, "romance-dawn", BoosterPacksCategoryId, "Romance Dawn Booster Pack", "BoosterPack", null, null, true, true, true);
        Add(OnePieceGameId, "paramount-war", BoosterBoxesCategoryId, "Paramount War Booster Box", "BoosterBox", null, null, true, true, true);
        Add(OnePieceGameId, "500-years-in-the-future", BoosterPacksCategoryId, "500 Years in the Future Booster Pack", "BoosterPack", null, null, true, false, true);
        return rows;
    }

    private static string Slug(string value) => value.ToLowerInvariant().Replace(":", "").Replace("!", "").Replace(".", "").Replace(" ", "-");

    private static string Image(Guid gameId, string name)
    {
        if (gameId == MagicGameId && !name.Contains("Pack") && !name.Contains("Box") && !name.Contains("Deck"))
        {
            return $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}&format=image&version=normal";
        }

        return $"https://placehold.co/245x342?text={Uri.EscapeDataString(name)}";
    }
}
