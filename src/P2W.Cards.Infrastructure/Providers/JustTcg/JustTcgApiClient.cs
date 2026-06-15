using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure.Providers.JustTcg;

public sealed class JustTcgApiClient(HttpClient http, IOptions<JustTcgOptions> options, MarketDiagnosticTrail diagnostics)
{
    public async Task<IReadOnlyList<JustTcgCardDto>> SearchCardsAsync(CatalogProduct product, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            diagnostics.Warning("justtcg.auth.missing", "JustTCG API key is missing.");
            return Array.Empty<JustTcgCardDto>();
        }

        var cards = new List<JustTcgCardDto>();
        foreach (var url in BuildSearchUrls(product))
        {
            diagnostics.Debug("justtcg.search.query", "Searching JustTCG cards.", new { Url = Redact(url) });
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", options.Value.ApiKey);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                diagnostics.Warning("justtcg.search.http", "JustTCG search returned non-success status.", new { Url = Redact(url), Status = (int)response.StatusCode });
                continue;
            }

            var payload = await response.Content.ReadFromJsonAsync<JustTcgCardSearchResponse>(cancellationToken: ct);
            var results = payload?.Results ?? Array.Empty<JustTcgCardDto>();
            diagnostics.Debug("justtcg.search.result", "JustTCG search returned candidates.", new { Url = Redact(url), Count = results.Count });
            cards.AddRange(results);
            if (cards.Count > 0)
            {
                break;
            }
        }

        return cards
            .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();
    }

    private IReadOnlyList<string> BuildSearchUrls(CatalogProduct product)
    {
        var take = Math.Clamp(options.Value.MaxReferenceCards, 1, 25);
        var game = product.Game?.Name.Contains("Pokemon", StringComparison.OrdinalIgnoreCase) == true ? "Pokemon" : product.Game?.Name ?? "";
        var setName = product.CardSet?.Name ?? "";
        var number = product.CardNumber ?? "";
        var name = product.Name;
        var baseUrl = BaseUrl;

        var urls = new List<string>
        {
            Query(baseUrl, new Dictionary<string, string?>
            {
                ["game"] = game,
                ["name"] = name,
                ["set"] = setName,
                ["number"] = number,
                ["limit"] = take.ToString()
            }),
            Query(baseUrl, new Dictionary<string, string?>
            {
                ["game"] = game,
                ["search"] = $"{name} {setName} {number}".Trim(),
                ["limit"] = take.ToString()
            }),
            Query(baseUrl, new Dictionary<string, string?>
            {
                ["q"] = $"{name} {setName} {number}".Trim(),
                ["limit"] = take.ToString()
            }),
            Query(baseUrl, new Dictionary<string, string?>
            {
                ["name"] = name,
                ["number"] = number,
                ["limit"] = take.ToString()
            })
        };

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private string BaseUrl => $"{(string.IsNullOrWhiteSpace(options.Value.BaseUrl) ? "https://api.justtcg.com/v1" : options.Value.BaseUrl.TrimEnd('/'))}/cards";

    private static string Query(string baseUrl, IReadOnlyDictionary<string, string?> values)
    {
        var query = string.Join("&", values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));
        return string.IsNullOrWhiteSpace(query) ? baseUrl : $"{baseUrl}?{query}";
    }

    private static string Redact(string url)
        => url.Replace("apiKey=", "apiKey=redacted-", StringComparison.OrdinalIgnoreCase);
}
