using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Enums;

namespace P2W.Cards.Infrastructure.Providers.Common;

public abstract class CardDataProviderBase(string sourceName, ProviderType providerType, bool isEnabled, ILogger logger) : ICardDataProvider
{
    protected ILogger Logger { get; } = logger;
    public string SourceName { get; } = sourceName;
    public ProviderType ProviderType { get; } = providerType;
    public bool IsEnabled { get; } = isEnabled;

    public virtual Task<ProviderHealthCheckResult> HealthCheckAsync(CancellationToken ct)
    {
        Logger.LogInformation("{Provider} health check requested. Enabled: {Enabled}", SourceName, IsEnabled);
        return Task.FromResult(new ProviderHealthCheckResult
        {
            SourceName = SourceName,
            ProviderType = ProviderType.ToString(),
            IsEnabled = IsEnabled,
            IsHealthy = true,
            Message = IsEnabled ? "Ready" : "Disabled"
        });
    }
}

public sealed class DataProviderRegistry(IEnumerable<ICardCatalogProvider> catalogProviders, IEnumerable<IMarketplaceListingProvider> listingProviders, IEnumerable<IPriceReferenceProvider> priceProviders) : IDataProviderRegistry
{
    public IReadOnlyList<ICardCatalogProvider> CatalogProviders { get; } = catalogProviders.ToArray();
    public IReadOnlyList<IMarketplaceListingProvider> MarketplaceListingProviders { get; } = listingProviders.ToArray();
    public IReadOnlyList<IPriceReferenceProvider> PriceReferenceProviders { get; } = priceProviders.ToArray();
    public IReadOnlyList<ICardDataProvider> AllProviders => CatalogProviders.Cast<ICardDataProvider>()
        .Concat(MarketplaceListingProviders)
        .Concat(PriceReferenceProviders)
        .ToArray();
}

public sealed class ConditionNormalizer
{
    public string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Unknown";
        }

        var value = raw.Trim().ToUpperInvariant();
        if (value.StartsWith("PSA ") || value.StartsWith("CGC ") || value.StartsWith("BGS "))
        {
            return "Graded";
        }

        return value switch
        {
            "NM" or "NEAR MINT" => "Near Mint",
            "LP" or "LIGHT PLAY" or "LIGHTLY PLAYED" => "Lightly Played",
            "MP" or "MODERATELY PLAYED" => "Moderately Played",
            "HP" or "HEAVILY PLAYED" => "Heavily Played",
            "DMG" or "DAMAGED" => "Damaged",
            "SEALED" => "Sealed",
            "GRADED" => "Graded",
            _ => "Unknown"
        };
    }
}

public sealed class MockCatalogProvider(IOptions<MockProviderOptions> options, ILogger<MockCatalogProvider> logger)
    : CardDataProviderBase("Mock", ProviderType.Catalog, options.Value.Enabled, logger), ICardCatalogProvider
{
    public Task<IReadOnlyList<ExternalCardDto>> SearchCardsAsync(string query, string? game, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ExternalCardDto>>(Array.Empty<ExternalCardDto>());
}

public sealed class MockMarketplaceListingProvider(IOptions<MockProviderOptions> options, ILogger<MockMarketplaceListingProvider> logger)
    : CardDataProviderBase("MockMarket", ProviderType.MarketplaceListing, options.Value.Enabled, logger), IMarketplaceListingProvider
{
    public Task<IReadOnlyList<MarketplaceListingDto>> SearchListingsAsync(CardSearchContext context, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            logger.LogInformation("Mock marketplace provider disabled.");
            return Task.FromResult<IReadOnlyList<MarketplaceListingDto>>(Array.Empty<MarketplaceListingDto>());
        }

        var key = context.CardName.ToLowerInvariant();
        var listings = key switch
        {
            "charizard" => Build(context.CardName, 399.99m, 450m, 380m, "Near Mint"),
            "pikachu" => Build(context.CardName, 24.99m, 29.99m, 19.99m, "LP"),
            "blastoise" => Build(context.CardName, 149.99m, 175m, 132.50m, "NM"),
            "black lotus" => Build(context.CardName, 125000m, 140000m, 119500m, "PSA 10"),
            "sol ring" => Build(context.CardName, 2.49m, 4.99m, 1.99m, "Near Mint"),
            "lightning bolt" => Build(context.CardName, 1.49m, 3.25m, 1.15m, "Light Play"),
            "mox diamond" => Build(context.CardName, 599.99m, 650m, 575m, "NM"),
            _ => Build(context.CardName, 9.99m, 12.49m, 8.50m, "Unknown")
        };
        return Task.FromResult<IReadOnlyList<MarketplaceListingDto>>(listings);
    }

    private static IReadOnlyList<MarketplaceListingDto> Build(string name, decimal a, decimal b, decimal c, string condition)
    {
        var slug = name.ToLowerInvariant().Replace(" ", "-");
        return new[]
        {
            NewListing(slug, name, "001", a, 4.99m, condition),
            NewListing(slug, name, "002", b, 0m, "Near Mint"),
            NewListing(slug, name, "003", c, 5.50m, "LP")
        };
    }

    private static MarketplaceListingDto NewListing(string slug, string name, string id, decimal price, decimal shipping, string condition) => new()
    {
        SourceName = "MockMarket",
        ExternalListingId = $"mock-{slug}-{id}",
        Title = $"{name} {condition}",
        Price = price,
        ShippingPrice = shipping,
        Condition = condition,
        ListingUrl = $"https://example.com/mock-{slug}-{id}",
        ImageUrl = $"https://placehold.co/245x342?text={Uri.EscapeDataString(name)}",
        ListedAtUtc = DateTime.UtcNow.AddDays(-2),
        RawSourceJson = $"{{\"mockId\":\"mock-{slug}-{id}\"}}"
    };
}

public sealed class MockPriceReferenceProvider(IOptions<MockProviderOptions> options, ILogger<MockPriceReferenceProvider> logger)
    : CardDataProviderBase("MockPriceReference", ProviderType.PriceReference, options.Value.Enabled, logger), IPriceReferenceProvider
{
    public Task<IReadOnlyList<ExternalPriceReferenceDto>> GetPriceReferencesAsync(CardSearchContext context, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return Task.FromResult<IReadOnlyList<ExternalPriceReferenceDto>>(Array.Empty<ExternalPriceReferenceDto>());
        }

        var market = context.CardName.ToLowerInvariant() switch
        {
            "charizard" => 420m,
            "black lotus" => 130000m,
            "mox diamond" => 625m,
            "sol ring" => 3m,
            "lightning bolt" => 2m,
            _ => 25m
        };

        return Task.FromResult<IReadOnlyList<ExternalPriceReferenceDto>>(new[]
        {
            new ExternalPriceReferenceDto
            {
                SourceName = "MockPriceReference",
                MarketPrice = market,
                UngradedPrice = market * 0.85m,
                Grade8Price = market * 1.2m,
                Grade9Price = market * 1.8m,
                Grade10Price = market * 4m,
                Currency = "USD",
                RawSourceJson = $"{{\"marketPrice\":{market}}}"
            }
        });
    }
}

public sealed class DisabledCatalogProvider(string sourceName, ProviderType providerType, bool isEnabled, ILogger logger)
    : CardDataProviderBase(sourceName, providerType, isEnabled, logger), ICardCatalogProvider
{
    public Task<IReadOnlyList<ExternalCardDto>> SearchCardsAsync(string query, string? game, CancellationToken ct)
    {
        Logger.LogInformation("{Provider} catalog placeholder returned no results.", SourceName);
        return Task.FromResult<IReadOnlyList<ExternalCardDto>>(Array.Empty<ExternalCardDto>());
    }
}

public sealed class DisabledListingProvider(string sourceName, bool isEnabled, ILogger logger)
    : CardDataProviderBase(sourceName, ProviderType.MarketplaceListing, isEnabled, logger), IMarketplaceListingProvider
{
    public Task<IReadOnlyList<MarketplaceListingDto>> SearchListingsAsync(CardSearchContext context, CancellationToken ct)
    {
        Logger.LogInformation("{Provider} marketplace placeholder returned no results.", SourceName);
        return Task.FromResult<IReadOnlyList<MarketplaceListingDto>>(Array.Empty<MarketplaceListingDto>());
    }
}

public sealed class DisabledPriceProvider(string sourceName, ProviderType providerType, bool isEnabled, ILogger logger)
    : CardDataProviderBase(sourceName, providerType, isEnabled, logger), IPriceReferenceProvider
{
    public Task<IReadOnlyList<ExternalPriceReferenceDto>> GetPriceReferencesAsync(CardSearchContext context, CancellationToken ct)
    {
        Logger.LogInformation("{Provider} price placeholder returned no results.", SourceName);
        return Task.FromResult<IReadOnlyList<ExternalPriceReferenceDto>>(Array.Empty<ExternalPriceReferenceDto>());
    }
}
