using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace P2W.DealFinder.Infrastructure.Providers.JustTcg;

public sealed class JustTcgApiClient(HttpClient http, JustTcgOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<IReadOnlyList<JustTcgGameDto>> GetGamesAsync(CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, Url("games"), body: null, ct);
        var payload = await ReadPayloadAsync<JustTcgGameDto>(response, ct);
        return payload.Items;
    }

    public async Task<IReadOnlyList<JustTcgSetDto>> GetSetsAsync(string? game, int limit, int offset, CancellationToken ct)
    {
        var url = Url("sets", new Dictionary<string, string?>
        {
            ["game"] = game,
            ["limit"] = Math.Clamp(limit, 1, 250).ToString(CultureInfo.InvariantCulture),
            ["offset"] = Math.Max(offset, 0).ToString(CultureInfo.InvariantCulture)
        });

        using var response = await SendAsync(HttpMethod.Get, url, body: null, ct);
        var payload = await ReadPayloadAsync<JustTcgSetDto>(response, ct);
        return payload.Items;
    }

    public async Task<IReadOnlyList<JustTcgCardDto>> SearchCardsAsync(JustTcgCardSearch search, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, CardsUrl(search), body: null, ct);
        var payload = await ReadPayloadAsync<JustTcgCardDto>(response, ct);
        return payload.Items;
    }

    public async Task<IReadOnlyList<JustTcgCardDto>> PostCardsAsync(object requestBody, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, Url("cards"), requestBody, ct);
        var payload = await ReadPayloadAsync<JustTcgCardDto>(response, ct);
        return payload.Items;
    }

    public async Task<JsonDocument> GetRawCardsAsync(JustTcgCardSearch search, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, CardsUrl(search), body: null, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private string CardsUrl(JustTcgCardSearch search)
        => Url("cards", new Dictionary<string, string?>
        {
            ["cardId"] = search.CardId,
            ["variantId"] = search.VariantId,
            ["game"] = search.Game,
            ["set"] = search.Set,
            ["q"] = search.Q,
            ["name"] = search.Name,
            ["number"] = search.Number,
            ["tcgplayerId"] = search.TcgPlayerId,
            ["tcgplayerSkuId"] = search.TcgPlayerSkuId,
            ["mtgjsonId"] = search.MtgJsonId,
            ["scryfallId"] = search.ScryfallId,
            ["condition"] = search.Condition,
            ["printing"] = search.Printing,
            ["min_price"] = FormatDecimal(search.MinPrice),
            ["include_null_prices"] = FormatBool(search.IncludeNullPrices),
            ["updated_after"] = search.UpdatedAfter,
            ["orderBy"] = search.OrderBy,
            ["order"] = search.Order,
            ["includePriceHistory"] = FormatBool(search.IncludePriceHistory),
            ["includeStatistics"] = FormatBool(search.IncludeStatistics),
            ["priceHistoryDuration"] = search.PriceHistoryDuration,
            ["limit"] = Math.Clamp(search.Limit, 1, 250).ToString(CultureInfo.InvariantCulture),
            ["offset"] = Math.Max(search.Offset, 0).ToString(CultureInfo.InvariantCulture)
        });

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("JustTCG API key is missing. Set JUSTTCG_API_KEY or keep it in the old ecompt2 appsettings.Local.json.");
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.UserAgent.ParseAdd("P2W-DealFinder/0.1");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await http.SendAsync(request, ct);
    }

    private static async Task<JustTcgResponse<T>> ReadPayloadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"JustTCG returned {(int)response.StatusCode} {response.ReasonPhrase}: {text}");
        }

        return JsonSerializer.Deserialize<JustTcgResponse<T>>(text, JsonOptions) ?? new JustTcgResponse<T>();
    }

    private string Url(string resource, IReadOnlyDictionary<string, string?>? query = null)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://api.justtcg.com/v1"
            : options.BaseUrl.TrimEnd('/');

        var builder = new StringBuilder($"{baseUrl}/{resource.TrimStart('/')}");
        if (query is null) return builder.ToString();

        var values = query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}")
            .ToArray();

        if (values.Length > 0)
        {
            builder.Append('?');
            builder.Append(string.Join("&", values));
        }

        return builder.ToString();
    }

    private static string? FormatDecimal(decimal? value)
        => value?.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? FormatBool(bool? value)
        => value.HasValue ? value.Value.ToString().ToLowerInvariant() : null;
}