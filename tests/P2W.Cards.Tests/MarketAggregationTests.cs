using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.Ebay;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Tests;

public sealed class MarketAggregationTests
{
    [Fact]
    public async Task Marketplace_Sources_Are_Seeded()
    {
        await using var db = CreateDb();
        var sourceNames = await db.MarketplaceSources.Select(s => s.Name).ToListAsync();
        Assert.Contains("eBay", sourceNames);
        Assert.Contains("PokemonTCG", sourceNames);
        Assert.Contains("PriceCharting", sourceNames);
        Assert.Contains("MockMarket", sourceNames);
    }

    [Fact]
    public void Ebay_Query_Builder_Uses_Game_Set_And_Negative_Keywords()
    {
        var product = new CatalogProduct
        {
            Name = "Charizard",
            CardNumber = "4/102",
            ProductType = "SingleCard",
            IsSingleCard = true,
            Game = new Game { Name = "Pokemon", Slug = "pokemon" },
            CardSet = new CardSet { Name = "Base Set", Code = "BS" }
        };

        var query = new EbaySearchQueryBuilder().Build(product);

        Assert.Contains("\"Charizard\"", query);
        Assert.Contains("\"Base Set\"", query);
        Assert.Contains("pokemon card", query);
        Assert.Contains("-proxy", query);
        Assert.Contains("-replica", query);
    }

    [Fact]
    public void Ebay_Normalizer_Excludes_Proxy_And_Computes_Effective_Price()
    {
        var product = new CatalogProduct
        {
            Name = "Charizard",
            CardNumber = "4/102",
            IsSingleCard = true,
            Game = new Game { Name = "Pokemon", Slug = "pokemon" },
            CardSet = new CardSet { Name = "Base Set", Code = "BS" }
        };
        var item = new EbayItemSummaryDto
        {
            ItemId = "listing-1",
            Title = "Pokemon Charizard 4/102 Base Set proxy",
            ItemWebUrl = "https://example.com/listing-1",
            Price = new EbayMoneyDto { Value = "20.00", Currency = "USD" },
            ShippingOptions = new() { new EbayShippingOptionDto { ShippingCost = new EbayMoneyDto { Value = "3.50", Currency = "USD" } } }
        };

        var listing = new EbayListingNormalizer().Normalize(item, product);

        Assert.Equal(23.50m, listing.EffectivePrice);
        Assert.True(listing.IsExcludedFromMarketValue);
        Assert.Contains("proxy", listing.ExclusionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Market_Summary_Returns_No_Data_State_When_No_Real_Snapshot_Exists()
    {
        await using var db = CreateDb();
        var productId = await db.CatalogProducts.Select(p => p.Id).FirstAsync();

        var summary = await new MarketSummaryService(db).GetSummaryAsync(productId, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.False(summary.HasMarketData);
        Assert.False(summary.IsDemoData);
        Assert.Null(summary.CurrentMarketPrice);
        Assert.Equal("Needs refresh", summary.DataStatus);
    }

    [Fact]
    public async Task Mock_Market_Refresh_Creates_Listings_Snapshot_And_Metric()
    {
        await using var db = CreateDb();
        var productId = await db.CatalogProducts.Select(p => p.Id).FirstAsync();
        var mock = new MockMarketDataProvider();
        var metrics = new MarketMetricsService(db, Options.Create(new MarketFeesOptions()));
        var diagnostics = new MarketDiagnosticTrail(
            Options.Create(new MarketDiagnosticsOptions()),
            NullLogger<MarketDiagnosticTrail>.Instance);
        var service = new MarketAggregationService(
            db,
            new IMarketplaceReferencePriceProvider[] { mock },
            new IMarketplaceActiveListingProvider[] { mock },
            new IMarketplaceSoldCompsProvider[] { mock },
            metrics,
            diagnostics);

        var result = await service.RefreshProductMarketDataAsync(productId, new MarketRefreshRequest { UseMockData = true }, CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.True(await db.CatalogMarketplaceListings.AnyAsync(l => l.CatalogProductId == productId));
        Assert.True(await db.CatalogMarketplaceSales.AnyAsync(s => s.CatalogProductId == productId));
        Assert.True(await db.CatalogMarketPriceSnapshots.AnyAsync(s => s.CatalogProductId == productId));
        Assert.True(await db.CatalogMarketMetrics.AnyAsync(m => m.CatalogProductId == productId));
    }

    private static CardsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CardsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
