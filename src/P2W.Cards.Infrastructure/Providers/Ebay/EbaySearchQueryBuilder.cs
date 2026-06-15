using P2W.Cards.Domain.Entities;

namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbaySearchQueryBuilder
{
    public string Build(CatalogProduct product)
    {
        var game = product.Game?.Slug ?? product.Game?.Name?.ToLowerInvariant() ?? "";
        var setName = product.CardSet?.Name ?? "";
        var setCode = product.CardSet?.Code ?? "";
        var typeDisplay = ProductTypeDisplay(product.ProductType);

        if (product.IsSealed)
        {
            return game.Contains("one-piece")
                ? $"\"{setName}\" \"{typeDisplay}\" one piece tcg sealed -proxy -custom -digital -code -replica"
                : $"\"{setName}\" \"{typeDisplay}\" pokemon sealed -proxy -custom -digital -code -replica";
        }

        if (game.Contains("one-piece"))
        {
            return $"\"{product.Name}\" \"{product.CardNumber}\" \"{setCode}\" one piece tcg card -proxy -custom -digital -code -replica -jumbo";
        }

        return $"\"{product.Name}\" \"{product.CardNumber}\" \"{setName}\" pokemon card -proxy -custom -digital -code -replica -jumbo";
    }

    private static string ProductTypeDisplay(string productType)
        => productType switch
        {
            "BoosterPack" => "booster pack",
            "BoosterBox" => "booster box",
            "EliteTrainerBox" => "elite trainer box",
            "StarterDeck" => "starter deck",
            _ => productType
        };
}
