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
            HasMore = hasMore
        };
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
}

public sealed class PokemonTcgPagedResult<T>
{
    public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
    public string? NextCheckpointValue { get; set; }
    public bool HasMore { get; set; }
}
