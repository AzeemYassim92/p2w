namespace P2W.Cards.Application.Options;

public sealed class CardOptions
{
    public bool UseMockProviders { get; set; } = true;
    public int PriceRefreshHours { get; set; } = 6;
    public bool EnableRawSourceJsonStorage { get; set; } = true;
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
    public string MarketplaceId { get; set; } = "EBAY_US";
    public string AffiliateCampaignId { get; set; } = "";
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
