using System.Text.Json.Serialization;

namespace P2W.Cards.Infrastructure.Providers.Scryfall;

public sealed class ScryfallListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
    [JsonPropertyName("next_page")]
    public string? NextPage { get; set; }
}

public sealed class ScryfallCardDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("set")]
    public string? SetCode { get; set; }
    [JsonPropertyName("set_name")]
    public string? SetName { get; set; }
    [JsonPropertyName("collector_number")]
    public string? CollectorNumber { get; set; }
    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }
    [JsonPropertyName("released_at")]
    public DateTime? ReleasedAt { get; set; }
    [JsonPropertyName("scryfall_uri")]
    public string? ScryfallUri { get; set; }
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
    [JsonPropertyName("image_uris")]
    public ScryfallImageUris? ImageUris { get; set; }
    [JsonPropertyName("card_faces")]
    public List<ScryfallCardFaceDto>? CardFaces { get; set; }
    [JsonPropertyName("foil")]
    public bool Foil { get; set; }
    [JsonPropertyName("nonfoil")]
    public bool Nonfoil { get; set; }
    [JsonPropertyName("finishes")]
    public List<string>? Finishes { get; set; }
}

public sealed class ScryfallCardFaceDto
{
    [JsonPropertyName("image_uris")]
    public ScryfallImageUris? ImageUris { get; set; }
}

public sealed class ScryfallImageUris
{
    [JsonPropertyName("normal")]
    public string? Normal { get; set; }
}

public sealed class ScryfallSetDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("released_at")]
    public DateTime? ReleasedAt { get; set; }
    [JsonPropertyName("icon_svg_uri")]
    public string? IconSvgUri { get; set; }
}
