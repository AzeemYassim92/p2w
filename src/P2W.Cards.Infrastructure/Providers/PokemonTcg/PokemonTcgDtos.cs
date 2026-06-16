using System.Text.Json.Serialization;

namespace P2W.Cards.Infrastructure.Providers.PokemonTcg;

public sealed class PokemonTcgListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    [JsonPropertyName("count")]
    public int Count { get; set; }
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public sealed class PokemonTcgSingleResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class PokemonTcgCardDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("number")]
    public string? Number { get; set; }
    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }
    [JsonPropertyName("supertype")]
    public string? Supertype { get; set; }
    [JsonPropertyName("subtypes")]
    public List<string>? Subtypes { get; set; }
    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
    [JsonPropertyName("set")]
    public PokemonTcgSetDto? Set { get; set; }
    [JsonPropertyName("images")]
    public PokemonTcgImagesDto? Images { get; set; }
    [JsonPropertyName("tcgplayer")]
    public PokemonTcgPlayerDto? TcgPlayer { get; set; }
}

public sealed class PokemonTcgSetDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("series")]
    public string? Series { get; set; }
    [JsonPropertyName("printedTotal")]
    public int? PrintedTotal { get; set; }
    [JsonPropertyName("total")]
    public int? Total { get; set; }
    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }
    [JsonPropertyName("images")]
    public PokemonTcgSetImagesDto? Images { get; set; }
}

public sealed class PokemonTcgImagesDto
{
    [JsonPropertyName("small")]
    public string? Small { get; set; }
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

public sealed class PokemonTcgSetImagesDto
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}

public sealed class PokemonTcgPlayerDto
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("prices")]
    public Dictionary<string, PokemonTcgPriceDto>? Prices { get; set; }
}

public sealed class PokemonTcgPriceDto
{
    [JsonPropertyName("low")]
    public decimal? Low { get; set; }

    [JsonPropertyName("mid")]
    public decimal? Mid { get; set; }

    [JsonPropertyName("high")]
    public decimal? High { get; set; }

    [JsonPropertyName("market")]
    public decimal? Market { get; set; }

    [JsonPropertyName("directLow")]
    public decimal? DirectLow { get; set; }
}
