using Microsoft.EntityFrameworkCore;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;

namespace P2W.Cards.Infrastructure.Services;

public sealed class MappingReviewService(CardsDbContext db) : IMappingReviewService
{
    public async Task<IReadOnlyList<MappingReviewDto>> GetMappingsForReviewAsync(string? status, int take, CancellationToken ct)
        => await db.ExternalProductMappings
            .Include(m => m.CatalogProduct).ThenInclude(p => p!.CardSet)
            .Where(m => string.IsNullOrWhiteSpace(status) || m.MappingStatus == status)
            .OrderBy(m => m.ConfidenceScore)
            .ThenByDescending(m => m.CreatedUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(m => ToDto(m))
            .ToListAsync(ct);

    public Task<MappingReviewDto?> ApproveAsync(Guid mappingId, CancellationToken ct) => SetStatusAsync(mappingId, "Approved", ct);

    public Task<MappingReviewDto?> RejectAsync(Guid mappingId, CancellationToken ct) => SetStatusAsync(mappingId, "Rejected", ct);

    public async Task<MappingReviewDto?> SaveNotesAsync(Guid mappingId, string? notes, CancellationToken ct)
    {
        var mapping = await db.ExternalProductMappings.Include(m => m.CatalogProduct).ThenInclude(p => p!.CardSet).FirstOrDefaultAsync(m => m.Id == mappingId, ct);
        if (mapping == null) return null;
        mapping.MappingNotes = notes;
        await db.SaveChangesAsync(ct);
        return ToDto(mapping);
    }

    private async Task<MappingReviewDto?> SetStatusAsync(Guid mappingId, string status, CancellationToken ct)
    {
        var mapping = await db.ExternalProductMappings.Include(m => m.CatalogProduct).ThenInclude(p => p!.CardSet).FirstOrDefaultAsync(m => m.Id == mappingId, ct);
        if (mapping == null) return null;
        mapping.MappingStatus = status;
        await db.SaveChangesAsync(ct);
        return ToDto(mapping);
    }

    private static MappingReviewDto ToDto(ExternalProductMapping mapping) => new()
    {
        MappingId = mapping.Id,
        CatalogProductId = mapping.CatalogProductId,
        ProductName = mapping.CatalogProduct?.Name ?? "",
        SetName = mapping.CatalogProduct?.CardSet?.Name,
        SourceName = mapping.SourceName,
        ExternalId = mapping.ExternalId,
        ExternalUrl = mapping.ExternalUrl,
        ConfidenceScore = mapping.ConfidenceScore,
        MappingStatus = mapping.MappingStatus,
        MappingNotes = mapping.MappingNotes
    };
}

public sealed class MockCatalogPricingProvider : IExternalPricingProvider
{
    public string SourceName => "MockCatalogPricing";
    public bool IsEnabled => true;

    public Task<IReadOnlyList<ExternalPriceReferenceDto>> GetPricesForProductAsync(CatalogProductDto product, IReadOnlyList<ExternalProductMappingDto> mappings, CancellationToken ct)
    {
        var seed = Math.Abs(product.Name.GetHashCode());
        var basePrice = Math.Round((seed % 20000) / 100m + 1m, 2);
        IReadOnlyList<ExternalPriceReferenceDto> prices = new[]
        {
            new ExternalPriceReferenceDto
            {
                SourceName = SourceName,
                MarketPrice = basePrice,
                LowPrice = Math.Max(0.25m, basePrice - 1.50m),
                MidPrice = basePrice,
                HighPrice = basePrice + 4.00m,
                UngradedPrice = basePrice,
                Grade9Price = basePrice * 2,
                Grade10Price = basePrice * 4,
                Currency = "USD"
            }
        };
        return Task.FromResult(prices);
    }
}

public sealed class CatalogPricingService(CardsDbContext db, IEnumerable<IExternalPricingProvider> providers, ICatalogService catalog) : ICatalogPricingService
{
    public async Task<IReadOnlyList<CatalogPriceReferenceSnapshotDto>> GetPriceHistoryAsync(Guid catalogProductId, CancellationToken ct)
        => await db.CatalogPriceReferenceSnapshots
            .Where(p => p.CatalogProductId == catalogProductId)
            .OrderByDescending(p => p.CapturedAtUtc)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

    public async Task RefreshPricesForProductAsync(Guid catalogProductId, CancellationToken ct)
    {
        var product = await catalog.GetProductDetailAsync(catalogProductId, ct) ?? throw new KeyNotFoundException("Catalog product not found.");
        foreach (var provider in providers.Where(p => p.IsEnabled))
        {
            var prices = await provider.GetPricesForProductAsync(product, product.ExternalMappings, ct);
            foreach (var price in prices)
            {
                db.CatalogPriceReferenceSnapshots.Add(new CatalogPriceReferenceSnapshot
                {
                    Id = Guid.NewGuid(),
                    CatalogProductId = catalogProductId,
                    SourceName = price.SourceName,
                    MarketPrice = price.MarketPrice,
                    LowPrice = price.LowPrice,
                    MidPrice = price.MidPrice,
                    HighPrice = price.HighPrice,
                    UngradedPrice = price.UngradedPrice,
                    Grade7Price = price.Grade7Price,
                    Grade8Price = price.Grade8Price,
                    Grade9Price = price.Grade9Price,
                    Grade10Price = price.Grade10Price,
                    BuylistPrice = price.BuylistPrice,
                    RetailPrice = price.RetailPrice,
                    Currency = price.Currency,
                    RawSourceJson = price.RawSourceJson,
                    CapturedAtUtc = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static CatalogPriceReferenceSnapshotDto ToDto(CatalogPriceReferenceSnapshot snapshot) => new()
    {
        CatalogPriceReferenceSnapshotId = snapshot.Id,
        CatalogProductId = snapshot.CatalogProductId,
        SourceName = snapshot.SourceName,
        MarketPrice = snapshot.MarketPrice,
        LowPrice = snapshot.LowPrice,
        MidPrice = snapshot.MidPrice,
        HighPrice = snapshot.HighPrice,
        Currency = snapshot.Currency,
        CapturedAtUtc = snapshot.CapturedAtUtc
    };
}
