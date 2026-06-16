using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.Options;

namespace P2W.Cards.Infrastructure.Providers.PokemonTcg;

public sealed class PokemonTcgApiClient(HttpClient http, IOptions<PokemonTcgOptions> options, ILogger<PokemonTcgApiClient> logger)
{
    public async Task<PokemonTcgPagedResult<PokemonTcgCardDto>> GetCardsAsync(int maxRecords, string? checkpointValue, CancellationToken ct)
    {
        AddApiKey();
        return await GetPagedAsync<PokemonTcgCardDto>("cards", maxRecords, checkpointValue, "-set.releaseDate", ct);
    }

    public async Task<PokemonTcgPagedResult<PokemonTcgSetDto>> GetSetsAsync(int maxRecords, string? checkpointValue, CancellationToken ct)
    {
        AddApiKey();
        return await GetPagedAsync<PokemonTcgSetDto>("sets", maxRecords, checkpointValue, "-releaseDate", ct);
    }

    public async Task<PokemonTcgCardDto?> GetCardByIdAsync(string externalId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        AddApiKey();
        var url = $"{BaseUrl}/cards/{Uri.EscapeDataString(externalId)}";
        using var httpResponse = await http.GetAsync(url, ct);
        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("PokemonTCG card id {ExternalId} returned 404. Continuing with search fallback.", externalId);
            return null;
        }
        if (IsTransientFailure(httpResponse.StatusCode))
        {
            logger.LogWarning("PokemonTCG card id {ExternalId} returned transient status {StatusCode}. Skipping reference lookup for this attempt.", externalId, (int)httpResponse.StatusCode);
            return null;
        }

        httpResponse.EnsureSuccessStatusCode();
        var response = await httpResponse.Content.ReadFromJsonAsync<PokemonTcgSingleResponse<PokemonTcgCardDto>>(cancellationToken: ct);
        return response?.Data;
    }

    public async Task<IReadOnlyList<PokemonTcgCardDto>> SearchCardsAsync(string query, int take, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<PokemonTcgCardDto>();
        }

        AddApiKey();
        var pageSize = Math.Clamp(take, 1, 25);
        var url = $"{BaseUrl}/cards?q={Uri.EscapeDataString(query)}&page=1&pageSize={pageSize}";
        using var httpResponse = await http.GetAsync(url, ct);
        if (IsTransientFailure(httpResponse.StatusCode))
        {
            logger.LogWarning("PokemonTCG search returned transient status {StatusCode} for query {Query}. Returning no candidates for this attempt.", (int)httpResponse.StatusCode, query);
            return Array.Empty<PokemonTcgCardDto>();
        }

        httpResponse.EnsureSuccessStatusCode();
        var response = await httpResponse.Content.ReadFromJsonAsync<PokemonTcgListResponse<PokemonTcgCardDto>>(cancellationToken: ct);
        return response?.Data ?? new List<PokemonTcgCardDto>();
    }

    private async Task<PokemonTcgPagedResult<T>> GetPagedAsync<T>(string route, int maxRecords, string? checkpointValue, string orderBy, CancellationToken ct)
    {
        var records = new List<T>();
        var page = ParseCheckpoint(checkpointValue);
        var hasMore = false;
        var nextPage = page;

        while (records.Count < maxRecords)
        {
            var pageSize = Math.Clamp(maxRecords - records.Count, 1, 250);
            var url = $"{BaseUrl}/{route}?page={page}&pageSize={pageSize}&orderBy={Uri.EscapeDataString(orderBy)}";
            var response = await http.GetFromJsonAsync<PokemonTcgListResponse<T>>(url, ct);
            if (response == null || response.Data.Count == 0)
            {
                hasMore = false;
                nextPage = page;
                break;
            }

            records.AddRange(response.Data);
            var currentPage = response.Page <= 0 ? page : response.Page;
            var currentPageSize = response.PageSize <= 0 ? pageSize : response.PageSize;
            nextPage = currentPage + 1;
            hasMore = currentPage * currentPageSize < response.TotalCount;

            if (!hasMore)
            {
                break;
            }

            page = nextPage;
        }

        return new PokemonTcgPagedResult<T>
        {
            Data = records.Take(maxRecords).ToArray(),
            NextCheckpointValue = hasMore ? nextPage.ToString() : null,
            HasMore = hasMore,
            TotalCount = records.Count == 0 ? 0 : records.Count < maxRecords ? records.Count : await ReadTotalCountAsync(route, orderBy, ct)
        };
    }

    private async Task<int> ReadTotalCountAsync(string route, string orderBy, CancellationToken ct)
    {
        var url = $"{BaseUrl}/{route}?page=1&pageSize=1&orderBy={Uri.EscapeDataString(orderBy)}";
        var response = await http.GetFromJsonAsync<PokemonTcgListResponse<object>>(url, ct);
        return response?.TotalCount ?? 0;
    }

    private void AddApiKey()
    {
        if (http.DefaultRequestHeaders.Contains("X-Api-Key")) return;
        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            http.DefaultRequestHeaders.Add("X-Api-Key", options.Value.ApiKey);
        }
        else
        {
            logger.LogWarning("PokemonTCG API key is empty. Continuing with unauthenticated requests.");
        }
    }

    private string BaseUrl => string.IsNullOrWhiteSpace(options.Value.BaseUrl) ? "https://api.pokemontcg.io/v2" : options.Value.BaseUrl.TrimEnd('/');

    private static int ParseCheckpoint(string? checkpointValue)
        => int.TryParse(checkpointValue, out var page) && page > 0 ? page : 1;

    private static bool IsTransientFailure(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;
}

public sealed class PokemonTcgPagedResult<T>
{
    public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
    public string? NextCheckpointValue { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
}
