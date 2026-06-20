using P2W.DealFinder.Domain.Catalog;
using P2W.DealFinder.Domain.MarketEvidence;

namespace P2W.DealFinder.Application.Ports;

public interface ICatalogImportProvider
{
    string SourceName { get; }
    Task<IReadOnlyList<CatalogProduct>> GetProductsAsync(string gameOrBrand, string? setNameOrCode, CancellationToken ct);
}

public interface IActiveListingProvider
{
    string SourceName { get; }
    Task<IReadOnlyList<ActiveListing>> SearchActiveListingsAsync(CatalogProduct product, CancellationToken ct);
}

public interface ISoldCompProvider
{
    string SourceName { get; }
    Task<IReadOnlyList<SoldComp>> SearchSoldCompsAsync(CatalogProduct product, DateTime sinceUtc, CancellationToken ct);
}

public interface IReferencePriceProvider
{
    string SourceName { get; }
    Task<IReadOnlyList<ReferencePrice>> GetReferencePricesAsync(CatalogProduct product, CancellationToken ct);
}
