namespace P2W.DealFinder.Infrastructure.Providers.JustTcg;

public sealed class JustTcgOptions
{
    public string BaseUrl { get; init; } = "https://api.justtcg.com/v1";
    public string? ApiKey { get; init; }
    public int DefaultLimit { get; init; } = 20;
}
