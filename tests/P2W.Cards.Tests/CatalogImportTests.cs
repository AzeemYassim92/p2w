using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.PokemonTcg;
using P2W.Cards.Infrastructure.Providers.Scryfall;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Tests;

public sealed class CatalogImportTests
{
    [Fact]
    public async Task Preview_Import_Does_Not_Create_Products()
    {
        await using var db = CreateDb();
        var before = await db.CatalogProducts.CountAsync();
        var service = CreateImportService(db, new FakeCatalogProvider("Scryfall", true, Product("sf-1", "Preview Card")));
        var preview = await service.PreviewImportAsync(Request(), CancellationToken.None);
        Assert.Equal(before, await db.CatalogProducts.CountAsync());
        Assert.Equal(1, preview.ExternalRecordsRead);
        Assert.Equal(1, preview.WouldCreate);
    }

    [Fact]
    public async Task Real_Import_Creates_Run_Product_Set_And_Mapping()
    {
        await using var db = CreateDb();
        var service = CreateImportService(db, new FakeCatalogProvider("Scryfall", true, Product("sf-2", "Imported Card")));
        var run = await service.StartImportAsync(Request(), CancellationToken.None);
        Assert.Equal("Completed", run.Status);
        Assert.Contains(await db.CatalogProducts.ToListAsync(), p => p.Name == "Imported Card");
        Assert.Contains(await db.CardSets.ToListAsync(), s => s.Name == "Test Set");
        Assert.Contains(await db.ExternalProductMappings.ToListAsync(), m => m.ExternalId == "sf-2");
    }

    [Fact]
    public async Task Real_Import_Is_Idempotent()
    {
        await using var db = CreateDb();
        var service = CreateImportService(db, new FakeCatalogProvider("Scryfall", true, Product("sf-3", "Idempotent Card")));
        await service.StartImportAsync(Request(), CancellationToken.None);
        await service.StartImportAsync(Request(), CancellationToken.None);
        Assert.Equal(1, await db.CatalogProducts.CountAsync(p => p.Name == "Idempotent Card"));
    }

    [Fact]
    public async Task Loose_Name_Match_Creates_New_Product_Instead_Of_Updating()
    {
        await using var db = CreateDb();
        var service = CreateImportService(db, new FakeCatalogProvider(
            "Scryfall",
            true,
            Product("sf-old", "Repeated Name"),
            Product("sf-new", "Repeated Name", "Different Set", "2")));

        await service.StartImportAsync(Request(), CancellationToken.None);

        Assert.Equal(2, await db.CatalogProducts.CountAsync(p => p.Name == "Repeated Name"));
        Assert.Contains(await db.CatalogProducts.Include(p => p.CardSet).ToListAsync(), p => p.Name == "Repeated Name" && p.CardSet!.Name == "Test Set" && p.CardNumber == "1");
        Assert.Contains(await db.CatalogProducts.Include(p => p.CardSet).ToListAsync(), p => p.Name == "Repeated Name" && p.CardSet!.Name == "Different Set" && p.CardNumber == "2");
    }

    [Fact]
    public async Task Real_Import_Saves_Next_Checkpoint()
    {
        await using var db = CreateDb();
        var service = CreateImportService(db, new FakeCatalogProvider("Scryfall", true, Product("sf-checkpoint", "Checkpoint Card")) { NextCheckpointValue = "page-2", HasMore = true });
        var run = await service.StartImportAsync(Request(), CancellationToken.None);
        var checkpoint = await db.CatalogImportCheckpoints.SingleAsync(c => c.SourceName == "Scryfall" && c.ImportType == "Cards");
        Assert.Equal("page-2", checkpoint.CheckpointValue);
        Assert.Equal("page-2", run.NextCheckpointValue);
        Assert.True(run.HasMore);
    }

    [Fact]
    public async Task MaxRecords_Is_Respected()
    {
        await using var db = CreateDb();
        var provider = new FakeCatalogProvider("Scryfall", true, Product("sf-4", "One"), Product("sf-5", "Two"));
        var service = CreateImportService(db, provider);
        var preview = await service.PreviewImportAsync(Request(maxRecords: 1), CancellationToken.None);
        Assert.Equal(1, preview.ExternalRecordsRead);
    }

    [Fact]
    public async Task Disabled_Provider_Returns_Clear_Error()
    {
        await using var db = CreateDb();
        var service = CreateImportService(db, new FakeCatalogProvider("Scryfall", false, Product("sf-6", "Disabled")));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewImportAsync(Request(), CancellationToken.None));
        Assert.Contains("disabled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scryfall_Normalizer_Uses_First_Face_Image_For_Double_Faced_Card()
    {
        var dto = ScryfallNormalizer.ToExternalProduct(new ScryfallCardDto
        {
            Id = "abc",
            Name = "Double Face",
            SetName = "Set",
            CardFaces = new() { new ScryfallCardFaceDto { ImageUris = new ScryfallImageUris { Normal = "https://img.example/face.jpg" } } }
        });
        Assert.Equal("https://img.example/face.jpg", dto.ImageUrl);
    }

    [Fact]
    public void Pokemon_Normalizer_Stores_Large_Image_Url()
    {
        var dto = PokemonTcgNormalizer.ToExternalProduct(new PokemonTcgCardDto
        {
            Id = "xy1-1",
            Name = "Pokemon Card",
            Images = new PokemonTcgImagesDto { Small = "small", Large = "large" },
            Set = new PokemonTcgSetDto { Id = "xy1", Name = "XY" }
        });
        Assert.Equal("large", dto.ImageUrl);
    }

    [Fact]
    public async Task Existing_Mapping_Returns_Confidence_One()
    {
        await using var db = CreateDb();
        var product = await db.CatalogProducts.FirstAsync();
        db.ExternalProductMappings.Add(new() { Id = Guid.NewGuid(), CatalogProductId = product.Id, SourceName = "Scryfall", ExternalId = "mapped", CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var result = await new CatalogProductMatchingService(db).FindBestMatchAsync(Product("mapped", product.Name), CancellationToken.None);
        Assert.Equal(1.00m, result.ConfidenceScore);
    }

    [Fact]
    public async Task Exact_Game_Set_Number_Name_Returns_Confidence_095()
    {
        await using var db = CreateDb();
        var product = await db.CatalogProducts.Include(p => p.CardSet).Include(p => p.Game).FirstAsync(p => p.CardNumber != null);
        var external = Product("new", product.Name);
        external.SetName = product.CardSet!.Name;
        external.CardNumber = product.CardNumber;
        external.GameSlug = product.Game!.Slug;
        var result = await new CatalogProductMatchingService(db).FindBestMatchAsync(external, CancellationToken.None);
        Assert.Equal(0.95m, result.ConfidenceScore);
    }

    [Fact]
    public async Task Low_Confidence_Mapping_Is_NeedsReview()
    {
        await using var db = CreateDb();
        var product = await db.CatalogProducts.FirstAsync();
        await new CatalogProductMatchingService(db).CreateOrUpdateMappingAsync(product.Id, Product("low", "Low"), 0.70m, CancellationToken.None);
        Assert.Equal("NeedsReview", await db.ExternalProductMappings.Where(m => m.ExternalId == "low").Select(m => m.MappingStatus).FirstAsync());
    }

    [Fact]
    public async Task High_Confidence_Mapping_Is_AutoMatched()
    {
        await using var db = CreateDb();
        var product = await db.CatalogProducts.FirstAsync();
        await new CatalogProductMatchingService(db).CreateOrUpdateMappingAsync(product.Id, Product("high", "High"), 0.95m, CancellationToken.None);
        Assert.Equal("AutoMatched", await db.ExternalProductMappings.Where(m => m.ExternalId == "high").Select(m => m.MappingStatus).FirstAsync());
    }

    [Fact]
    public async Task Mapping_Review_Approve_Reject_And_Notes_Work()
    {
        await using var db = CreateDb();
        var product = await db.CatalogProducts.FirstAsync();
        var mapping = new P2W.Cards.Domain.Entities.ExternalProductMapping { Id = Guid.NewGuid(), CatalogProductId = product.Id, SourceName = "Scryfall", ExternalId = "review", MappingStatus = "NeedsReview", CreatedUtc = DateTime.UtcNow };
        db.ExternalProductMappings.Add(mapping);
        await db.SaveChangesAsync();
        var service = new MappingReviewService(db);
        Assert.Equal("Approved", (await service.ApproveAsync(mapping.Id, CancellationToken.None))!.MappingStatus);
        Assert.Equal("Rejected", (await service.RejectAsync(mapping.Id, CancellationToken.None))!.MappingStatus);
        Assert.Equal("Looks wrong", (await service.SaveNotesAsync(mapping.Id, "Looks wrong", CancellationToken.None))!.MappingNotes);
    }

    [Fact]
    public async Task Mock_Catalog_Pricing_Refresh_Creates_Snapshot()
    {
        await using var db = CreateDb();
        var productId = await db.CatalogProducts.Select(p => p.Id).FirstAsync();
        var service = new CatalogPricingService(db, new IExternalPricingProvider[] { new MockCatalogPricingProvider() }, CreateCatalogService(db));
        await service.RefreshPricesForProductAsync(productId, CancellationToken.None);
        var history = await service.GetPriceHistoryAsync(productId, CancellationToken.None);
        Assert.Single(history);
        Assert.Equal("MockCatalogPricing", history[0].SourceName);
        Assert.Equal("USD", history[0].Currency);
    }

    private static StartCatalogImportRequest Request(int maxRecords = 25) => new()
    {
        SourceName = "Scryfall",
        GameSlug = "magic-the-gathering",
        ImportType = "Cards",
        MaxRecords = maxRecords,
        IncludeImages = true,
        UpdateExistingProducts = true,
        CreateMissingProducts = true
    };

    private static ExternalCatalogProductDto Product(string id, string name, string setName = "Test Set", string cardNumber = "1") => new()
    {
        SourceName = "Scryfall",
        ExternalId = id,
        Name = name,
        GameSlug = "magic-the-gathering",
        SetName = setName,
        SetCode = "TST",
        CardNumber = cardNumber,
        Rarity = "rare",
        Artist = "Tester",
        ImageUrl = "https://example.com/card.jpg",
        ExternalUrl = "https://scryfall.com/card/test/1",
        VariantNames = new[] { "nonfoil", "foil" }
    };

    private static CardsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new CardsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static CatalogImportService CreateImportService(CardsDbContext db, IExternalCatalogProvider provider)
        => new(db, new[] { provider }, new CatalogProductMatchingService(db), new ImportCheckpointService(db), Options.Create(new CatalogImportOptions()), NullLogger<CatalogImportService>.Instance);

    private static CatalogService CreateCatalogService(CardsDbContext db)
        => new(db, Options.Create(new TcgPlayerOptions()), Options.Create(new EbayOptions()), Options.Create(new ScryfallOptions()), Options.Create(new PokemonTcgOptions()), Options.Create(new MtgJsonOptions()), Options.Create(new PriceChartingOptions()), Options.Create(new CardKingdomOptions()));

    private sealed class FakeCatalogProvider : IExternalCatalogProvider
    {
        private readonly ExternalCatalogProductDto[] products;

        public FakeCatalogProvider(string sourceName, bool enabled, params ExternalCatalogProductDto[] products)
        {
            SourceName = sourceName;
            IsEnabled = enabled;
            this.products = products;
        }

        public string SourceName { get; }
        public bool IsEnabled { get; }
        public string? NextCheckpointValue { get; init; }
        public bool HasMore { get; init; }
        public Task<ExternalCatalogImportResult> ImportAsync(CatalogImportContext context, CancellationToken ct)
            => Task.FromResult(new ExternalCatalogImportResult { Products = products.Take(context.MaxRecords).ToArray(), Sets = products.Select(p => new ExternalCatalogSetDto { SourceName = SourceName, ExternalId = p.SetCode ?? p.SetName ?? "set", Name = p.SetName ?? "Set", GameSlug = p.GameSlug, Code = p.SetCode }).DistinctBy(s => s.ExternalId).ToArray(), NextCheckpointValue = NextCheckpointValue, HasMore = HasMore });
        public async Task<ExternalCatalogImportPreview> PreviewAsync(CatalogImportContext context, CancellationToken ct)
        {
            var result = await ImportAsync(context, ct);
            return new ExternalCatalogImportPreview { Products = result.Products, Sets = result.Sets, NextCheckpointValue = result.NextCheckpointValue, HasMore = result.HasMore };
        }
    }
}
