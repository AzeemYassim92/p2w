using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbayActiveListingProvider(
    IOptions<EbayOptions> options,
    EbaySearchQueryBuilder queryBuilder,
    EbayListingNormalizer normalizer,
    EbayBrowseApiClient client,
    ILogger<EbayActiveListingProvider> logger,
    LocalSessionLog? sessionLog = null) : IMarketplaceActiveListingProvider
{
    public string SourceName => "eBay";
    public bool IsEnabled => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.ClientId) && !string.IsNullOrWhiteSpace(options.Value.ClientSecret);

    public async Task<IReadOnlyList<ExternalMarketplaceListingDto>> GetCurrentListingsAsync(CatalogProduct product, MarketplaceSearchContext context, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            logger.LogInformation("eBay Browse provider disabled or missing credentials. Query would be: {Query}", queryBuilder.Build(product));
            sessionLog?.Warning("provider.ebay", "ebay.listings.disabled", "eBay active listing provider is disabled or missing credentials.", new { product.Id, product.Name });
            return Array.Empty<ExternalMarketplaceListingDto>();
        }

        var query = queryBuilder.Build(product);
        sessionLog?.Info("provider.ebay", "ebay.listings.query", "Requesting eBay active listings.", new { product.Id, product.Name, product.CardNumber, Query = query });
        var accessToken = await client.GetApplicationAccessTokenAsync(ct);
        var response = await client.SearchAsync(query, accessToken, Math.Clamp(options.Value.MaxListingsPerProduct, 1, 200), ct);
        var listings = response.ItemSummaries
            .Select(item => normalizer.Normalize(item, product))
            .ToArray();
        sessionLog?.Info("provider.ebay", "ebay.listings.result", "eBay active listing response normalized.", new
        {
            product.Id,
            product.Name,
            Count = listings.Length,
            Excluded = listings.Count(l => l.IsExcludedFromMarketValue),
            Matched = listings.Count(l => !l.IsExcludedFromMarketValue)
        });
        return listings;
    }

    public ExternalMarketplaceListingDto NormalizeForTests(EbayItemSummaryDto item, CatalogProduct product) => normalizer.Normalize(item, product);
}

public sealed class EbaySoldCompsProvider(ILogger<EbaySoldCompsProvider> logger, LocalSessionLog? sessionLog = null) : IMarketplaceSoldCompsProvider
{
    public string SourceName => "eBay";
    public bool IsEnabled => false;

    public Task<IReadOnlyList<ExternalMarketplaceSaleDto>> GetRecentSalesAsync(CatalogProduct product, MarketplaceSearchContext context, DateTime sinceUtc, CancellationToken ct)
    {
        logger.LogInformation("eBay sold comps provider is UnsupportedNoApprovedAccess and remains disabled.");
        sessionLog?.Warning("provider.ebay", "ebay.sales.disabled", "eBay sold comps are disabled for the current Browse API integration.", new
        {
            product.Id,
            product.Name,
            SinceUtc = sinceUtc
        });
        return Task.FromResult<IReadOnlyList<ExternalMarketplaceSaleDto>>(Array.Empty<ExternalMarketplaceSaleDto>());
    }
}
