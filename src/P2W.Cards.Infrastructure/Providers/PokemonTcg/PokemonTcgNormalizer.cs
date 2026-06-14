using System.Text.Json;
using P2W.Cards.Application.DTOs;

namespace P2W.Cards.Infrastructure.Providers.PokemonTcg;

public static class PokemonTcgNormalizer
{
    public static ExternalCatalogProductDto ToExternalProduct(PokemonTcgCardDto card) => new()
    {
        SourceName = "PokemonTCG",
        ExternalId = card.Id,
        Name = card.Name,
        GameSlug = "pokemon",
        SetName = card.Set?.Name,
        SetCode = card.Set?.Id?.ToUpperInvariant(),
        ReleaseDate = ParseDate(card.Set?.ReleaseDate),
        CardNumber = card.Number,
        Rarity = card.Rarity,
        Artist = card.Artist,
        ImageUrl = card.Images?.Large ?? card.Images?.Small,
        ExternalUrl = card.TcgPlayer?.Url,
        VariantNames = BuildVariants(card),
        RawSourceJson = JsonSerializer.Serialize(card)
    };

    public static ExternalCatalogSetDto ToExternalSet(PokemonTcgSetDto set) => new()
    {
        SourceName = "PokemonTCG",
        ExternalId = set.Id,
        Name = set.Name,
        GameSlug = "pokemon",
        Code = set.Id.ToUpperInvariant(),
        ReleaseDate = ParseDate(set.ReleaseDate),
        LogoUrl = set.Images?.Logo,
        SymbolUrl = set.Images?.Symbol
    };

    private static IReadOnlyList<string> BuildVariants(PokemonTcgCardDto card)
    {
        var variants = new List<string> { "normal" };
        if (card.Rarity?.Contains("holo", StringComparison.OrdinalIgnoreCase) == true) variants.Add("holofoil");
        variants.Add("reverse holo");
        return variants.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed;
        }

        var parts = value.Split(new[] { '/', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3
            && int.TryParse(parts[0], out var year)
            && int.TryParse(parts[1], out var month)
            && int.TryParse(parts[2], out var day)
            && year is >= 1900 and <= 2100
            && month is >= 1 and <= 12
            && day is >= 1 and <= 31)
        {
            try
            {
                return new DateTime(year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        return null;
    }
}
