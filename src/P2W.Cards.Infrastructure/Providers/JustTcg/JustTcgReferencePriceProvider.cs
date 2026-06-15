using System.Text.Json;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure.Providers.JustTcg;

public sealed class JustTcgReferencePriceProvider(JustTcgApiClient client, IOptions<JustTcgOptions> options, MarketDiagnosticTrail diagnostics) : IMarketplaceReferencePriceProvider
{
    public string SourceName => "JustTCG";
    public bool IsEnabled => options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<IReadOnlyList<ExternalReferencePriceDto>> GetReferencePricesAsync(CatalogProduct product, IReadOnlyList<ExternalProductMapping> mappings, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            diagnostics.Debug("justtcg.skipped", "JustTCG provider disabled or missing API key.");
            return Array.Empty<ExternalReferencePriceDto>();
        }

        var cards = await client.SearchCardsAsync(product, ct);
        diagnostics.Info("justtcg.search.complete", "JustTCG card search completed.", new { Count = cards.Count });
        if (diagnostics.IncludeMatchCandidates)
        {
            foreach (var candidate in cards.Take(5))
            {
                diagnostics.Debug("justtcg.search.candidate", "JustTCG candidate scored.", new
                {
                    candidate.Id,
                    candidate.Name,
                    candidate.Number,
                    Set = candidate.SetName ?? candidate.Set,
                    VariantCount = candidate.Variants.Count,
                    Score = MatchScore(product, candidate)
                });
            }
        }

        var match = cards
            .OrderByDescending(c => MatchScore(product, c))
            .FirstOrDefault(c => MatchScore(product, c) >= 3);
        if (match == null)
        {
            diagnostics.Warning("justtcg.search.no-match", "JustTCG did not return a confident card match.", new
            {
                product.Name,
                Set = product.CardSet?.Name,
                product.CardNumber
            });
            return Array.Empty<ExternalReferencePriceDto>();
        }

        var variant = SelectVariant(match);
        if (variant == null)
        {
            diagnostics.Warning("justtcg.price.empty", "JustTCG matched a card but no variant contained price values.", new
            {
                match.Id,
                match.Name,
                VariantCount = match.Variants.Count
            });
            return Array.Empty<ExternalReferencePriceDto>();
        }

        var market = MarketValue(variant);
        if (!market.HasValue)
        {
            diagnostics.Warning("justtcg.price.empty", "JustTCG selected variant had no usable market value.", new { match.Id, match.Name });
            return Array.Empty<ExternalReferencePriceDto>();
        }

        diagnostics.Info("justtcg.price.selected", "JustTCG selected a reference price.", new
        {
            match.Id,
            match.Name,
            variant.Condition,
            variant.Printing,
            variant.MarketPrice,
            variant.Price,
            variant.LowPrice,
            variant.MidPrice,
            variant.HighPrice
        });

        return new[]
        {
            new ExternalReferencePriceDto
            {
                SourceName = SourceName,
                Condition = NormalizeCondition(variant.Condition),
                MarketPrice = variant.MarketPrice ?? variant.ReadDecimal("market", "market_price", "tcgplayerMarketPrice") ?? market,
                LowPrice = variant.LowPrice ?? variant.ReadDecimal("low", "low_price"),
                MidPrice = variant.MidPrice ?? variant.ReadDecimal("mid", "mid_price"),
                HighPrice = variant.HighPrice ?? variant.ReadDecimal("high", "high_price"),
                UngradedPrice = market,
                RetailPrice = variant.Price ?? variant.ReadDecimal("price", "retailPrice"),
                Currency = string.IsNullOrWhiteSpace(variant.Currency) ? "USD" : variant.Currency!,
                CapturedAtUtc = DateTime.UtcNow,
                ExternalUrl = match.Url,
                RawSourceJson = JsonSerializer.Serialize(new
                {
                    source = "justtcg",
                    cardId = match.Id,
                    cardName = match.Name,
                    number = match.Number,
                    set = match.SetName ?? match.Set,
                    variant.Condition,
                    variant.Printing,
                    variant.Language
                })
            }
        };
    }

    private static JustTcgVariantDto? SelectVariant(JustTcgCardDto card)
        => card.Variants
            .Where(v => MarketValue(v).HasValue)
            .OrderByDescending(v => ConditionRank(v.Condition))
            .ThenByDescending(v => PrintingRank(v.Printing))
            .FirstOrDefault();

    private static decimal? MarketValue(JustTcgVariantDto variant)
        => variant.MarketPrice
            ?? variant.ReadDecimal("market", "market_price", "tcgplayerMarketPrice", "avgPrice")
            ?? variant.MidPrice
            ?? variant.ReadDecimal("mid", "mid_price")
            ?? variant.Price
            ?? variant.ReadDecimal("price", "retailPrice")
            ?? variant.LowPrice
            ?? variant.ReadDecimal("low", "low_price")
            ?? variant.HighPrice
            ?? variant.ReadDecimal("high", "high_price");

    private static int MatchScore(CatalogProduct product, JustTcgCardDto card)
    {
        var score = 0;
        if (card.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)) score += 3;
        if (!string.IsNullOrWhiteSpace(product.CardNumber) && card.Number?.Equals(product.CardNumber, StringComparison.OrdinalIgnoreCase) == true) score += 2;
        var setName = card.SetName ?? card.Set;
        if (!string.IsNullOrWhiteSpace(product.CardSet?.Name) && setName?.Equals(product.CardSet.Name, StringComparison.OrdinalIgnoreCase) == true) score += 2;
        if (card.Variants.Count > 0) score += 1;
        return score;
    }

    private static int ConditionRank(string? condition)
        => condition?.Replace(" ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant() switch
        {
            "nearmint" or "nm" => 3,
            "lightlyplayed" or "lp" => 2,
            _ => 1
        };

    private static int PrintingRank(string? printing)
        => printing?.Contains("normal", StringComparison.OrdinalIgnoreCase) == true ? 3
            : printing?.Contains("holo", StringComparison.OrdinalIgnoreCase) == true ? 2
            : 1;

    private static string NormalizeCondition(string? condition)
        => condition?.Replace(" ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant() switch
        {
            "nearmint" or "nm" => "NearMint",
            "lightlyplayed" or "lp" => "LightlyPlayed",
            "moderatelyplayed" or "mp" => "ModeratelyPlayed",
            "heavilyplayed" or "hp" => "HeavilyPlayed",
            "damaged" => "Damaged",
            _ => "Unknown"
        };
}
