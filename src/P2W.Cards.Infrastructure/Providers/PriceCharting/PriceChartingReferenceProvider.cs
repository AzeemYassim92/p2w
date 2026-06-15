using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Providers.PriceCharting;

public sealed class PriceChartingReferenceProvider(IOptions<PriceChartingOptions> options, ILogger<PriceChartingReferenceProvider> logger) : IMarketplaceReferencePriceProvider
{
    public string SourceName => "PriceCharting";
    public bool IsEnabled => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.ApiToken);

    public Task<IReadOnlyList<ExternalReferencePriceDto>> GetReferencePricesAsync(CatalogProduct product, IReadOnlyList<ExternalProductMapping> mappings, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            logger.LogInformation("PriceCharting provider disabled or missing token. No API token logged.");
            return Task.FromResult<IReadOnlyList<ExternalReferencePriceDto>>(Array.Empty<ExternalReferencePriceDto>());
        }

        // Scaffold only: use official API/CSV exports here once provider access and token usage are finalized.
        return Task.FromResult<IReadOnlyList<ExternalReferencePriceDto>>(Array.Empty<ExternalReferencePriceDto>());
    }
}
