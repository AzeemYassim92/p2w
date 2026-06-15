using System.Text.Json;
using System.Text.Json.Serialization;

namespace P2W.Cards.Infrastructure.Providers.JustTcg;

public sealed class JustTcgCardSearchResponse
{
    [JsonPropertyName("data")]
    public List<JustTcgCardDto> Data { get; set; } = new();

    [JsonPropertyName("cards")]
    public List<JustTcgCardDto>? Cards { get; set; }

    public IReadOnlyList<JustTcgCardDto> Results => Data.Count > 0 ? Data : Cards ?? new List<JustTcgCardDto>();
}

public sealed class JustTcgCardDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("set")]
    public string? Set { get; set; }

    [JsonPropertyName("setName")]
    public string? SetName { get; set; }

    [JsonPropertyName("game")]
    public string? Game { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("variants")]
    public List<JustTcgVariantDto> Variants { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = new();
}

public sealed class JustTcgVariantDto
{
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("printing")]
    public string? Printing { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("marketPrice")]
    public decimal? MarketPrice { get; set; }

    [JsonPropertyName("lowPrice")]
    public decimal? LowPrice { get; set; }

    [JsonPropertyName("midPrice")]
    public decimal? MidPrice { get; set; }

    [JsonPropertyName("highPrice")]
    public decimal? HighPrice { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; } = new();

    public decimal? ReadDecimal(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (ExtensionData.TryGetValue(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric))
                {
                    return numeric;
                }
                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }
}
