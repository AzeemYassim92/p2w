using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Domain.Enums;
using P2W.Cards.Infrastructure.Data;

namespace P2W.Cards.Infrastructure.Services;

public sealed class CatalogService(CardsDbContext db, IOptions<TcgPlayerOptions> tcgPlayerOptions, IOptions<EbayOptions> ebayOptions, IOptions<ScryfallOptions> scryfallOptions, IOptions<PokemonTcgOptions> pokemonOptions, IOptions<MtgJsonOptions> mtgJsonOptions, IOptions<PriceChartingOptions> priceChartingOptions, IOptions<CardKingdomOptions> cardKingdomOptions) : ICatalogService
{
    private static readonly Dictionary<string, decimal> DemoPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Black Lotus"] = 130000.00m,
        ["Sol Ring"] = 3.25m,
        ["Lightning Bolt"] = 2.10m,
        ["Counterspell"] = 1.75m,
        ["Mox Diamond"] = 625.00m,
        ["Mana Crypt"] = 195.00m,
        ["Rhystic Study"] = 38.00m,
        ["Dockside Extortionist"] = 69.00m,
        ["Charizard"] = 420.00m,
        ["Pikachu"] = 28.50m,
        ["Blastoise"] = 165.00m,
        ["Mewtwo"] = 72.00m,
        ["Gengar"] = 88.00m,
        ["Rayquaza"] = 145.00m,
        ["Lugia"] = 210.00m,
        ["Umbreon"] = 235.00m
    };

    public async Task<IReadOnlyList<GameDto>> GetGamesAsync(bool primaryOnly, CancellationToken ct)
        => await db.Games
            .Where(g => g.IsActive)
            .Where(g => !primaryOnly || g.IsPrimaryFocus)
            .OrderBy(g => g.DisplayOrder)
            .Select(g => ToDto(g))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CardSetDto>> GetSetsAsync(Guid? gameId, string? gameSlug, bool? upcoming, int take, CancellationToken ct)
    {
        var query = db.CardSets.Include(s => s.Game).Where(s => s.IsActive);
        query = ApplyGameFilter(query, gameId, gameSlug);
        if (upcoming.HasValue)
        {
            query = query.Where(s => s.IsUpcoming == upcoming.Value);
        }

        return await query
            .OrderByDescending(s => s.ReleaseDate)
            .Take(Math.Clamp(take, 1, 50))
            .Select(s => ToDto(s))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct)
    {
        var categories = await db.ProductCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ProductCategoryDto
            {
                ProductCategoryId = c.Id,
                ParentCategoryId = c.ParentCategoryId,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder
            })
            .ToListAsync(ct);

        return categories
            .Where(c => c.ParentCategoryId == null)
            .Select(c => WithChildren(c, categories))
            .ToList();
    }

    public async Task<IReadOnlyList<CatalogProductDto>> GetProductsAsync(CatalogProductQuery query, CancellationToken ct)
    {
        var products = db.CatalogProducts
            .Include(p => p.Game)
            .Include(p => p.CardSet)
            .Include(p => p.ProductCategory)
            .Where(p => p.IsActive);

        products = ApplyProductFilters(products, query);

        var rows = await products
            .OrderByDescending(p => p.IsTrending)
            .ThenByDescending(p => p.IsFeatured)
            .ThenBy(p => p.Name)
            .Take(Math.Clamp(query.Take, 1, 1000))
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        return rows.Select(AttachMarketHints).ToList();
    }

    public async Task<CatalogProductDetailDto?> GetProductDetailAsync(Guid productId, CancellationToken ct)
    {
        var product = await db.CatalogProducts
            .Include(p => p.Game)
            .Include(p => p.CardSet)
            .Include(p => p.ProductCategory)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, ct);

        if (product == null)
        {
            return null;
        }

        var mappings = await db.ExternalProductMappings
            .Where(m => m.CatalogProductId == productId)
            .OrderBy(m => m.SourceName)
            .Select(m => new ExternalProductMappingDto
            {
                SourceName = m.SourceName,
                ExternalId = m.ExternalId,
                ExternalUrl = m.ExternalUrl,
                ExternalSlug = m.ExternalSlug,
                ConfidenceScore = m.ConfidenceScore
            })
            .ToListAsync(ct);

        var dto = new CatalogProductDetailDto
        {
            CatalogProductId = product.Id,
            GameId = product.GameId,
            GameName = product.Game?.Name ?? "",
            CardSetId = product.CardSetId,
            SetName = product.CardSet?.Name,
            SetCode = product.CardSet?.Code,
            ProductCategoryId = product.ProductCategoryId,
            CategoryName = product.ProductCategory?.Name ?? "",
            CategorySlug = product.ProductCategory?.Slug ?? "",
            Name = product.Name,
            Slug = product.Slug,
            ProductType = product.ProductType,
            CardNumber = product.CardNumber,
            Rarity = product.Rarity,
            Artist = product.Artist,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            ReleaseDate = product.ReleaseDate,
            IsSealed = product.IsSealed,
            IsSingleCard = product.IsSingleCard,
            IsGradedEligible = product.IsGradedEligible,
            IsFeatured = product.IsFeatured,
            IsTrending = product.IsTrending,
            Variants = product.Variants.OrderBy(v => v.VariantName).Select(ToDto).ToArray(),
            ExternalMappings = mappings
        };

        AttachMarketHints(dto);
        return dto;
    }

    public Task<IReadOnlyList<ProviderCapabilityDto>> GetProviderCapabilitiesAsync(CancellationToken ct)
    {
        IReadOnlyList<ProviderCapabilityDto> providers = new[]
        {
            new ProviderCapabilityDto { SourceName = "TCGplayer", SupportsMagic = true, SupportsPokemon = true, SupportsOnePiece = false, SupportsCatalogSearch = true, SupportsMarketplaceListings = true, SupportsPriceReference = true, IsConfigured = tcgPlayerOptions.Value.Enabled, Notes = "Future primary marketplace and price reference connector." },
            new ProviderCapabilityDto { SourceName = "Scryfall", SupportsMagic = true, SupportsPokemon = false, SupportsOnePiece = false, SupportsCatalogSearch = true, SupportsMarketplaceListings = false, SupportsPriceReference = false, IsConfigured = scryfallOptions.Value.Enabled, Notes = "Best fit for Magic card metadata and images." },
            new ProviderCapabilityDto { SourceName = "PokemonTCG", SupportsMagic = false, SupportsPokemon = true, SupportsOnePiece = false, SupportsCatalogSearch = true, SupportsMarketplaceListings = false, SupportsPriceReference = false, IsConfigured = pokemonOptions.Value.Enabled, Notes = "Best fit for Pokemon card metadata and images." },
            new ProviderCapabilityDto { SourceName = "eBay", SupportsMagic = true, SupportsPokemon = true, SupportsOnePiece = true, SupportsCatalogSearch = false, SupportsMarketplaceListings = true, SupportsPriceReference = false, IsConfigured = ebayOptions.Value.Enabled, Notes = "Future broad marketplace listing source." },
            new ProviderCapabilityDto { SourceName = "MTGJSON", SupportsMagic = true, SupportsPokemon = false, SupportsOnePiece = false, SupportsCatalogSearch = true, SupportsMarketplaceListings = false, SupportsPriceReference = true, IsConfigured = mtgJsonOptions.Value.Enabled, Notes = "Future Magic set/card import and price snapshots." },
            new ProviderCapabilityDto { SourceName = "PriceCharting", SupportsMagic = true, SupportsPokemon = true, SupportsOnePiece = false, SupportsCatalogSearch = false, SupportsMarketplaceListings = false, SupportsPriceReference = true, IsConfigured = priceChartingOptions.Value.Enabled, Notes = "Future collectible price reference source." },
            new ProviderCapabilityDto { SourceName = "Card Kingdom", SupportsMagic = true, SupportsPokemon = false, SupportsOnePiece = false, SupportsCatalogSearch = false, SupportsMarketplaceListings = false, SupportsPriceReference = true, IsConfigured = cardKingdomOptions.Value.Enabled, Notes = "Future Magic retail reference source." }
        };
        return Task.FromResult(providers);
    }

    internal static CatalogProductDto AttachMarketHints(CatalogProductDto dto)
    {
        dto.ImageUrl = ResolveImageUrl(dto.GameName, dto.Name, dto.ImageUrl);
        dto.EstimatedMarketPrice = DemoPrices.GetValueOrDefault(dto.Name, dto.IsSealed ? 49.99m : 9.99m);
        dto.PrimarySourceName = "TCGplayer";
        dto.PrimarySourceUrl = $"https://www.tcgplayer.com/search/all/product?q={Uri.EscapeDataString(dto.Name)}";
        return dto;
    }

    internal static string? ResolveImageUrl(string gameName, string productName, string? fallback)
    {
        if (gameName.Equals("Magic: The Gathering", StringComparison.OrdinalIgnoreCase) && !IsSealedProduct(productName))
        {
            return $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(productName)}&format=image&version=normal";
        }

        if (PokemonTcgImageIds.TryGetValue(productName, out var pokemonId))
        {
            return $"https://images.pokemontcg.io/{pokemonId}_hires.png";
        }

        if (string.IsNullOrWhiteSpace(fallback))
        {
            return $"https://placehold.co/245x342/e2e8f0/475569?text={Uri.EscapeDataString(productName)}";
        }

        return fallback;
    }

    private static bool IsSealedProduct(string productName)
        => productName.Contains("Pack", StringComparison.OrdinalIgnoreCase)
            || productName.Contains("Box", StringComparison.OrdinalIgnoreCase)
            || productName.Contains("Deck", StringComparison.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> PokemonTcgImageIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Charizard"] = "base1/4",
        ["Pikachu"] = "jungle/60",
        ["Blastoise"] = "base1/2",
        ["Mewtwo"] = "base1/10",
        ["Gengar"] = "fossil/5",
        ["Rayquaza"] = "ex8/22",
        ["Lugia"] = "neo1/9",
        ["Umbreon"] = "neo2/13"
    };

    internal static IQueryable<CatalogProduct> ApplyProductFilters(IQueryable<CatalogProduct> products, CatalogProductQuery query)
    {
        products = ApplyGameFilter(products, query.GameId, query.GameSlug);
        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.ProductCategoryId == query.CategoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            products = products.Where(p => p.ProductCategory != null && p.ProductCategory.Slug == query.CategorySlug);
        }
        if (query.CardSetId.HasValue)
        {
            products = products.Where(p => p.CardSetId == query.CardSetId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.ProductType))
        {
            products = products.Where(p => p.ProductType == query.ProductType);
        }
        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var search = query.Query.ToLower();
            products = products.Where(p => p.Name.ToLower().Contains(search) || (p.CardSet != null && p.CardSet.Name.ToLower().Contains(search)));
        }

        return products;
    }

    internal static IQueryable<T> ApplyGameFilter<T>(IQueryable<T> query, Guid? gameId, string? gameSlug) where T : class
    {
        if (typeof(T) == typeof(CardSet))
        {
            var sets = (IQueryable<CardSet>)query;
            if (gameId.HasValue)
            {
                sets = sets.Where(s => s.GameId == gameId.Value);
            }
            if (!string.IsNullOrWhiteSpace(gameSlug))
            {
                sets = sets.Where(s => s.Game != null && s.Game.Slug == gameSlug);
            }
            return (IQueryable<T>)sets;
        }

        var products = (IQueryable<CatalogProduct>)query;
        if (gameId.HasValue)
        {
            products = products.Where(p => p.GameId == gameId.Value);
        }
        if (!string.IsNullOrWhiteSpace(gameSlug))
        {
            products = products.Where(p => p.Game != null && p.Game.Slug == gameSlug);
        }
        return (IQueryable<T>)products;
    }

    internal static ProductCategoryDto WithChildren(ProductCategoryDto category, IReadOnlyList<ProductCategoryDto> all)
    {
        category.Children = all
            .Where(c => c.ParentCategoryId == category.ProductCategoryId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => WithChildren(c, all))
            .ToList();
        return category;
    }

    internal static GameDto ToDto(Game game) => new()
    {
        GameId = game.Id,
        Name = DisplayGameName(game.Name),
        Slug = game.Slug,
        Description = game.Description,
        IsPrimaryFocus = game.IsPrimaryFocus,
        IsActive = game.IsActive,
        DisplayOrder = game.DisplayOrder
    };

    internal static CardSetDto ToDto(CardSet set) => new()
    {
        CardSetId = set.Id,
        GameId = set.GameId,
        GameName = DisplayGameName(set.Game?.Name ?? ""),
        Name = set.Name,
        Slug = set.Slug,
        Code = set.Code,
        ReleaseDate = set.ReleaseDate,
        IsUpcoming = set.IsUpcoming,
        LogoUrl = set.LogoUrl,
        SymbolUrl = set.SymbolUrl
    };

    internal static CatalogProductDto ToDto(CatalogProduct product) => new()
    {
        CatalogProductId = product.Id,
        GameId = product.GameId,
        GameName = DisplayGameName(product.Game?.Name ?? ""),
        CardSetId = product.CardSetId,
        SetName = product.CardSet?.Name,
        SetCode = product.CardSet?.Code,
        ProductCategoryId = product.ProductCategoryId,
        CategoryName = product.ProductCategory?.Name ?? "",
        CategorySlug = product.ProductCategory?.Slug ?? "",
        Name = product.Name,
        Slug = product.Slug,
        ProductType = product.ProductType,
        CardNumber = product.CardNumber,
        Rarity = product.Rarity,
        ImageUrl = product.ImageUrl,
        ReleaseDate = product.ReleaseDate,
        IsSealed = product.IsSealed,
        IsSingleCard = product.IsSingleCard,
        IsFeatured = product.IsFeatured,
        IsTrending = product.IsTrending
    };

    private static ProductVariantDto ToDto(ProductVariant variant) => new()
    {
        ProductVariantId = variant.Id,
        VariantName = variant.VariantName,
        Language = variant.Language,
        IsFoil = variant.IsFoil,
        IsReverseHolo = variant.IsReverseHolo,
        IsFirstEdition = variant.IsFirstEdition,
        IsPromo = variant.IsPromo,
        IsSerialized = variant.IsSerialized,
        IsSealedCase = variant.IsSealedCase
    };

    private static string DisplayGameName(string gameName)
        => gameName.Equals("One Piece", StringComparison.OrdinalIgnoreCase) ? "One Piece TCG" : gameName;
}

public sealed class CatalogDiscoveryService(ICatalogService catalog) : ICatalogDiscoveryService
{
    public async Task<MarketplaceHomeDto> GetMarketplaceHomeAsync(string? gameSlug, CancellationToken ct)
    {
        var games = await catalog.GetGamesAsync(primaryOnly: true, ct);
        var categories = await catalog.GetCategoriesAsync(ct);
        var products = string.IsNullOrWhiteSpace(gameSlug)
            ? await GetBalancedPrimaryGameProductsAsync(games, ct)
            : await catalog.GetProductsAsync(new CatalogProductQuery { GameSlug = gameSlug, Take = 12 }, ct);
        var latestSets = await catalog.GetSetsAsync(null, gameSlug, upcoming: false, take: 6, ct);
        var upcomingSets = await catalog.GetSetsAsync(null, gameSlug, upcoming: true, take: 6, ct);
        var capabilities = await catalog.GetProviderCapabilitiesAsync(ct);

        return new MarketplaceHomeDto
        {
            PrimaryGames = games,
            Categories = categories,
            TrendingProducts = products.Where(p => p.IsTrending).Take(8).ToArray(),
            FeaturedProducts = products.Where(p => p.IsFeatured).Take(8).ToArray(),
            LatestSets = latestSets,
            UpcomingSets = upcomingSets,
            ProviderCapabilities = capabilities
        };
    }

    private async Task<IReadOnlyList<CatalogProductDto>> GetBalancedPrimaryGameProductsAsync(IReadOnlyList<GameDto> games, CancellationToken ct)
    {
        var products = new List<CatalogProductDto>();
        foreach (var game in games)
        {
            var gameProducts = await catalog.GetProductsAsync(new CatalogProductQuery { GameSlug = game.Slug, Take = 4 }, ct);
            products.AddRange(gameProducts);
        }

        return products
            .OrderByDescending(p => p.IsTrending)
            .ThenByDescending(p => p.IsFeatured)
            .ThenBy(p => p.GameName)
            .ThenBy(p => p.Name)
            .ToList();
    }
}

public sealed class SellerInventoryService(CardsDbContext db) : ISellerInventoryService
{
    public async Task<IReadOnlyList<SellerInventoryItemDto>> GetInventoryAsync(Guid sellerUserId, CancellationToken ct)
        => await db.SellerInventoryItems
            .Include(i => i.CatalogProduct).ThenInclude(p => p!.Game)
            .Include(i => i.CatalogProduct).ThenInclude(p => p!.CardSet)
            .Include(i => i.ProductVariant)
            .Include(i => i.Images)
            .Where(i => i.SellerUserId == sellerUserId)
            .OrderByDescending(i => i.CreatedUtc)
            .Select(i => ToDto(i))
            .ToListAsync(ct);

    public async Task<SellerInventoryItemDto> CreateInventoryItemAsync(Guid sellerUserId, CreateSellerInventoryItemRequest request, CancellationToken ct)
    {
        ValidateRequest(request);
        var productExists = await db.CatalogProducts.AnyAsync(p => p.Id == request.CatalogProductId && p.IsActive, ct);
        if (!productExists)
        {
            throw new KeyNotFoundException("Catalog product not found.");
        }

        if (request.ProductVariantId.HasValue)
        {
            var variantMatchesProduct = await db.ProductVariants.AnyAsync(v => v.Id == request.ProductVariantId.Value && v.CatalogProductId == request.CatalogProductId, ct);
            if (!variantMatchesProduct)
            {
                throw new ArgumentException("Product variant does not belong to the selected catalog product.", nameof(request));
            }
        }

        var now = DateTime.UtcNow;
        var item = new SellerInventoryItem
        {
            Id = Guid.NewGuid(),
            SellerUserId = sellerUserId,
            CatalogProductId = request.CatalogProductId,
            ProductVariantId = request.ProductVariantId,
            Condition = request.Condition,
            RawConditionNotes = request.RawConditionNotes,
            IsGraded = request.IsGraded,
            GradingCompany = request.GradingCompany,
            Grade = request.Grade,
            CertificationNumber = request.CertificationNumber,
            Quantity = request.Quantity,
            AskingPrice = request.AskingPrice,
            CostBasis = request.CostBasis,
            AcquiredAtUtc = request.AcquiredAtUtc,
            AcquisitionSource = request.AcquisitionSource,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.ToUpperInvariant(),
            IsAvailableForSale = request.IsAvailableForSale,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        var imageOrder = 1;
        foreach (var imageUrl in request.ImageUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().Take(8))
        {
            item.Images.Add(new SellerInventoryImage
            {
                Id = Guid.NewGuid(),
                ImageUrl = imageUrl,
                DisplayOrder = imageOrder++,
                CreatedUtc = now
            });
        }

        db.SellerInventoryItems.Add(item);
        await db.SaveChangesAsync(ct);

        return await db.SellerInventoryItems
            .Include(i => i.CatalogProduct).ThenInclude(p => p!.Game)
            .Include(i => i.CatalogProduct).ThenInclude(p => p!.CardSet)
            .Include(i => i.ProductVariant)
            .Include(i => i.Images)
            .Where(i => i.Id == item.Id)
            .Select(i => ToDto(i))
            .FirstAsync(ct);
    }

    private static void ValidateRequest(CreateSellerInventoryItemRequest request)
    {
        if (request.CatalogProductId == Guid.Empty)
        {
            throw new ArgumentException("Catalog product is required.", nameof(request));
        }
        if (request.Condition == ProductCondition.Unknown)
        {
            throw new ArgumentException("Condition is required.", nameof(request));
        }
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(request));
        }
        if (request.IsGraded && (request.Grade == null || string.IsNullOrWhiteSpace(request.GradingCompany)))
        {
            throw new ArgumentException("Graded inventory requires a grade and grading company.", nameof(request));
        }
        // Later: AI-assisted condition and grading can pre-fill these fields, but sellers must still own the final values.
    }

    private static SellerInventoryItemDto ToDto(SellerInventoryItem item) => new()
    {
        SellerInventoryItemId = item.Id,
        SellerUserId = item.SellerUserId,
        CatalogProductId = item.CatalogProductId,
        ProductName = item.CatalogProduct?.Name ?? "",
        GameName = item.CatalogProduct?.Game?.Name ?? "",
        SetName = item.CatalogProduct?.CardSet?.Name,
        ProductVariantId = item.ProductVariantId,
        VariantName = item.ProductVariant?.VariantName,
        Condition = item.Condition,
        RawConditionNotes = item.RawConditionNotes,
        IsGraded = item.IsGraded,
        GradingCompany = item.GradingCompany,
        Grade = item.Grade,
        CertificationNumber = item.CertificationNumber,
        Quantity = item.Quantity,
        AskingPrice = item.AskingPrice,
        CostBasis = item.CostBasis,
        AcquiredAtUtc = item.AcquiredAtUtc,
        AcquisitionSource = item.AcquisitionSource,
        Currency = item.Currency,
        IsAvailableForSale = item.IsAvailableForSale,
        ImageUrls = item.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToArray(),
        CreatedUtc = item.CreatedUtc,
        UpdatedUtc = item.UpdatedUtc
    };
}
