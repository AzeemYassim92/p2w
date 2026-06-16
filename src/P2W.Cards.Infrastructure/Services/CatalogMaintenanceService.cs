using Microsoft.EntityFrameworkCore;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.PokemonTcg;

namespace P2W.Cards.Infrastructure.Services;

public sealed class CatalogMaintenanceService(
    CardsDbContext db,
    PokemonTcgApiClient pokemonTcg,
    LocalSessionLog sessionLog) : ICatalogMaintenanceService
{
    public async Task<CatalogCompletenessDto> GetCompletenessAsync(string gameSlug, CancellationToken ct)
    {
        var slug = string.IsNullOrWhiteSpace(gameSlug) ? "pokemon" : gameSlug.Trim().ToLowerInvariant();
        var sourceName = ExternalSourceForGame(slug);
        var sets = await db.CardSets.Include(s => s.Game)
            .Where(s => s.Game != null && s.Game.Slug == slug)
            .OrderByDescending(s => s.ReleaseDate ?? DateTime.MinValue)
            .Take(12)
            .ToListAsync(ct);
        var products = db.CatalogProducts.Include(p => p.Game).Where(p => p.Game != null && p.Game.Slug == slug && p.IsActive);

        var rows = new List<CatalogSetCompletenessRowDto>();
        foreach (var set in sets)
        {
            var setProducts = db.CatalogProducts.Where(p => p.CardSetId == set.Id && p.IsActive);
            rows.Add(new CatalogSetCompletenessRowDto
            {
                CardSetId = set.Id,
                SetName = set.Name,
                SetCode = set.Code,
                ReleaseDate = set.ReleaseDate,
                ProductCount = await setProducts.CountAsync(ct),
                ProductsMissingImage = await setProducts.CountAsync(p => p.ImageUrl == null || p.ImageUrl == "", ct),
                ProductsMissingDescription = await setProducts.CountAsync(p => p.Description == null || p.Description == "", ct),
                ProductsWithoutExternalMapping = await setProducts.CountAsync(p => !db.ExternalProductMappings.Any(m => m.CatalogProductId == p.Id && m.SourceName == sourceName), ct)
            });
        }

        var dto = new CatalogCompletenessDto
        {
            GameSlug = slug,
            SetCount = await db.CardSets.CountAsync(s => s.Game != null && s.Game.Slug == slug, ct),
            ProductCount = await products.CountAsync(ct),
            ProductsMissingImage = await products.CountAsync(p => p.ImageUrl == null || p.ImageUrl == "", ct),
            ProductsMissingDescription = await products.CountAsync(p => p.Description == null || p.Description == "", ct),
            ProductsMissingRarity = await products.CountAsync(p => p.Rarity == null || p.Rarity == "", ct),
            ProductsMissingCardNumber = await products.CountAsync(p => p.CardNumber == null || p.CardNumber == "", ct),
            ProductsWithoutExternalMapping = await products.CountAsync(p => !db.ExternalProductMappings.Any(m => m.CatalogProductId == p.Id && m.SourceName == sourceName), ct),
            SetsWithoutProducts = await db.CardSets.CountAsync(s => s.Game != null && s.Game.Slug == slug && !db.CatalogProducts.Any(p => p.CardSetId == s.Id && p.IsActive), ct),
            RecentSets = rows
        };

        sessionLog.Info("catalog", "catalog.completeness", "Catalog completeness report generated.", dto);
        return dto;
    }

    public async Task<CatalogMetadataBackfillResultDto> BackfillMetadataAsync(CatalogMetadataBackfillRequest request, CancellationToken ct)
    {
        var slug = string.IsNullOrWhiteSpace(request.GameSlug) ? "pokemon" : request.GameSlug.Trim().ToLowerInvariant();
        if (!slug.Equals("pokemon", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only PokemonTCG metadata backfill is implemented right now.");
        }

        var result = new CatalogMetadataBackfillResultDto { GameSlug = slug };
        var notes = new List<string>();
        var take = Math.Clamp(request.Take <= 0 ? 50 : request.Take, 1, 500);
        var query = db.CatalogProducts
            .Include(p => p.Game)
            .Include(p => p.CardSet)
            .Include(p => p.Variants)
            .Where(p => p.Game != null && p.Game.Slug == slug && p.IsActive);

        if (request.CardSetId.HasValue)
        {
            query = query.Where(p => p.CardSetId == request.CardSetId.Value);
        }

        if (request.MissingOnly)
        {
            query = query.Where(p =>
                p.Description == null || p.Description == ""
                || p.ImageUrl == null || p.ImageUrl == ""
                || p.Rarity == null || p.Rarity == ""
                || p.CardNumber == null || p.CardNumber == ""
                || !db.ProductVariants.Any(v => v.CatalogProductId == p.Id));
        }

        var products = await query
            .OrderByDescending(p => p.CardSet != null ? p.CardSet.ReleaseDate : null)
            .ThenBy(p => p.Name)
            .Take(take)
            .ToListAsync(ct);

        sessionLog.Info("catalog", "catalog.backfill.start", "Pokemon metadata backfill started.", new
        {
            request.CardSetId,
            request.MissingOnly,
            request.DryRun,
            Take = take,
            ProductCount = products.Count
        });

        foreach (var product in products)
        {
            result.ProductsScanned++;
            try
            {
                var mapping = await db.ExternalProductMappings
                    .Where(m => m.CatalogProductId == product.Id && m.SourceName == "PokemonTCG")
                    .OrderByDescending(m => m.ConfidenceScore ?? 0m)
                    .FirstOrDefaultAsync(ct);

                if (mapping == null)
                {
                    result.MissingMappings++;
                    result.ProductsSkipped++;
                    continue;
                }

                var card = await pokemonTcg.GetCardByIdAsync(mapping.ExternalId, ct);
                if (card == null)
                {
                    result.ProviderMisses++;
                    result.ProductsSkipped++;
                    continue;
                }

                var external = PokemonTcgNormalizer.ToExternalProduct(card);
                var changed = WouldChange(product, external) || HasMissingVariant(product, external.VariantNames);
                if (!changed)
                {
                    result.ProductsSkipped++;
                    continue;
                }

                result.ProductsUpdated++;
                notes.Add($"{product.Name} / {product.CardSet?.Name} / {product.CardNumber}");
                if (request.DryRun)
                {
                    continue;
                }

                ApplyMetadata(product, external);
                UpsertVariants(product, external.VariantNames);
                mapping.ExternalUrl = external.ExternalUrl;
                mapping.ExternalSlug = CatalogTextNormalizer.Slug(external.Name);
                mapping.LastVerifiedUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.ProductsSkipped++;
                sessionLog.Error("catalog", "catalog.backfill.product.failed", "Pokemon metadata backfill failed for product.", ex, new
                {
                    product.Id,
                    product.Name,
                    product.CardNumber
                });
            }
        }

        if (!request.DryRun)
        {
            await db.SaveChangesAsync(ct);
        }

        result.Notes = notes.Take(25).ToArray();
        sessionLog.Info("catalog", "catalog.backfill.complete", "Pokemon metadata backfill completed.", result);
        return result;
    }

    private static string ExternalSourceForGame(string gameSlug)
        => gameSlug.Equals("pokemon", StringComparison.OrdinalIgnoreCase) ? "PokemonTCG" : gameSlug;

    private static bool WouldChange(CatalogProduct product, ExternalCatalogProductDto external)
        => product.Name != external.Name
            || product.CardNumber != external.CardNumber
            || product.Rarity != external.Rarity
            || product.Artist != external.Artist
            || (string.IsNullOrWhiteSpace(product.Description) && !string.IsNullOrWhiteSpace(external.Description))
            || (string.IsNullOrWhiteSpace(product.ImageUrl) && !string.IsNullOrWhiteSpace(external.ImageUrl))
            || product.ReleaseDate != external.ReleaseDate;

    private static bool HasMissingVariant(CatalogProduct product, IReadOnlyList<string> variantNames)
        => variantNames.DefaultIfEmpty("Normal")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Any(variant => !product.Variants.Any(v => v.VariantName.Equals(variant, StringComparison.OrdinalIgnoreCase)));

    private static void ApplyMetadata(CatalogProduct product, ExternalCatalogProductDto external)
    {
        product.Name = external.Name;
        product.NormalizedName = CatalogTextNormalizer.NormalizeName(external.Name);
        product.Slug = CatalogTextNormalizer.Slug(external.Name);
        product.CardNumber = external.CardNumber;
        product.Rarity = external.Rarity;
        product.Artist = external.Artist;
        product.Description = string.IsNullOrWhiteSpace(external.Description) ? product.Description : external.Description;
        product.ImageUrl = string.IsNullOrWhiteSpace(external.ImageUrl) ? product.ImageUrl : external.ImageUrl;
        product.ReleaseDate = external.ReleaseDate;
        product.UpdatedUtc = DateTime.UtcNow;
    }

    private static void UpsertVariants(CatalogProduct product, IReadOnlyList<string> variantNames)
    {
        foreach (var variantName in variantNames.DefaultIfEmpty("Normal").Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (product.Variants.Any(v => v.VariantName.Equals(variantName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            product.Variants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                CatalogProductId = product.Id,
                VariantName = variantName,
                Language = "English",
                IsFoil = variantName.Contains("foil", StringComparison.OrdinalIgnoreCase),
                IsReverseHolo = variantName.Contains("reverse", StringComparison.OrdinalIgnoreCase),
                IsFirstEdition = variantName.Contains("first", StringComparison.OrdinalIgnoreCase),
                IsPromo = variantName.Contains("promo", StringComparison.OrdinalIgnoreCase),
                CreatedUtc = DateTime.UtcNow
            });
        }
    }
}
