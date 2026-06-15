using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure.Providers.PokemonTcg;

public sealed class PokemonTcgReferencePriceProvider(PokemonTcgApiClient client, IOptions<PokemonTcgOptions> options, ILogger<PokemonTcgReferencePriceProvider> logger, MarketDiagnosticTrail diagnostics) : IMarketplaceReferencePriceProvider
{
    public string SourceName => "PokemonTCG";
    public bool IsEnabled => options.Value.Enabled;

    public async Task<IReadOnlyList<ExternalReferencePriceDto>> GetReferencePricesAsync(CatalogProduct product, IReadOnlyList<ExternalProductMapping> mappings, CancellationToken ct)
    {
        if (!IsEnabled || product.Game?.Slug != "pokemon")
        {
            return Array.Empty<ExternalReferencePriceDto>();
        }

        var mapping = mappings
            .Where(m => m.SourceName.Equals(SourceName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.LastVerifiedUtc ?? m.CreatedUtc)
            .FirstOrDefault();
        diagnostics.Info("pokemontcg.mapping", mapping == null ? "No PokemonTCG mapping found; search fallback will be used." : "PokemonTCG mapping found.", new
        {
            product.Id,
            MappingExternalId = mapping?.ExternalId,
            mapping?.MappingStatus,
            mapping?.ConfidenceScore
        });

        var card = mapping == null
            ? null
            : await TryGetCardByIdAsync(mapping.ExternalId, product, ct);
        if (mapping != null)
        {
            diagnostics.Info("pokemontcg.card-by-id", card == null ? "Mapped PokemonTCG card id returned no card." : "Mapped PokemonTCG card id returned a card.", new
            {
                mapping.ExternalId,
                CardId = card?.Id,
                CardName = card?.Name,
                HasTcgPlayer = card?.TcgPlayer != null,
                PriceVariantCount = card?.TcgPlayer?.Prices?.Count ?? 0
            });
        }

        if (card?.TcgPlayer?.Prices?.Count > 0 != true)
        {
            card = await TryFindCardByProductFieldsAsync(product, ct);
        }

        var price = SelectPrice(card);
        if (card?.TcgPlayer == null || price == null)
        {
            logger.LogInformation(
                "PokemonTCG reference price skipped for {ProductId}; no mapping/search match with TCGplayer price payload.",
                product.Id);
            diagnostics.Warning("pokemontcg.price.empty", "No PokemonTCG match with a TCGplayer price payload was found.", new
            {
                product.Id,
                product.Name,
                Set = product.CardSet?.Name,
                product.CardNumber
            });
            return Array.Empty<ExternalReferencePriceDto>();
        }

        var market = price.Price.Market ?? price.Price.Mid ?? price.Price.Low ?? price.Price.DirectLow ?? price.Price.High;
        if (!market.HasValue)
        {
            diagnostics.Warning("pokemontcg.price.empty", "PokemonTCG matched a card but all selected price values were null.", new
            {
                card.Id,
                card.Name,
                price.Variant
            });
            return Array.Empty<ExternalReferencePriceDto>();
        }

        diagnostics.Info("pokemontcg.price.selected", "PokemonTCG selected a TCGplayer price variant.", new
        {
            card.Id,
            card.Name,
            price.Variant,
            price.Price.Market,
            price.Price.Low,
            price.Price.Mid,
            price.Price.High,
            price.Price.DirectLow
        });

        return new[]
        {
            new ExternalReferencePriceDto
            {
                SourceName = SourceName,
                MarketplaceSourceId = CardsDbContext.PokemonTcgMarketplaceSourceId,
                Condition = "NearMint",
                MarketPrice = price.Price.Market,
                LowPrice = price.Price.Low,
                MidPrice = price.Price.Mid,
                HighPrice = price.Price.High,
                UngradedPrice = market,
                Currency = "USD",
                CapturedAtUtc = DateTime.UtcNow,
                ExternalUrl = card.TcgPlayer.Url ?? mapping?.ExternalUrl,
                RawSourceJson = JsonSerializer.Serialize(new
                {
                    source = "pokemon-tcg-api",
                    priceSource = "tcgplayer",
                    variant = price.Variant,
                    cardId = card.Id,
                    updatedAt = card.TcgPlayer.UpdatedAt
                })
            }
        };
    }

    private async Task<PokemonTcgCardDto?> TryGetCardByIdAsync(string externalId, CatalogProduct product, CancellationToken ct)
    {
        try
        {
            return await client.GetCardByIdAsync(externalId, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            diagnostics.Warning("pokemontcg.transient", "PokemonTCG card id lookup timed out; refresh will continue without reference price for this product.", new
            {
                product.Id,
                product.Name,
                ExternalId = externalId
            });
            return null;
        }
        catch (HttpRequestException ex)
        {
            diagnostics.Warning("pokemontcg.transient", "PokemonTCG card id lookup failed; refresh will continue without reference price for this product.", new
            {
                product.Id,
                product.Name,
                ExternalId = externalId,
                ex.Message
            });
            return null;
        }
    }

    private async Task<PokemonTcgCardDto?> TryFindCardByProductFieldsAsync(CatalogProduct product, CancellationToken ct)
    {
        try
        {
            return await FindCardByProductFieldsAsync(product, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            diagnostics.Warning("pokemontcg.transient", "PokemonTCG search lookup timed out; refresh will continue without reference price for this product.", new
            {
                product.Id,
                product.Name,
                Set = product.CardSet?.Name,
                product.CardNumber
            });
            return null;
        }
        catch (HttpRequestException ex)
        {
            diagnostics.Warning("pokemontcg.transient", "PokemonTCG search lookup failed; refresh will continue without reference price for this product.", new
            {
                product.Id,
                product.Name,
                Set = product.CardSet?.Name,
                product.CardNumber,
                ex.Message
            });
            return null;
        }
    }

    private async Task<PokemonTcgCardDto?> FindCardByProductFieldsAsync(CatalogProduct product, CancellationToken ct)
    {
        var queries = BuildCandidateQueries(product);
        foreach (var query in queries)
        {
            if (diagnostics.IncludeSearchQueries)
            {
                diagnostics.Debug("pokemontcg.search.query", "Searching PokemonTCG cards.", new { Query = query });
            }
            var cards = await client.SearchCardsAsync(query, 8, ct);
            diagnostics.Debug("pokemontcg.search.result", "PokemonTCG search returned candidates.", new { Query = query, Count = cards.Count });
            if (diagnostics.IncludeMatchCandidates)
            {
                foreach (var candidate in cards.Take(5))
                {
                    diagnostics.Debug("pokemontcg.search.candidate", "PokemonTCG search candidate scored.", new
                    {
                        candidate.Id,
                        candidate.Name,
                        Number = candidate.Number,
                        SetId = candidate.Set?.Id,
                        SetName = candidate.Set?.Name,
                        HasTcgPlayer = candidate.TcgPlayer != null,
                        PriceVariantCount = candidate.TcgPlayer?.Prices?.Count ?? 0,
                        Score = MatchScore(product, candidate)
                    });
                }
            }
            var match = cards
                .OrderByDescending(c => MatchScore(product, c))
                .FirstOrDefault(c => MatchScore(product, c) >= 3);
            if (match != null)
            {
                logger.LogInformation("PokemonTCG reference price resolved {ProductId} by search query {Query} to {ExternalId}.", product.Id, query, match.Id);
                diagnostics.Info("pokemontcg.search.match", "PokemonTCG search resolved a card.", new
                {
                    Query = query,
                    match.Id,
                    match.Name,
                    Number = match.Number,
                    SetId = match.Set?.Id,
                    SetName = match.Set?.Name,
                    Score = MatchScore(product, match),
                    HasTcgPlayer = match.TcgPlayer != null,
                    PriceVariantCount = match.TcgPlayer?.Prices?.Count ?? 0
                });
                return match;
            }
        }

        diagnostics.Warning("pokemontcg.search.no-match", "PokemonTCG search could not resolve a confident card match.", new
        {
            product.Id,
            product.Name,
            Set = product.CardSet?.Name,
            SetCode = product.CardSet?.Code,
            product.CardNumber,
            QueryCount = queries.Count
        });
        return null;
    }

    private static IReadOnlyList<string> BuildCandidateQueries(CatalogProduct product)
    {
        var name = EscapeQueryValue(product.Name);

        var queries = new List<string>();
        var setCode = product.CardSet?.Code?.Trim().ToLowerInvariant();
        var setName = EscapeQueryValue(product.CardSet?.Name);
        var number = EscapeQueryValue(product.CardNumber);

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(setCode) && !string.IsNullOrWhiteSpace(number))
        {
            queries.Add($"name:\"{name}\" set.id:{setCode} number:\"{number}\"");
        }
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(setName) && !string.IsNullOrWhiteSpace(number))
        {
            queries.Add($"name:\"{name}\" set.name:\"{setName}\" number:\"{number}\"");
        }
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(number))
        {
            queries.Add($"name:\"{name}\" number:\"{number}\"");
        }
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(setCode))
        {
            queries.Add($"name:\"{name}\" set.id:{setCode}");
        }
        if (!string.IsNullOrWhiteSpace(setCode) && !string.IsNullOrWhiteSpace(number))
        {
            queries.Add($"set.id:{setCode} number:\"{number}\"");
        }
        if (!string.IsNullOrWhiteSpace(setName) && !string.IsNullOrWhiteSpace(number))
        {
            queries.Add($"set.name:\"{setName}\" number:\"{number}\"");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            queries.Add($"name:\"{name}\"");
        }
        return queries.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int MatchScore(CatalogProduct product, PokemonTcgCardDto card)
    {
        var score = 0;
        if (card.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)) score += 3;
        if (!string.IsNullOrWhiteSpace(product.CardNumber) && card.Number?.Equals(product.CardNumber, StringComparison.OrdinalIgnoreCase) == true) score += 2;
        if (!string.IsNullOrWhiteSpace(product.CardSet?.Code) && card.Set?.Id.Equals(product.CardSet.Code, StringComparison.OrdinalIgnoreCase) == true) score += 2;
        if (!string.IsNullOrWhiteSpace(product.CardSet?.Name) && card.Set?.Name.Equals(product.CardSet.Name, StringComparison.OrdinalIgnoreCase) == true) score += 2;
        if (card.TcgPlayer?.Prices?.Count > 0) score += 1;
        return score;
    }

    private static string? EscapeQueryValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static SelectedPokemonPrice? SelectPrice(PokemonTcgCardDto? card)
    {
        var prices = card?.TcgPlayer?.Prices;
        if (prices == null || prices.Count == 0)
        {
            return null;
        }

        var preferred = new[] { "normal", "holofoil", "reverseHolofoil", "1stEditionNormal", "1stEditionHolofoil" };
        foreach (var key in preferred)
        {
            if (prices.TryGetValue(key, out var price))
            {
                return new SelectedPokemonPrice(key, price);
            }
        }

        var fallback = prices.First();
        return new SelectedPokemonPrice(fallback.Key, fallback.Value);
    }

    private sealed record SelectedPokemonPrice(string Variant, PokemonTcgPriceDto Price);
}
