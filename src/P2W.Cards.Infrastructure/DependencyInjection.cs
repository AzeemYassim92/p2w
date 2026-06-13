using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Enums;
using P2W.Cards.Infrastructure.BackgroundJobs;
using P2W.Cards.Infrastructure.CurrentUser;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.Common;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCardsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CardOptions>(configuration.GetSection("Cards"));
        services.Configure<MockProviderOptions>(configuration.GetSection("Providers:Mock"));
        services.Configure<TcgPlayerOptions>(configuration.GetSection("Providers:TcgPlayer"));
        services.Configure<EbayOptions>(configuration.GetSection("Providers:Ebay"));
        services.Configure<ScryfallOptions>(configuration.GetSection("Providers:Scryfall"));
        services.Configure<MtgJsonOptions>(configuration.GetSection("Providers:MtgJson"));
        services.Configure<PokemonTcgOptions>(configuration.GetSection("Providers:PokemonTcg"));
        services.Configure<PriceChartingOptions>(configuration.GetSection("Providers:PriceCharting"));
        services.Configure<CardKingdomOptions>(configuration.GetSection("Providers:CardKingdom"));

        services.AddDbContext<CardsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ConditionNormalizer>();
        services.AddScoped<ICardSearchService, CardSearchService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IPriceAlertService, PriceAlertService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogDiscoveryService, CatalogDiscoveryService>();
        services.AddScoped<ISellerInventoryService, SellerInventoryService>();

        services.AddScoped<ICardCatalogProvider, MockCatalogProvider>();
        services.AddScoped<IMarketplaceListingProvider, MockMarketplaceListingProvider>();
        services.AddScoped<IPriceReferenceProvider, MockPriceReferenceProvider>();
        services.AddScoped<ICardCatalogProvider>(sp => new DisabledCatalogProvider("Scryfall", ProviderType.Catalog, sp.GetRequiredService<IOptions<ScryfallOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("ScryfallCatalogProvider")));
        services.AddScoped<ICardCatalogProvider>(sp => new DisabledCatalogProvider("PokemonTCG", ProviderType.Catalog, sp.GetRequiredService<IOptions<PokemonTcgOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("PokemonTcgCatalogProvider")));
        services.AddScoped<ICardCatalogProvider>(sp => new DisabledCatalogProvider("TCGplayer", ProviderType.Catalog, sp.GetRequiredService<IOptions<TcgPlayerOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("TcgPlayerCatalogProvider")));
        services.AddScoped<IMarketplaceListingProvider>(sp => new DisabledListingProvider("eBay", sp.GetRequiredService<IOptions<EbayOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("EbayMarketplaceListingProvider")));
        services.AddScoped<IMarketplaceListingProvider>(sp => new DisabledListingProvider("TCGplayer", sp.GetRequiredService<IOptions<TcgPlayerOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("TcgPlayerMarketplaceListingProvider")));
        services.AddScoped<IPriceReferenceProvider>(sp => new DisabledPriceProvider("TCGplayer", ProviderType.PriceReference, sp.GetRequiredService<IOptions<TcgPlayerOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("TcgPlayerPriceReferenceProvider")));
        services.AddScoped<IPriceReferenceProvider>(sp => new DisabledPriceProvider("MTGJSON", ProviderType.PriceReference, sp.GetRequiredService<IOptions<MtgJsonOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("MtgJsonPriceReferenceProvider")));
        services.AddScoped<IPriceReferenceProvider>(sp => new DisabledPriceProvider("PriceCharting", ProviderType.PriceReference, sp.GetRequiredService<IOptions<PriceChartingOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("PriceChartingPriceReferenceProvider")));
        services.AddScoped<IPriceReferenceProvider>(sp => new DisabledPriceProvider("CardKingdom", ProviderType.RetailReference, sp.GetRequiredService<IOptions<CardKingdomOptions>>().Value.Enabled, sp.GetRequiredService<ILoggerFactory>().CreateLogger("CardKingdomPriceReferenceProvider")));
        services.AddScoped<IDataProviderRegistry, DataProviderRegistry>();
        services.AddHostedService<PriceRefreshWorker>();

        return services;
    }
}
