using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbayActiveListingProvider(IOptions<EbayOptions> options, EbaySearchQueryBuilder queryBuilder, EbayListingNormalizer normalizer, EbayBrowseApiClient client, ILogger<EbayActiveListingProvider> logger) : IMarketplaceActiveListingProvider
{
    public string SourceName => "eBay";
    public bool IsEnabled => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.ClientId) && !string.IsNullOrWhiteSpace(options.Value.ClientSecret);

    public async Task<IReadOnlyList<ExternalMarketplaceListingDto>> GetCurrentListingsAsync(CatalogProduct product, MarketplaceSearchContext context, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            logger.LogInformation("eBay Browse provider disabled or missing credentials. Query would be: {Query}", queryBuilder.Build(product));
            return Array.Empty<ExternalMarketplaceListingDto>();
        }

        var query = queryBuilder.Build(product);
        var accessToken = await client.GetApplicationAccessTokenAsync(ct);
        var response = await client.SearchAsync(query, accessToken, Math.Clamp(options.Value.MaxListingsPerProduct, 1, 200), ct);
        return response.ItemSummaries
            .Select(item => normalizer.Normalize(item, product))
            .ToArray();
    }

    public ExternalMarketplaceListingDto NormalizeForTests(EbayItemSummaryDto item, CatalogProduct product) => normalizer.Normalize(item, product);
}

public sealed class EbaySoldCompsProvider(ILogger<EbaySoldCompsProvider> logger) : IMarketplaceSoldCompsProvider
{
    public string SourceName => "eBay";
    public bool IsEnabled => false;

    public Task<IReadOnlyList<ExternalMarketplaceSaleDto>> GetRecentSalesAsync(CatalogProduct product, MarketplaceSearchContext context, DateTime sinceUtc, CancellationToken ct)
    {
        logger.LogInformation("eBay sold comps provider is UnsupportedNoApprovedAccess and remains disabled.");
        return Task.FromResult<IReadOnlyList<ExternalMarketplaceSaleDto>>(Array.Empty<ExternalMarketplaceSaleDto>());
    }
}
