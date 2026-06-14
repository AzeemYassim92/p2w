using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.Options;

namespace P2W.Cards.Infrastructure.Providers.Scryfall;

public sealed class ScryfallApiClient(HttpClient http, IOptions<ScryfallOptions> options)
{
    public async Task<ScryfallPagedResult<ScryfallCardDto>> GetCardsAsync(int maxRecords, string? checkpointValue, CancellationToken ct)
    {
        PrepareHeaders();
        var query = Uri.EscapeDataString("game:paper");
        var start = ParseCheckpoint(checkpointValue, $"{BaseUrl}/cards/search?q={query}&unique=prints&order=released&dir=desc");
        return await GetPagedAsync<ScryfallCardDto>(start.Url, start.Offset, maxRecords, ct);
    }

    public async Task<ScryfallPagedResult<ScryfallSetDto>> GetSetsAsync(int maxRecords, string? checkpointValue, CancellationToken ct)
    {
        PrepareHeaders();
        var start = ParseCheckpoint(checkpointValue, $"{BaseUrl}/sets");
        return await GetPagedAsync<ScryfallSetDto>(start.Url, start.Offset, maxRecords, ct);
    }

    private async Task<ScryfallPagedResult<T>> GetPagedAsync<T>(string url, int offset, int maxRecords, CancellationToken ct)
    {
        var records = new List<T>();
        var nextUrl = url;
        var currentUrl = url;
        var currentOffset = offset;
        var hasMore = false;
        string? nextCheckpoint = null;

        while (records.Count < maxRecords && !string.IsNullOrWhiteSpace(nextUrl))
        {
            currentUrl = nextUrl;
            var response = await GetAsync<ScryfallListResponse<T>>(nextUrl, ct);
            if (response == null || response.Data.Count == 0)
            {
                hasMore = false;
                nextUrl = null;
                break;
            }

            var available = response.Data.Skip(currentOffset).ToArray();
            var remaining = maxRecords - records.Count;
            records.AddRange(available.Take(remaining));

            if (available.Length > remaining)
            {
                hasMore = true;
                nextCheckpoint = FormatCheckpoint(currentUrl, currentOffset + remaining);
                break;
            }

            hasMore = response.HasMore;
            nextUrl = response.NextPage;
            nextCheckpoint = hasMore && !string.IsNullOrWhiteSpace(nextUrl) ? nextUrl : null;
            currentOffset = 0;
            if (!hasMore)
            {
                nextUrl = null;
                break;
            }
        }

        return new ScryfallPagedResult<T>
        {
            Data = records.Take(maxRecords).ToArray(),
            NextCheckpointValue = hasMore ? nextCheckpoint : null,
            HasMore = hasMore
        };
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"Scryfall request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    private void PrepareHeaders()
    {
        if (http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("P2WCards", "0.1"));
        }
        if (!http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
        {
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    private string BaseUrl => string.IsNullOrWhiteSpace(options.Value.BaseUrl) ? "https://api.scryfall.com" : options.Value.BaseUrl.TrimEnd('/');

    private static ScryfallCheckpoint ParseCheckpoint(string? checkpointValue, string defaultUrl)
    {
        if (string.IsNullOrWhiteSpace(checkpointValue))
        {
            return new ScryfallCheckpoint(defaultUrl, 0);
        }

        const string offsetPrefix = "offset=";
        const string urlMarker = ";url=";
        if (!checkpointValue.StartsWith(offsetPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new ScryfallCheckpoint(checkpointValue, 0);
        }

        var markerIndex = checkpointValue.IndexOf(urlMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return new ScryfallCheckpoint(defaultUrl, 0);
        }

        var offsetText = checkpointValue[offsetPrefix.Length..markerIndex];
        var url = checkpointValue[(markerIndex + urlMarker.Length)..];
        return new ScryfallCheckpoint(string.IsNullOrWhiteSpace(url) ? defaultUrl : url, int.TryParse(offsetText, out var offset) && offset > 0 ? offset : 0);
    }

    private static string FormatCheckpoint(string url, int offset) => $"offset={offset};url={url}";
}

readonly record struct ScryfallCheckpoint(string Url, int Offset);

public sealed class ScryfallPagedResult<T>
{
    public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
    public string? NextCheckpointValue { get; set; }
    public bool HasMore { get; set; }
}
