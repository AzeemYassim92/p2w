using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.Common;

namespace P2W.Cards.Infrastructure.Services;

public sealed class CardSearchService(CardsDbContext db) : ICardSearchService
{
    private static readonly Dictionary<string, decimal> DemoTcgPlayerPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Charizard"] = 420.00m,
        ["Pikachu"] = 28.50m,
        ["Blastoise"] = 165.00m,
        ["Mewtwo"] = 72.00m,
        ["Gengar"] = 88.00m,
        ["Rayquaza"] = 145.00m,
        ["Lugia"] = 210.00m,
        ["Umbreon"] = 235.00m,
        ["Black Lotus"] = 130000.00m,
        ["Sol Ring"] = 3.25m,
        ["Lightning Bolt"] = 2.10m,
        ["Counterspell"] = 1.75m,
        ["Mox Diamond"] = 625.00m,
        ["Mana Crypt"] = 195.00m,
        ["Rhystic Study"] = 38.00m,
        ["Dockside Extortionist"] = 69.00m
    };

    public async Task<IReadOnlyList<MarketplaceProductDto>> GetFeaturedMarketplaceProductsAsync(string? productType, int take, CancellationToken ct)
    {
        var normalizedType = NormalizeProductType(productType);
        if (normalizedType != "Individual Cards")
        {
            return Array.Empty<MarketplaceProductDto>();
        }

        var limit = Math.Clamp(take, 1, 50);
        var cards = await db.Cards
            .OrderBy(c => c.Game)
            .ThenBy(c => c.Name)
            .Take(limit)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Game,
                c.SetName,
                c.CardNumber,
                c.Rarity,
                c.ImageUrl
            })
            .ToListAsync(ct);

        return cards.Select(c => new MarketplaceProductDto
        {
            CardId = c.Id,
            ProductType = normalizedType,
            Name = c.Name,
            Game = c.Game,
            SetName = c.SetName,
            CardNumber = c.CardNumber,
            Rarity = c.Rarity,
            ImageUrl = ResolveImageUrl(c.Game, c.Name, c.ImageUrl),
            Price = DemoTcgPlayerPrices.GetValueOrDefault(c.Name, 9.99m),
            Currency = "USD",
            SourceName = "TCGplayer",
            SourceUrl = $"https://www.tcgplayer.com/search/all/product?q={Uri.EscapeDataString(c.Name)}"
        }).ToArray();
    }

    public async Task<IReadOnlyList<CardSearchResultDto>> SearchCardsAsync(string query, string? game, CancellationToken ct)
    {
        var normalizedGame = NormalizeGame(game);
        var normalizedQuery = query.ToLowerInvariant();
        var cards = await db.Cards
            .Where(c => c.Name.ToLower().Contains(normalizedQuery) || c.SetName.ToLower().Contains(normalizedQuery))
            .Where(c => normalizedGame == null || c.Game == normalizedGame)
            .OrderBy(c => c.Game).ThenBy(c => c.Name)
            .Take(25)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Game,
                c.SetName,
                c.CardNumber,
                c.Rarity,
                c.ImageUrl,
                LowestListingPrice = db.Listings.Where(l => l.CardId == c.Id && l.IsActive).Select(l => (decimal?)(l.Price + (l.ShippingPrice ?? 0))).Min(),
                MarketReferencePrice = db.PriceReferenceSnapshots.Where(p => p.CardId == c.Id).OrderByDescending(p => p.CapturedAtUtc).Select(p => p.MarketPrice).FirstOrDefault()
            })
            .ToListAsync(ct);
        return cards.Select(c => new CardSearchResultDto
        {
            CardId = c.Id,
            Name = c.Name,
            Game = c.Game,
            SetName = c.SetName,
            CardNumber = c.CardNumber,
            Rarity = c.Rarity,
            ImageUrl = ResolveImageUrl(c.Game, c.Name, c.ImageUrl),
            LowestListingPrice = c.LowestListingPrice,
            MarketReferencePrice = c.MarketReferencePrice
        }).ToArray();
    }

    public async Task<CardDetailDto?> GetCardDetailAsync(Guid cardId, CancellationToken ct)
    {
        var card = await db.Cards
            .Where(c => c.Id == cardId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Game,
                c.SetName,
                c.SetCode,
                c.CardNumber,
                c.Rarity,
                c.Artist,
                c.ImageUrl,
                Variants = c.Variants.Select(v => new CardVariantDto
                {
                    CardVariantId = v.Id,
                    VariantName = v.VariantName,
                    Language = v.Language,
                    IsFoil = v.IsFoil,
                    IsReverseHolo = v.IsReverseHolo,
                    IsFirstEdition = v.IsFirstEdition,
                    IsGraded = v.IsGraded,
                    GradingCompany = v.GradingCompany,
                    Grade = v.Grade
                }).ToArray()
            })
            .FirstOrDefaultAsync(ct);

        return card == null
            ? null
            : new CardDetailDto
            {
                CardId = card.Id,
                Name = card.Name,
                Game = card.Game,
                SetName = card.SetName,
                SetCode = card.SetCode,
                CardNumber = card.CardNumber,
                Rarity = card.Rarity,
                Artist = card.Artist,
                ImageUrl = ResolveImageUrl(card.Game, card.Name, card.ImageUrl),
                Variants = card.Variants
            };
    }

    public static string? NormalizeGame(string? game)
    {
        if (string.IsNullOrWhiteSpace(game) || game.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (game.Equals("Pokemon", StringComparison.OrdinalIgnoreCase) || game.Equals("Pokémon", StringComparison.OrdinalIgnoreCase))
        {
            return "Pokemon";
        }

        if (game.Equals("Magic", StringComparison.OrdinalIgnoreCase))
        {
            return "Magic";
        }

        throw new ArgumentException("Invalid game. Use Pokemon or Magic.", nameof(game));
    }

    private static string NormalizeProductType(string? productType)
    {
        if (string.IsNullOrWhiteSpace(productType) || productType.Equals("individual", StringComparison.OrdinalIgnoreCase) || productType.Equals("individual-cards", StringComparison.OrdinalIgnoreCase))
        {
            return "Individual Cards";
        }

        if (productType.Equals("packs", StringComparison.OrdinalIgnoreCase) || productType.Equals("packs-of-cards", StringComparison.OrdinalIgnoreCase))
        {
            return "Packs of Cards";
        }

        if (productType.Equals("boxes", StringComparison.OrdinalIgnoreCase) || productType.Equals("boxes-of-cards", StringComparison.OrdinalIgnoreCase))
        {
            return "Boxes of Cards";
        }

        return "Individual Cards";
    }

    private static string ResolveImageUrl(string game, string name, string? fallback)
    {
        if (game.Equals("Magic", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}&format=image&version=normal";
        }

        if (PokemonTcgImageIds.TryGetValue(name, out var pokemonId))
        {
            return $"https://images.pokemontcg.io/{pokemonId}_hires.png";
        }

        return fallback ?? $"https://placehold.co/245x342?text={Uri.EscapeDataString(name)}";
    }

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
}

public sealed class ListingService(CardsDbContext db, IDataProviderRegistry providers, ConditionNormalizer conditionNormalizer, IOptions<CardOptions> options) : IListingService
{
    public async Task<IReadOnlyList<ListingDto>> GetListingsForCardAsync(Guid cardId, CancellationToken ct)
    {
        return await db.Listings
            .Include(l => l.Marketplace)
            .Where(l => l.CardId == cardId && l.IsActive)
            .OrderBy(l => l.Price + (l.ShippingPrice ?? 0))
            .Select(l => ToDto(l))
            .ToListAsync(ct);
    }

    public async Task RefreshListingsForCardAsync(Guid cardId, CancellationToken ct)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, ct) ?? throw new KeyNotFoundException("Card not found");
        var marketplace = await db.Marketplaces.FirstAsync(m => m.Name == "MockMarket", ct);
        var context = new CardSearchContext { CardId = card.Id, CardName = card.Name, Game = card.Game, SetName = card.SetName, SetCode = card.SetCode, CardNumber = card.CardNumber };

        foreach (var provider in providers.MarketplaceListingProviders.Where(p => p.IsEnabled))
        {
            var incoming = await provider.SearchListingsAsync(context, ct);
            foreach (var item in incoming)
            {
                var existing = await db.Listings.FirstOrDefaultAsync(l => l.MarketplaceId == marketplace.Id && l.ExternalListingId == item.ExternalListingId, ct);
                if (existing == null)
                {
                    existing = new Listing { Id = Guid.NewGuid(), CardId = card.Id, MarketplaceId = marketplace.Id, SourceName = item.SourceName, ExternalListingId = item.ExternalListingId };
                    db.Listings.Add(existing);
                }

                existing.Title = item.Title;
                existing.Price = item.Price;
                existing.ShippingPrice = item.ShippingPrice;
                existing.Currency = item.Currency;
                existing.RawCondition = item.Condition;
                existing.Condition = conditionNormalizer.Normalize(item.Condition);
                existing.ListingUrl = item.ListingUrl;
                existing.ImageUrl = item.ImageUrl;
                existing.IsAuction = item.IsAuction;
                existing.AuctionEndsUtc = item.AuctionEndsUtc;
                existing.ListedAtUtc = item.ListedAtUtc == default ? DateTime.UtcNow : item.ListedAtUtc;
                existing.CapturedAtUtc = DateTime.UtcNow;
                existing.IsActive = true;
                existing.RawSourceJson = options.Value.EnableRawSourceJsonStorage ? item.RawSourceJson : null;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static ListingDto ToDto(Listing l) => new()
    {
        ListingId = l.Id,
        MarketplaceName = l.Marketplace?.Name ?? "",
        SourceName = l.SourceName,
        Title = l.Title,
        Price = l.Price,
        ShippingPrice = l.ShippingPrice,
        EffectivePrice = l.Price + (l.ShippingPrice ?? 0),
        Currency = l.Currency,
        Condition = l.Condition,
        RawCondition = l.RawCondition,
        ListingUrl = l.ListingUrl,
        ImageUrl = l.ImageUrl,
        IsAuction = l.IsAuction,
        AuctionEndsUtc = l.AuctionEndsUtc,
        CapturedAtUtc = l.CapturedAtUtc
    };
}

public sealed class PriceHistoryService(CardsDbContext db, IDataProviderRegistry providers, IOptions<CardOptions> options) : IPriceHistoryService
{
    public async Task<IReadOnlyList<PriceSnapshotDto>> GetListingPriceHistoryAsync(Guid cardId, CancellationToken ct)
    {
        return await db.PriceSnapshots.Include(p => p.Marketplace)
            .Where(p => p.CardId == cardId)
            .OrderByDescending(p => p.CapturedAtUtc)
            .Select(p => new PriceSnapshotDto
            {
                CardId = p.CardId,
                SourceName = p.Marketplace!.Name,
                LowestPrice = p.LowestPrice,
                AveragePrice = p.AveragePrice,
                MedianPrice = p.MedianPrice,
                ListingCount = p.ListingCount,
                Currency = p.Currency,
                CapturedAtUtc = p.CapturedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PriceReferenceSnapshotDto>> GetReferencePriceHistoryAsync(Guid cardId, CancellationToken ct)
    {
        return await db.PriceReferenceSnapshots
            .Where(p => p.CardId == cardId)
            .OrderByDescending(p => p.CapturedAtUtc)
            .Select(p => new PriceReferenceSnapshotDto
            {
                CardId = p.CardId,
                SourceName = p.SourceName,
                MarketPrice = p.MarketPrice,
                UngradedPrice = p.UngradedPrice,
                Grade7Price = p.Grade7Price,
                Grade8Price = p.Grade8Price,
                Grade9Price = p.Grade9Price,
                Grade10Price = p.Grade10Price,
                BuylistPrice = p.BuylistPrice,
                RetailPrice = p.RetailPrice,
                Currency = p.Currency,
                CapturedAtUtc = p.CapturedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task CaptureListingSnapshotForCardAsync(Guid cardId, CancellationToken ct)
    {
        var marketplace = await db.Marketplaces.FirstAsync(m => m.Name == "MockMarket", ct);
        var prices = await db.Listings
            .Where(l => l.CardId == cardId && l.IsActive)
            .Select(l => l.Price + (l.ShippingPrice ?? 0))
            .OrderBy(x => x)
            .ToListAsync(ct);

        if (prices.Count == 0)
        {
            return;
        }

        db.PriceSnapshots.Add(new PriceSnapshot
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            MarketplaceId = marketplace.Id,
            LowestPrice = prices.First(),
            AveragePrice = Math.Round(prices.Average(), 2),
            MedianPrice = CalculateMedian(prices),
            ListingCount = prices.Count,
            Currency = "USD",
            CapturedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task RefreshReferencePricesForCardAsync(Guid cardId, CancellationToken ct)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, ct) ?? throw new KeyNotFoundException("Card not found");
        var context = new CardSearchContext { CardId = card.Id, CardName = card.Name, Game = card.Game, SetName = card.SetName, SetCode = card.SetCode, CardNumber = card.CardNumber };

        foreach (var provider in providers.PriceReferenceProviders.Where(p => p.IsEnabled))
        {
            var references = await provider.GetPriceReferencesAsync(context, ct);
            db.PriceReferenceSnapshots.AddRange(references.Select(r => new PriceReferenceSnapshot
            {
                Id = Guid.NewGuid(),
                CardId = cardId,
                SourceName = r.SourceName,
                MarketPrice = r.MarketPrice,
                LowPrice = r.LowPrice,
                MidPrice = r.MidPrice,
                HighPrice = r.HighPrice,
                UngradedPrice = r.UngradedPrice,
                Grade7Price = r.Grade7Price,
                Grade8Price = r.Grade8Price,
                Grade9Price = r.Grade9Price,
                Grade10Price = r.Grade10Price,
                BuylistPrice = r.BuylistPrice,
                RetailPrice = r.RetailPrice,
                Currency = r.Currency,
                RawSourceJson = options.Value.EnableRawSourceJsonStorage ? r.RawSourceJson : null,
                CapturedAtUtc = DateTime.UtcNow
            }));
        }

        await db.SaveChangesAsync(ct);
    }

    public static decimal CalculateMedian(IReadOnlyList<decimal> sortedPrices)
    {
        if (sortedPrices.Count == 0)
        {
            return 0;
        }

        var middle = sortedPrices.Count / 2;
        return sortedPrices.Count % 2 == 1
            ? sortedPrices[middle]
            : Math.Round((sortedPrices[middle - 1] + sortedPrices[middle]) / 2, 2);
    }
}

public sealed class WatchlistService(CardsDbContext db) : IWatchlistService
{
    public async Task<IReadOnlyList<WatchlistItemDto>> GetUserWatchlistAsync(Guid userId, CancellationToken ct)
    {
        return await db.WatchlistItems.Include(w => w.Card)
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Card!.Name)
            .Select(w => ToDto(w, db.Listings.Where(l => l.CardId == w.CardId && l.IsActive).Select(l => (decimal?)(l.Price + (l.ShippingPrice ?? 0))).Min()))
            .ToListAsync(ct);
    }

    public async Task<WatchlistItemDto> AddToWatchlistAsync(Guid userId, AddWatchlistItemRequest request, CancellationToken ct)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, ct) ?? throw new KeyNotFoundException("Card not found");
        var existing = await db.WatchlistItems.Include(w => w.Card).FirstOrDefaultAsync(w => w.UserId == userId && w.CardId == request.CardId && w.CardVariantId == request.CardVariantId, ct);
        if (existing != null)
        {
            return ToDto(existing, await CurrentLowestAsync(existing.CardId, ct));
        }

        var item = new WatchlistItem { Id = Guid.NewGuid(), UserId = userId, CardId = request.CardId, CardVariantId = request.CardVariantId, TargetPrice = request.TargetPrice, Notes = request.Notes, CreatedUtc = DateTime.UtcNow, Card = card };
        db.WatchlistItems.Add(item);
        await db.SaveChangesAsync(ct);
        return ToDto(item, await CurrentLowestAsync(item.CardId, ct));
    }

    public async Task RemoveFromWatchlistAsync(Guid userId, Guid watchlistItemId, CancellationToken ct)
    {
        var item = await db.WatchlistItems.FirstOrDefaultAsync(w => w.Id == watchlistItemId && w.UserId == userId, ct);
        if (item != null)
        {
            db.WatchlistItems.Remove(item);
            await db.SaveChangesAsync(ct);
        }
    }

    private Task<decimal?> CurrentLowestAsync(Guid cardId, CancellationToken ct)
        => db.Listings.Where(l => l.CardId == cardId && l.IsActive).Select(l => (decimal?)(l.Price + (l.ShippingPrice ?? 0))).MinAsync(ct);

    private static WatchlistItemDto ToDto(WatchlistItem item, decimal? currentLowestPrice) => new()
    {
        WatchlistItemId = item.Id,
        CardId = item.CardId,
        CardName = item.Card?.Name ?? "",
        Game = item.Card?.Game ?? "",
        SetName = item.Card?.SetName ?? "",
        ImageUrl = item.Card?.ImageUrl,
        TargetPrice = item.TargetPrice,
        CurrentLowestPrice = currentLowestPrice,
        Notes = item.Notes,
        CreatedUtc = item.CreatedUtc
    };
}

public sealed class PriceAlertService(CardsDbContext db, ILogger<PriceAlertService> logger) : IPriceAlertService
{
    public async Task<IReadOnlyList<PriceAlertDto>> GetUserAlertsAsync(Guid userId, CancellationToken ct)
        => await db.PriceAlerts.Include(a => a.Card).Where(a => a.UserId == userId).OrderByDescending(a => a.CreatedUtc).Select(a => ToDto(a)).ToListAsync(ct);

    public async Task<PriceAlertDto> CreateAlertAsync(Guid userId, CreatePriceAlertRequest request, CancellationToken ct)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, ct) ?? throw new KeyNotFoundException("Card not found");
        var alert = new PriceAlert { Id = Guid.NewGuid(), UserId = userId, CardId = request.CardId, CardVariantId = request.CardVariantId, TargetPrice = request.TargetPrice, IsActive = true, CreatedUtc = DateTime.UtcNow, Card = card };
        db.PriceAlerts.Add(alert);
        await db.SaveChangesAsync(ct);
        await CheckAlertsForCardAsync(request.CardId, ct);
        return ToDto(alert);
    }

    public async Task DisableAlertAsync(Guid userId, Guid alertId, CancellationToken ct)
    {
        var alert = await db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, ct);
        if (alert != null)
        {
            alert.IsActive = false;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task CheckAlertsForCardAsync(Guid cardId, CancellationToken ct)
    {
        var lowest = await db.Listings.Where(l => l.CardId == cardId && l.IsActive).Select(l => (decimal?)(l.Price + (l.ShippingPrice ?? 0))).MinAsync(ct);
        if (lowest == null)
        {
            return;
        }

        var alerts = await db.PriceAlerts.Where(a => a.CardId == cardId && a.IsActive && !a.HasTriggered && lowest <= a.TargetPrice).ToListAsync(ct);
        foreach (var alert in alerts)
        {
            alert.HasTriggered = true;
            alert.TriggeredAtUtc = DateTime.UtcNow;
            logger.LogInformation("Price alert {AlertId} triggered for card {CardId} at {LowestPrice}", alert.Id, cardId, lowest);
        }

        await db.SaveChangesAsync(ct);
    }

    private static PriceAlertDto ToDto(PriceAlert alert) => new()
    {
        PriceAlertId = alert.Id,
        CardId = alert.CardId,
        CardName = alert.Card?.Name ?? "",
        TargetPrice = alert.TargetPrice,
        IsActive = alert.IsActive,
        HasTriggered = alert.HasTriggered,
        TriggeredAtUtc = alert.TriggeredAtUtc,
        CreatedUtc = alert.CreatedUtc
    };
}
