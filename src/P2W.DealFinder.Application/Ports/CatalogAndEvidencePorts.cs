using P2W.DealFinder.Domain.Catalog;
using P2W.DealFinder.Domain.MarketEvidence;

namespace P2W.DealFinder.Application.Ports;

public interface IProductCatalogReader
{
    Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<CatalogProduct>> GetProductsInSetAsync(string gameOrBrand, string setNameOrCode, CancellationToken ct);
}

public interface IMarketEvidenceReader
{
    Task<MarketSnapshot?> GetLatestSnapshotAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<ActiveListing>> GetActiveListingsAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<SoldComp>> GetSoldCompsAsync(Guid productId, CancellationToken ct);
}

public interface IMarketEvidenceWriter
{
    Task RecordObservationAsync(ProviderObservation observation, CancellationToken ct);
    Task UpsertActiveListingsAsync(IReadOnlyList<ActiveListing> listings, CancellationToken ct);
    Task UpsertSoldCompsAsync(IReadOnlyList<SoldComp> soldComps, CancellationToken ct);
    Task UpsertReferencePricesAsync(IReadOnlyList<ReferencePrice> referencePrices, CancellationToken ct);
}
