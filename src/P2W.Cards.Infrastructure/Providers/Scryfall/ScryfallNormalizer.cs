using System.Text.Json;
using P2W.Cards.Application.DTOs;

namespace P2W.Cards.Infrastructure.Providers.Scryfall;

public static class ScryfallNormalizer
{
    public static ExternalCatalogProductDto ToExternalProduct(ScryfallCardDto card) => new()
    {
        SourceName = "Scryfall",
        ExternalId = card.Id,
        Name = card.Name,
        GameSlug = "magic-the-gathering",
        SetName = card.SetName,
        SetCode = card.SetCode?.ToUpperInvariant(),
        ReleaseDate = card.ReleasedAt,
        CardNumber = card.CollectorNumber,
        Rarity = card.Rarity,
        Artist = card.Artist,
        ImageUrl = card.ImageUris?.Normal ?? card.CardFaces?.FirstOrDefault()?.ImageUris?.Normal,
        ExternalUrl = card.ScryfallUri ?? card.Uri,
        VariantNames = BuildVariants(card),
        RawSourceJson = JsonSerializer.Serialize(card)
    };

    public static ExternalCatalogSetDto ToExternalSet(ScryfallSetDto set) => new()
    {
        SourceName = "Scryfall",
        ExternalId = set.Id,
        Name = set.Name,
        GameSlug = "magic-the-gathering",
        Code = set.Code?.ToUpperInvariant(),
        ReleaseDate = set.ReleasedAt,
        SymbolUrl = set.IconSvgUri
    };

    private static IReadOnlyList<string> BuildVariants(ScryfallCardDto card)
    {
        var variants = new List<string>();
        if (card.Nonfoil || card.Finishes?.Contains("nonfoil") == true) variants.Add("nonfoil");
        if (card.Foil || card.Finishes?.Contains("foil") == true) variants.Add("foil");
        if (card.Finishes?.Contains("etched") == true) variants.Add("etched");
        return variants.Count == 0 ? new[] { "nonfoil" } : variants;
    }
}
