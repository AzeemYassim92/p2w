using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.Options;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbayBrowseApiClient(HttpClient http, IOptions<EbayOptions> options, EbayRateLimiter limiter, MarketDiagnosticTrail diagnostics)
{
    private string? cachedAccessToken;
    private DateTime expiresAtUtc;

    public async Task<string> GetApplicationAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(cachedAccessToken) && expiresAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            return cachedAccessToken;
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.ClientId}:{options.Value.ClientSecret}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OAuthBaseUrl}/identity/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "https://api.ebay.com/oauth/api_scope"
        });

        diagnostics.Info("ebay.oauth.start", "Requesting eBay application access token.");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            diagnostics.Warning("ebay.oauth.failed", "eBay OAuth token request failed.", new { Status = (int)response.StatusCode });
        }
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<EbayOAuthTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("eBay OAuth response was empty.");
        cachedAccessToken = token.AccessToken;
        expiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn));
        diagnostics.Info("ebay.oauth.complete", "eBay application access token acquired.", new { token.ExpiresIn });
        return cachedAccessToken;
    }

    public async Task<EbayBrowseSearchResponse> SearchAsync(string query, string accessToken, int limit, CancellationToken ct)
    {
        await limiter.WaitAsync(ct);
        var baseUrl = string.IsNullOrWhiteSpace(options.Value.BaseUrl) ? "https://api.ebay.com" : options.Value.BaseUrl.TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/buy/browse/v1/item_summary/search?q={Uri.EscapeDataString(query)}&limit={limit}&filter=buyingOptions:%7BFIXED_PRICE%7D");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", options.Value.MarketplaceId);
        diagnostics.Debug("ebay.browse.query", "Searching eBay Browse API.", new { Query = query, Limit = limit, options.Value.MarketplaceId });
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            diagnostics.Warning("ebay.browse.failed", "eBay Browse search failed.", new { Status = (int)response.StatusCode, Query = query });
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EbayBrowseSearchResponse>(cancellationToken: ct) ?? new EbayBrowseSearchResponse();
        diagnostics.Info("ebay.browse.complete", "eBay Browse search returned item summaries.", new { Query = query, Count = result.ItemSummaries.Count });
        return result;
    }

    private string OAuthBaseUrl
        => string.IsNullOrWhiteSpace(options.Value.OAuthBaseUrl)
            ? (options.Value.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase) ? "https://api.sandbox.ebay.com" : "https://api.ebay.com")
            : options.Value.OAuthBaseUrl.TrimEnd('/');
}
