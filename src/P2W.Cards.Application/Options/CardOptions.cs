namespace P2W.Cards.Application.Options;

public sealed class CardOptions
{
    public bool UseMockProviders { get; set; } = true;
    public int PriceRefreshHours { get; set; } = 6;
    public bool EnableRawSourceJsonStorage { get; set; } = true;
}

public sealed class CatalogImportOptions
{
    public int DefaultMaxRecords { get; set; } = 250;
    public int HardMaxRecords { get; set; } = 5000;
    public bool EnableDryRun { get; set; } = true;
}

public class ProviderSwitchOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "";
}

public sealed class MockProviderOptions : ProviderSwitchOptions;
public sealed class ScryfallOptions : ProviderSwitchOptions;
public sealed class MtgJsonOptions : ProviderSwitchOptions;
public sealed class CardKingdomOptions : ProviderSwitchOptions;

public sealed class TcgPlayerOptions : ProviderSwitchOptions
{
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string PartnerTag { get; set; } = "";
}

public sealed class EbayOptions : ProviderSwitchOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string OAuthBaseUrl { get; set; } = "";
    public string MarketplaceId { get; set; } = "EBAY_US";
    public string AffiliateCampaignId { get; set; } = "";
    public int MaxListingsPerProduct { get; set; } = 100;
    public int CacheHours { get; set; } = 6;
    public bool RequireHighConfidenceForMarketValues { get; set; } = true;
}

public sealed class JustTcgOptions : ProviderSwitchOptions
{
    public string ApiKey { get; set; } = "";
    public int MaxReferenceCards { get; set; } = 10;
}

public sealed class PokemonTcgOptions : ProviderSwitchOptions
{
    public string ApiKey { get; set; } = "";
}

public sealed class PriceChartingOptions : ProviderSwitchOptions
{
    public string ApiToken { get; set; } = "";
    public int RefreshHours { get; set; } = 24;
}

public sealed class MarketAggregationOptions
{
    public bool Enabled { get; set; }
    public int RefreshReferencePricesHours { get; set; } = 24;
    public int RefreshListingsHours { get; set; } = 6;
    public int ComputeMetricsHours { get; set; } = 6;
    public int RefreshRecentlyViewedHours { get; set; } = 12;
    public int RefreshWatchlistedHours { get; set; } = 12;
    public int MaxProductsPerRun { get; set; } = 500;
    public string DefaultCurrency { get; set; } = "USD";
    public bool StoreRawSourceJson { get; set; } = true;
}

public sealed class MarketDiagnosticsOptions
{
    public bool Enabled { get; set; }
    public bool IncludeSearchQueries { get; set; } = true;
    public bool IncludeMatchCandidates { get; set; } = true;
    public bool IncludeProviderPayloadHints { get; set; }
    public int MaxEventsPerRun { get; set; } = 200;
}

public sealed class MarketFeesOptions
{
    public decimal DefaultMarketplaceFeePercent { get; set; } = 13.25m;
    public decimal DefaultPaymentFeePercent { get; set; } = 2.9m;
    public decimal DefaultPaymentFixedFee { get; set; } = 0.30m;
    public decimal DefaultShippingCost { get; set; } = 4.50m;
}
