using System.Text.Json;
using System.Text.Json.Serialization;

namespace P2W.DealFinder.Infrastructure.Providers.JustTcg;

public sealed class JustTcgResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; init; } = new();

    [JsonPropertyName("results")]
    public List<T>? Results { get; init; }

    [JsonPropertyName("cards")]
    public List<T>? Cards { get; init; }

    [JsonPropertyName("sets")]
    public List<T>? Sets { get; init; }

    [JsonPropertyName("games")]
    public List<T>? Games { get; init; }

    [JsonPropertyName("total")]
    public int? Total { get; init; }

    [JsonPropertyName("meta")]
    public JustTcgMetaDto? Meta { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();

    public IReadOnlyList<T> Items
        => Data.Count > 0 ? Data
            : Results is { Count: > 0 } ? Results
            : Cards is { Count: > 0 } ? Cards
            : Sets is { Count: > 0 } ? Sets
            : Games is { Count: > 0 } ? Games
            : Array.Empty<T>();
}

public sealed class JustTcgMetaDto
{
    [JsonPropertyName("total")]
    public int? Total { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("offset")]
    public int? Offset { get; init; }

    [JsonPropertyName("hasMore")]
    public bool? HasMore { get; init; }
}

public sealed class JustTcgGameDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("game_value_usd")]
    public decimal? GameValueUsd { get; init; }

    [JsonPropertyName("sealed_count")]
    public int? SealedCount { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();
}

public sealed class JustTcgSetDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("game")]
    public string? Game { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; init; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDateSnake { get; init; }

    [JsonPropertyName("cardCount")]
    public int? CardCount { get; init; }

    [JsonPropertyName("set_value_usd")]
    public decimal? SetValueUsd { get; init; }

    [JsonPropertyName("variants_count")]
    public int? VariantsCount { get; init; }

    [JsonPropertyName("sealed_count")]
    public int? SealedCount { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();
}

public sealed class JustTcgCardDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("game")]
    public string? Game { get; init; }

    [JsonPropertyName("set")]
    public string? Set { get; init; }

    [JsonPropertyName("set_name")]
    public string? SetName { get; init; }

    [JsonPropertyName("number")]
    public string? Number { get; init; }

    [JsonPropertyName("rarity")]
    public string? Rarity { get; init; }

    [JsonPropertyName("tcgplayerId")]
    public string? TcgPlayerId { get; init; }

    [JsonPropertyName("details")]
    public JsonElement? Details { get; init; }

    [JsonPropertyName("variants")]
    public List<JustTcgVariantDto> Variants { get; init; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();
}

public sealed class JustTcgVariantDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("condition")]
    public string? Condition { get; init; }

    [JsonPropertyName("printing")]
    public string? Printing { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("tcgplayerSkuId")]
    public string? TcgPlayerSkuId { get; init; }

    [JsonPropertyName("lastUpdated")]
    public long? LastUpdated { get; init; }

    [JsonPropertyName("priceChange24hr")]
    public decimal? PriceChange24h { get; init; }

    [JsonPropertyName("priceChange7d")]
    public decimal? PriceChange7d { get; init; }

    [JsonPropertyName("priceChange30d")]
    public decimal? PriceChange30d { get; init; }

    [JsonPropertyName("priceChange90d")]
    public decimal? PriceChange90d { get; init; }

    [JsonPropertyName("avgPrice")]
    public decimal? AvgPrice7d { get; init; }

    [JsonPropertyName("avgPrice30d")]
    public decimal? AvgPrice30d { get; init; }

    [JsonPropertyName("avgPrice90d")]
    public decimal? AvgPrice90d { get; init; }

    [JsonPropertyName("priceHistory")]
    public List<JustTcgPricePointDto> PriceHistory { get; init; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();

    public decimal? BestKnownPrice
        => Price ?? ReadDecimal("marketPrice", "market", "market_price", "tcgplayerMarketPrice", "ungraded", "ungradedPrice");

    public decimal? ReadDecimal(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!ExtensionData.TryGetValue(key, out var value)) continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric)) return numeric;
            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed)) return parsed;
        }

        return null;
    }
}

public sealed class JustTcgPricePointDto
{
    [JsonPropertyName("p")]
    public decimal? P { get; init; }

    [JsonPropertyName("t")]
    public long? T { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();
}

public sealed record JustTcgCardSearch(
    string? CardId = null,
    string? VariantId = null,
    string? Game = null,
    string? Set = null,
    string? Q = null,
    string? Name = null,
    string? Number = null,
    string? TcgPlayerId = null,
    string? TcgPlayerSkuId = null,
    string? MtgJsonId = null,
    string? ScryfallId = null,
    string? Condition = null,
    string? Printing = null,
    decimal? MinPrice = null,
    bool? IncludeNullPrices = null,
    string? UpdatedAfter = null,
    string? OrderBy = null,
    string? Order = null,
    bool? IncludePriceHistory = null,
    bool? IncludeStatistics = null,
    string? PriceHistoryDuration = null,
    int Limit = 20,
    int Offset = 0);