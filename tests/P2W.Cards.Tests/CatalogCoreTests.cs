using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Enums;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Tests;

public sealed class CatalogCoreTests
{
    [Fact]
    public async Task Catalog_Seeds_Primary_Games()
    {
        await using var db = CreateDb();
        var games = await db.Games.Where(g => g.IsPrimaryFocus).OrderBy(g => g.DisplayOrder).Select(g => g.Slug).ToListAsync();
        Assert.Equal(new[] { "magic-the-gathering", "pokemon", "one-piece" }, games);
    }

    [Fact]
    public async Task Catalog_Seeds_Core_Product_Categories()
    {
        await using var db = CreateDb();
        var slugs = await db.ProductCategories.Select(c => c.Slug).ToListAsync();
        Assert.Contains("booster-packs", slugs);
        Assert.Contains("booster-boxes", slugs);
        Assert.Contains("raw-singles", slugs);
    }

    [Fact]
    public async Task Catalog_Service_Returns_Category_Tree()
    {
        await using var db = CreateDb();
        var categories = await CreateCatalogService(db).GetCategoriesAsync(CancellationToken.None);
        var sealedCategory = categories.Single(c => c.Slug == "sealed");
        Assert.Contains(sealedCategory.Children, c => c.Slug == "booster-packs");
    }

    [Fact]
    public async Task Catalog_Product_Query_Filters_By_Game_Slug()
    {
        await using var db = CreateDb();
        var products = await CreateCatalogService(db).GetProductsAsync(new CatalogProductQuery { GameSlug = "pokemon", Take = 20 }, CancellationToken.None);
        Assert.NotEmpty(products);
        Assert.All(products, p => Assert.Equal("Pokemon", p.GameName));
    }

    [Fact]
    public async Task Catalog_Product_Query_Filters_By_Category_Slug()
    {
        await using var db = CreateDb();
        var products = await CreateCatalogService(db).GetProductsAsync(new CatalogProductQuery { CategorySlug = "booster-packs", Take = 20 }, CancellationToken.None);
        Assert.NotEmpty(products);
        Assert.All(products, p => Assert.Equal("booster-packs", p.CategorySlug));
    }

    [Fact]
    public async Task Marketplace_Home_Returns_Discovery_Sections()
    {
        await using var db = CreateDb();
        var home = await new CatalogDiscoveryService(CreateCatalogService(db)).GetMarketplaceHomeAsync(null, CancellationToken.None);
        Assert.NotEmpty(home.PrimaryGames);
        Assert.NotEmpty(home.TrendingProducts);
        Assert.NotEmpty(home.FeaturedProducts);
        Assert.NotEmpty(home.LatestSets);
    }

    [Fact]
    public async Task Provider_Capabilities_Show_Planned_Connectors()
    {
        await using var db = CreateDb();
        var providers = await CreateCatalogService(db).GetProviderCapabilitiesAsync(CancellationToken.None);
        Assert.Contains(providers, p => p.SourceName == "TCGplayer" && p.SupportsMarketplaceListings);
        Assert.Contains(providers, p => p.SourceName == "Scryfall" && p.SupportsMagic);
    }

    [Fact]
    public async Task Seller_Inventory_Create_Requires_Known_Product()
    {
        await using var db = CreateDb();
        var service = new SellerInventoryService(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateInventoryItemAsync(Guid.NewGuid(), new CreateSellerInventoryItemRequest
        {
            CatalogProductId = Guid.NewGuid(),
            Condition = ProductCondition.NearMint,
            Quantity = 1
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Seller_Inventory_Create_Rejects_Unknown_Condition()
    {
        await using var db = CreateDb();
        var productId = await db.CatalogProducts.Select(p => p.Id).FirstAsync();
        var service = new SellerInventoryService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateInventoryItemAsync(Guid.NewGuid(), new CreateSellerInventoryItemRequest
        {
            CatalogProductId = productId,
            Condition = ProductCondition.Unknown,
            Quantity = 1
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Seller_Inventory_Create_Persists_Item_And_Images()
    {
        await using var db = CreateDb();
        var productId = await db.CatalogProducts.Select(p => p.Id).FirstAsync();
        var sellerId = Guid.NewGuid();
        var service = new SellerInventoryService(db);
        var item = await service.CreateInventoryItemAsync(sellerId, new CreateSellerInventoryItemRequest
        {
            CatalogProductId = productId,
            Condition = ProductCondition.NearMint,
            Quantity = 2,
            AskingPrice = 12.50m,
            ImageUrls = new[] { "https://example.com/card-front.jpg" }
        }, CancellationToken.None);

        Assert.Equal(sellerId, item.SellerUserId);
        Assert.Equal(2, item.Quantity);
        Assert.Single(item.ImageUrls);
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

    private static CatalogService CreateCatalogService(CardsDbContext db)
        => new(
            db,
            Options.Create(new TcgPlayerOptions()),
            Options.Create(new EbayOptions()),
            Options.Create(new ScryfallOptions()),
            Options.Create(new PokemonTcgOptions()),
            Options.Create(new MtgJsonOptions()),
            Options.Create(new PriceChartingOptions()),
            Options.Create(new CardKingdomOptions()));
}
