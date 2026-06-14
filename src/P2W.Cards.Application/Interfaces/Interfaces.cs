using P2W.Cards.Application.DTOs;
using P2W.Cards.Domain.Enums;

namespace P2W.Cards.Application.Interfaces;

public interface ICardSearchService
{
    Task<IReadOnlyList<MarketplaceProductDto>> GetFeaturedMarketplaceProductsAsync(string? productType, int take, CancellationToken ct);
    Task<IReadOnlyList<CardSearchResultDto>> SearchCardsAsync(string query, string? game, CancellationToken ct);
    Task<CardDetailDto?> GetCardDetailAsync(Guid cardId, CancellationToken ct);
}

public interface IListingService
{
    Task<IReadOnlyList<ListingDto>> GetListingsForCardAsync(Guid cardId, CancellationToken ct);
    Task RefreshListingsForCardAsync(Guid cardId, CancellationToken ct);
}

public interface IPriceHistoryService
{
    Task<IReadOnlyList<PriceSnapshotDto>> GetListingPriceHistoryAsync(Guid cardId, CancellationToken ct);
    Task<IReadOnlyList<PriceReferenceSnapshotDto>> GetReferencePriceHistoryAsync(Guid cardId, CancellationToken ct);
    Task CaptureListingSnapshotForCardAsync(Guid cardId, CancellationToken ct);
    Task RefreshReferencePricesForCardAsync(Guid cardId, CancellationToken ct);
}

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItemDto>> GetUserWatchlistAsync(Guid userId, CancellationToken ct);
    Task<WatchlistItemDto> AddToWatchlistAsync(Guid userId, AddWatchlistItemRequest request, CancellationToken ct);
    Task RemoveFromWatchlistAsync(Guid userId, Guid watchlistItemId, CancellationToken ct);
}

public interface IPriceAlertService
{
    Task<IReadOnlyList<PriceAlertDto>> GetUserAlertsAsync(Guid userId, CancellationToken ct);
    Task<PriceAlertDto> CreateAlertAsync(Guid userId, CreatePriceAlertRequest request, CancellationToken ct);
    Task DisableAlertAsync(Guid userId, Guid alertId, CancellationToken ct);
    Task CheckAlertsForCardAsync(Guid cardId, CancellationToken ct);
}

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}

public interface ICardDataProvider
{
    string SourceName { get; }
    ProviderType ProviderType { get; }
    bool IsEnabled { get; }
    Task<ProviderHealthCheckResult> HealthCheckAsync(CancellationToken ct);
}

public interface ICardCatalogProvider : ICardDataProvider
{
    Task<IReadOnlyList<ExternalCardDto>> SearchCardsAsync(string query, string? game, CancellationToken ct);
}

public interface IMarketplaceListingProvider : ICardDataProvider
{
    Task<IReadOnlyList<MarketplaceListingDto>> SearchListingsAsync(CardSearchContext context, CancellationToken ct);
}

public interface IPriceReferenceProvider : ICardDataProvider
{
    Task<IReadOnlyList<ExternalPriceReferenceDto>> GetPriceReferencesAsync(CardSearchContext context, CancellationToken ct);
}

public interface IDataProviderRegistry
{
    IReadOnlyList<ICardCatalogProvider> CatalogProviders { get; }
    IReadOnlyList<IMarketplaceListingProvider> MarketplaceListingProviders { get; }
    IReadOnlyList<IPriceReferenceProvider> PriceReferenceProviders { get; }
    IReadOnlyList<ICardDataProvider> AllProviders { get; }
}

public interface ICatalogService
{
    Task<IReadOnlyList<GameDto>> GetGamesAsync(bool primaryOnly, CancellationToken ct);
    Task<IReadOnlyList<CardSetDto>> GetSetsAsync(Guid? gameId, string? gameSlug, bool? upcoming, int take, CancellationToken ct);
    Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<CatalogProductDto>> GetProductsAsync(CatalogProductQuery query, CancellationToken ct);
    Task<CatalogProductDetailDto?> GetProductDetailAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<ProviderCapabilityDto>> GetProviderCapabilitiesAsync(CancellationToken ct);
}

public interface ICatalogDiscoveryService
{
    Task<MarketplaceHomeDto> GetMarketplaceHomeAsync(string? gameSlug, CancellationToken ct);
}

public interface ISellerInventoryService
{
    Task<IReadOnlyList<SellerInventoryItemDto>> GetInventoryAsync(Guid sellerUserId, CancellationToken ct);
    Task<SellerInventoryItemDto> CreateInventoryItemAsync(Guid sellerUserId, CreateSellerInventoryItemRequest request, CancellationToken ct);
}

public interface ICatalogImportService
{
    Task<CatalogImportRunDto> StartImportAsync(StartCatalogImportRequest request, CancellationToken ct);
    Task<CatalogImportPreviewDto> PreviewImportAsync(StartCatalogImportRequest request, CancellationToken ct);
    Task<CatalogImportRunDetailDto?> GetImportRunAsync(Guid importRunId, CancellationToken ct);
    Task<IReadOnlyList<CatalogImportRunDto>> GetImportRunsAsync(string? sourceName, int take, CancellationToken ct);
}

public interface IExternalCatalogProvider
{
    string SourceName { get; }
    bool IsEnabled { get; }
    Task<ExternalCatalogImportResult> ImportAsync(CatalogImportContext context, CancellationToken ct);
    Task<ExternalCatalogImportPreview> PreviewAsync(CatalogImportContext context, CancellationToken ct);
}

public interface ICatalogProductMatchingService
{
    Task<CatalogProductMatchResult> FindBestMatchAsync(ExternalCatalogProductDto externalProduct, CancellationToken ct);
    Task<ExternalProductMappingDto> CreateOrUpdateMappingAsync(Guid catalogProductId, ExternalCatalogProductDto externalProduct, decimal confidenceScore, CancellationToken ct);
}

public interface IImportCheckpointService
{
    Task<CatalogImportCheckpointDto?> GetCheckpointAsync(string sourceName, string importType, CancellationToken ct);
    Task SaveCheckpointAsync(string sourceName, string importType, string checkpointValue, CancellationToken ct);
}

public interface IMappingReviewService
{
    Task<IReadOnlyList<MappingReviewDto>> GetMappingsForReviewAsync(string? status, int take, CancellationToken ct);
    Task<MappingReviewDto?> ApproveAsync(Guid mappingId, CancellationToken ct);
    Task<MappingReviewDto?> RejectAsync(Guid mappingId, CancellationToken ct);
    Task<MappingReviewDto?> SaveNotesAsync(Guid mappingId, string? notes, CancellationToken ct);
}

public interface IExternalPricingProvider
{
    string SourceName { get; }
    bool IsEnabled { get; }
    Task<IReadOnlyList<ExternalPriceReferenceDto>> GetPricesForProductAsync(CatalogProductDto product, IReadOnlyList<ExternalProductMappingDto> mappings, CancellationToken ct);
}

public interface ICatalogPricingService
{
    Task<IReadOnlyList<CatalogPriceReferenceSnapshotDto>> GetPriceHistoryAsync(Guid catalogProductId, CancellationToken ct);
    Task RefreshPricesForProductAsync(Guid catalogProductId, CancellationToken ct);
}

public sealed class ProviderHealthCheckResult
{
    public string SourceName { get; set; } = "";
    public string ProviderType { get; set; } = "";
    public bool IsEnabled { get; set; }
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = "";
}
