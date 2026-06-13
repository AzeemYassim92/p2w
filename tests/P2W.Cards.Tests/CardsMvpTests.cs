using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.Common;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Tests;

public sealed class CardsMvpTests
{
    [Fact]
    public async Task CardSearch_Returns_Matching_Seeded_Cards()
    {
        await using var db = CreateDb();
        var service = new CardSearchService(db);
        var results = await service.SearchCardsAsync("charizard", null, CancellationToken.None);
        Assert.Contains(results, c => c.Name == "Charizard");
    }

    [Fact]
    public async Task CardSearch_Game_Filter_Works_For_Pokemon()
    {
        await using var db = CreateDb();
        var service = new CardSearchService(db);
        var results = await service.SearchCardsAsync("charizard", "Pokemon", CancellationToken.None);
        Assert.All(results, c => Assert.Equal("Pokemon", c.Game));
    }

    [Fact]
    public async Task CardSearch_Game_Filter_Works_For_Magic()
    {
        await using var db = CreateDb();
        var service = new CardSearchService(db);
        var results = await service.SearchCardsAsync("sol", "Magic", CancellationToken.None);
        Assert.All(results, c => Assert.Equal("Magic", c.Game));
    }

    [Fact]
    public async Task Featured_Marketplace_Products_Returns_Ten_Individual_Cards_With_Tcgplayer_Source()
    {
        await using var db = CreateDb();
        var service = new CardSearchService(db);
        var results = await service.GetFeaturedMarketplaceProductsAsync("individual-cards", 10, CancellationToken.None);
        Assert.Equal(10, results.Count);
        Assert.All(results, product =>
        {
            Assert.Equal("Individual Cards", product.ProductType);
            Assert.Equal("TCGplayer", product.SourceName);
            Assert.StartsWith("https://www.tcgplayer.com/search/all/product", product.SourceUrl);
            Assert.True(product.Price > 0);
        });
    }

    [Fact]
    public async Task Mock_Marketplace_Provider_Returns_Listings()
    {
        var provider = MockListingProvider();
        var listings = await provider.SearchListingsAsync(new CardSearchContext { CardName = "Charizard", Game = "Pokemon" }, CancellationToken.None);
        Assert.True(listings.Count >= 3);
    }

    [Fact]
    public async Task Listing_Refresh_Inserts_Listings()
    {
        await using var db = CreateDb();
        var service = CreateListingService(db);
        var cardId = await CardId(db, "Charizard");
        await service.RefreshListingsForCardAsync(cardId, CancellationToken.None);
        Assert.True(await db.Listings.CountAsync(l => l.CardId == cardId) >= 3);
    }

    [Fact]
    public async Task Listing_Refresh_Updates_Duplicates()
    {
        await using var db = CreateDb();
        var service = CreateListingService(db);
        var cardId = await CardId(db, "Charizard");
        await service.RefreshListingsForCardAsync(cardId, CancellationToken.None);
        await service.RefreshListingsForCardAsync(cardId, CancellationToken.None);
        Assert.Equal(3, await db.Listings.CountAsync(l => l.CardId == cardId));
    }

    [Theory]
    [InlineData("NM", "Near Mint")]
    [InlineData("PSA 10", "Graded")]
    public void Condition_Normalizer_Maps_Values(string raw, string expected)
    {
        Assert.Equal(expected, new ConditionNormalizer().Normalize(raw));
    }

    [Fact]
    public void Price_Snapshot_Median_Odd()
    {
        Assert.Equal(3m, PriceHistoryService.CalculateMedian(new[] { 1m, 3m, 5m }));
    }

    [Fact]
    public void Price_Snapshot_Median_Even()
    {
        Assert.Equal(4m, PriceHistoryService.CalculateMedian(new[] { 1m, 3m, 5m, 7m }));
    }

    [Fact]
    public async Task Price_Snapshot_Calculates_Lowest_And_Average()
    {
        await using var db = CreateDb();
        var cardId = await CardId(db, "Sol Ring");
        await CreateListingService(db).RefreshListingsForCardAsync(cardId, CancellationToken.None);
        var service = new PriceHistoryService(db, Registry(), Options.Create(new CardOptions()));
        await service.CaptureListingSnapshotForCardAsync(cardId, CancellationToken.None);
        var snapshot = await db.PriceSnapshots.FirstAsync(p => p.CardId == cardId);
        Assert.Equal(4.99m, snapshot.LowestPrice);
        Assert.True(snapshot.AveragePrice > snapshot.LowestPrice);
    }

    [Fact]
    public async Task Watchlist_Add_Works_And_Prevents_Duplicate()
    {
        await using var db = CreateDb();
        var service = new WatchlistService(db);
        var cardId = await CardId(db, "Pikachu");
        var userId = Guid.NewGuid();
        await service.AddToWatchlistAsync(userId, new AddWatchlistItemRequest { CardId = cardId }, CancellationToken.None);
        await service.AddToWatchlistAsync(userId, new AddWatchlistItemRequest { CardId = cardId }, CancellationToken.None);
        Assert.Equal(1, await db.WatchlistItems.CountAsync(w => w.UserId == userId && w.CardId == cardId));
    }

    [Fact]
    public async Task Watchlist_Remove_Works()
    {
        await using var db = CreateDb();
        var service = new WatchlistService(db);
        var cardId = await CardId(db, "Pikachu");
        var userId = Guid.NewGuid();
        var item = await service.AddToWatchlistAsync(userId, new AddWatchlistItemRequest { CardId = cardId }, CancellationToken.None);
        await service.RemoveFromWatchlistAsync(userId, item.WatchlistItemId, CancellationToken.None);
        Assert.Empty(await service.GetUserWatchlistAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task Price_Alert_Triggers_When_Lowest_Is_At_Target()
    {
        await using var db = CreateDb();
        var cardId = await CardId(db, "Sol Ring");
        await CreateListingService(db).RefreshListingsForCardAsync(cardId, CancellationToken.None);
        var service = new PriceAlertService(db, NullLogger<PriceAlertService>.Instance);
        var alert = await service.CreateAlertAsync(Guid.NewGuid(), new CreatePriceAlertRequest { CardId = cardId, TargetPrice = 10m }, CancellationToken.None);
        Assert.True(alert.HasTriggered);
    }

    [Fact]
    public async Task Disabled_Provider_Returns_Empty_Result()
    {
        var provider = new DisabledListingProvider("eBay", false, NullLogger.Instance);
        var result = await provider.SearchListingsAsync(new CardSearchContext { CardName = "Charizard" }, CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public void Provider_Registry_Returns_Enabled_Providers()
    {
        var registry = Registry();
        Assert.Contains(registry.AllProviders, p => p.SourceName == "MockMarket" && p.IsEnabled);
    }

    private static async Task<Guid> CardId(CardsDbContext db, string name) => await db.Cards.Where(c => c.Name == name).Select(c => c.Id).FirstAsync();

    private static CardsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CardsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static MockMarketplaceListingProvider MockListingProvider()
        => new(Options.Create(new MockProviderOptions { Enabled = true }), NullLogger<MockMarketplaceListingProvider>.Instance);

    private static IDataProviderRegistry Registry()
        => new DataProviderRegistry(
            new ICardCatalogProvider[] { new MockCatalogProvider(Options.Create(new MockProviderOptions { Enabled = true }), NullLogger<MockCatalogProvider>.Instance) },
            new IMarketplaceListingProvider[] { MockListingProvider() },
            new IPriceReferenceProvider[] { new MockPriceReferenceProvider(Options.Create(new MockProviderOptions { Enabled = true }), NullLogger<MockPriceReferenceProvider>.Instance) });

    private static ListingService CreateListingService(CardsDbContext db)
        => new(db, Registry(), new ConditionNormalizer(), Options.Create(new CardOptions { EnableRawSourceJsonStorage = true }));
}
