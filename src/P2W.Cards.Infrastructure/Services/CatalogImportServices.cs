using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;

namespace P2W.Cards.Infrastructure.Services;

public static class CatalogTextNormalizer
{
    private static readonly Regex Punctuation = new("[^a-z0-9 ]", RegexOptions.Compiled);
    private static readonly Regex Spaces = new("\\s+", RegexOptions.Compiled);

    public static string NormalizeName(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("pokémon", "pokemon")
            .Replace("pokemon", "pokemon")
            .Replace("magic: the gathering", "mtg")
            .Replace("magic the gathering", "mtg");
        normalized = Punctuation.Replace(normalized, " ");
        return Spaces.Replace(normalized, " ").Trim();
    }

    public static string Slug(string value) => NormalizeName(value).Replace(" ", "-");
}

public sealed class CatalogImportService(CardsDbContext db, IEnumerable<IExternalCatalogProvider> providers, ICatalogProductMatchingService matching, IImportCheckpointService checkpoints, IOptions<CatalogImportOptions> options, ILogger<CatalogImportService> logger) : ICatalogImportService
{
    public async Task<CatalogImportPreviewDto> PreviewImportAsync(StartCatalogImportRequest request, CancellationToken ct)
    {
        if (!options.Value.EnableDryRun)
        {
            throw new InvalidOperationException("Catalog import dry run is disabled.");
        }

        var provider = GetProvider(request.SourceName);
        var context = await ToContextAsync(request, ct);
        var preview = await provider.PreviewAsync(context, ct);
        return await BuildPreviewDtoAsync(context, preview, ct);
    }

    public async Task<CatalogImportRunDto> StartImportAsync(StartCatalogImportRequest request, CancellationToken ct)
    {
        if (request.DryRun)
        {
            var preview = await PreviewImportAsync(request, ct);
            return new CatalogImportRunDto
            {
                CatalogImportRunId = Guid.Empty,
                SourceName = preview.SourceName,
                ImportType = preview.ImportType,
                StartedUtc = DateTime.UtcNow,
                FinishedUtc = DateTime.UtcNow,
                RecordsProcessed = preview.ExternalRecordsRead,
                RecordsCreated = preview.WouldCreate,
                RecordsUpdated = preview.WouldUpdate,
                RecordsSkipped = preview.WouldSkip,
                Status = "Preview",
                CheckpointValue = preview.CheckpointValue,
                NextCheckpointValue = preview.NextCheckpointValue,
                HasMore = preview.HasMore
            };
        }

        var provider = GetProvider(request.SourceName);
        var context = await ToContextAsync(request, ct);
        var run = new CatalogImportRun
        {
            Id = Guid.NewGuid(),
            SourceName = provider.SourceName,
            ImportType = context.ImportType,
            StartedUtc = DateTime.UtcNow,
            Status = "Started"
        };
        db.CatalogImportRuns.Add(run);
        await db.SaveChangesAsync(ct);

        try
        {
            var result = await provider.ImportAsync(context, ct);
            await UpsertSetsAsync(result.Sets, run, ct);
            foreach (var externalProduct in result.Products.Take(context.MaxRecords))
            {
                try
                {
                    var outcome = await UpsertProductAsync(externalProduct, context, ct);
                    run.RecordsProcessed++;
                    if (outcome == "Created") run.RecordsCreated++;
                    else if (outcome == "Updated") run.RecordsUpdated++;
                    else run.RecordsSkipped++;
                }
                catch (Exception ex)
                {
                    run.ErrorCount++;
                    db.CatalogImportErrors.Add(new CatalogImportError
                    {
                        Id = Guid.NewGuid(),
                        CatalogImportRunId = run.Id,
                        SourceName = externalProduct.SourceName,
                        ExternalId = externalProduct.ExternalId,
                        ErrorMessage = ex.Message,
                        RawSourceJson = externalProduct.RawSourceJson,
                        CreatedUtc = DateTime.UtcNow
                    });
                    logger.LogWarning(ex, "Catalog import failed for {SourceName} {ExternalId}", externalProduct.SourceName, externalProduct.ExternalId);
                }
            }

            run.Status = run.ErrorCount > 0 ? "Partial" : "Completed";
            run.FinishedUtc = DateTime.UtcNow;
            run.Notes = BuildRunNotes(context.CheckpointValue, result.NextCheckpointValue, result.HasMore);
            await db.SaveChangesAsync(ct);
            if (context.SaveCheckpoint)
            {
                await checkpoints.SaveCheckpointAsync(provider.SourceName, context.ImportType, result.NextCheckpointValue ?? "complete", ct);
            }
            var dto = ToDto(run);
            dto.CheckpointValue = context.CheckpointValue;
            dto.NextCheckpointValue = result.NextCheckpointValue;
            dto.HasMore = result.HasMore;
            return dto;
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.FinishedUtc = DateTime.UtcNow;
            run.Notes = ex.Message;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    public async Task<CatalogImportRunDetailDto?> GetImportRunAsync(Guid importRunId, CancellationToken ct)
    {
        var run = await db.CatalogImportRuns.FirstOrDefaultAsync(r => r.Id == importRunId, ct);
        if (run == null) return null;
        var dto = new CatalogImportRunDetailDto
        {
            CatalogImportRunId = run.Id,
            SourceName = run.SourceName,
            ImportType = run.ImportType,
            StartedUtc = run.StartedUtc,
            FinishedUtc = run.FinishedUtc,
            RecordsProcessed = run.RecordsProcessed,
            RecordsCreated = run.RecordsCreated,
            RecordsUpdated = run.RecordsUpdated,
            RecordsSkipped = run.RecordsSkipped,
            ErrorCount = run.ErrorCount,
            Status = run.Status,
            Notes = run.Notes,
            Errors = await db.CatalogImportErrors.Where(e => e.CatalogImportRunId == run.Id).OrderByDescending(e => e.CreatedUtc).Select(e => new CatalogImportErrorDto
            {
                CatalogImportErrorId = e.Id,
                SourceName = e.SourceName,
                ExternalId = e.ExternalId,
                ErrorMessage = e.ErrorMessage,
                CreatedUtc = e.CreatedUtc
            }).ToListAsync(ct)
        };
        return dto;
    }

    public async Task<IReadOnlyList<CatalogImportRunDto>> GetImportRunsAsync(string? sourceName, int take, CancellationToken ct)
        => await db.CatalogImportRuns
            .Where(r => string.IsNullOrWhiteSpace(sourceName) || r.SourceName == sourceName)
            .OrderByDescending(r => r.StartedUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(r => ToDto(r))
            .ToListAsync(ct);

    private IExternalCatalogProvider GetProvider(string sourceName)
    {
        var provider = providers.FirstOrDefault(p => p.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
        if (provider == null) throw new InvalidOperationException($"Catalog provider '{sourceName}' is not registered.");
        if (!provider.IsEnabled) throw new InvalidOperationException($"Catalog provider '{sourceName}' is disabled.");
        return provider;
    }

    private async Task<CatalogImportContext> ToContextAsync(StartCatalogImportRequest request, CancellationToken ct)
    {
        var importType = string.IsNullOrWhiteSpace(request.ImportType) ? "Cards" : request.ImportType;
        var checkpointValue = string.IsNullOrWhiteSpace(request.CheckpointValue) ? null : request.CheckpointValue.Trim();
        if (checkpointValue == null && request.UseCheckpoint)
        {
            checkpointValue = (await checkpoints.GetCheckpointAsync(request.SourceName, importType, ct))?.CheckpointValue;
        }
        if (checkpointValue?.Equals("complete", StringComparison.OrdinalIgnoreCase) == true)
        {
            checkpointValue = null;
        }

        return new CatalogImportContext
        {
            SourceName = request.SourceName,
            GameSlug = request.GameSlug,
            ImportType = importType,
            MaxRecords = Math.Clamp(request.MaxRecords <= 0 ? options.Value.DefaultMaxRecords : request.MaxRecords, 1, options.Value.HardMaxRecords),
            IncludeImages = request.IncludeImages,
            UpdateExistingProducts = request.UpdateExistingProducts,
            CreateMissingProducts = request.CreateMissingProducts,
            UseCheckpoint = request.UseCheckpoint,
            SaveCheckpoint = request.SaveCheckpoint,
            CheckpointValue = checkpointValue
        };
    }

    private async Task<CatalogImportPreviewDto> BuildPreviewDtoAsync(CatalogImportContext context, ExternalCatalogImportResult preview, CancellationToken ct)
    {
        var rows = new List<CatalogImportPreviewRowDto>();
        foreach (var product in preview.Products.Take(context.MaxRecords))
        {
            var match = await matching.FindBestMatchAsync(product, ct);
            var canUpdate = IsWritableCatalogMatch(match);
            var action = match.CatalogProductId == null || !canUpdate
                ? context.CreateMissingProducts ? "Create" : "Skip"
                : context.UpdateExistingProducts ? "Update" : "MatchOnly";
            rows.Add(new CatalogImportPreviewRowDto
            {
                ExternalId = product.ExternalId,
                SourceName = product.SourceName,
                Name = product.Name,
                SetName = product.SetName,
                CardNumber = product.CardNumber,
                Rarity = product.Rarity,
                Action = action,
                ConfidenceScore = match.ConfidenceScore,
                MatchedCatalogProductId = match.CatalogProductId,
                MatchedCatalogProductName = match.CatalogProductName
            });
        }

        return new CatalogImportPreviewDto
        {
            SourceName = context.SourceName,
            GameSlug = context.GameSlug,
            ImportType = context.ImportType,
            ExternalRecordsRead = rows.Count,
            ExistingMatches = rows.Count(r => r.MatchedCatalogProductId != null),
            WouldCreate = rows.Count(r => r.Action == "Create"),
            WouldUpdate = rows.Count(r => r.Action == "Update"),
            WouldSkip = rows.Count(r => r.Action == "Skip"),
            CheckpointValue = context.CheckpointValue,
            NextCheckpointValue = preview.NextCheckpointValue,
            HasMore = preview.HasMore,
            SampleRows = rows.Take(25).ToArray()
        };
    }

    private static string BuildRunNotes(string? checkpointValue, string? nextCheckpointValue, bool hasMore)
        => $"Checkpoint: {checkpointValue ?? "start"}; Next checkpoint: {nextCheckpointValue ?? "complete"}; Has more: {hasMore}";

    private async Task UpsertSetsAsync(IReadOnlyList<ExternalCatalogSetDto> sets, CatalogImportRun run, CancellationToken ct)
    {
        foreach (var set in sets)
        {
            var game = await db.Games.FirstOrDefaultAsync(g => g.Slug == set.GameSlug, ct);
            if (game == null) continue;
            var normalized = CatalogTextNormalizer.NormalizeName(set.Name);
            var existing = await db.CardSets.FirstOrDefaultAsync(s => s.GameId == game.Id && s.NormalizedName == normalized, ct);
            if (existing == null)
            {
                db.CardSets.Add(new CardSet
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    Name = set.Name,
                    NormalizedName = normalized,
                    Slug = CatalogTextNormalizer.Slug(set.Name),
                    Code = set.Code,
                    ReleaseDate = set.ReleaseDate,
                    IsActive = true,
                    IsUpcoming = set.ReleaseDate > DateTime.UtcNow,
                    LogoUrl = set.LogoUrl,
                    SymbolUrl = set.SymbolUrl,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> UpsertProductAsync(ExternalCatalogProductDto externalProduct, CatalogImportContext context, CancellationToken ct)
    {
        var game = await db.Games.FirstAsync(g => g.Slug == externalProduct.GameSlug, ct);
        var category = await db.ProductCategories.FirstAsync(c => c.Slug == "raw-singles", ct);
        var set = await EnsureSetAsync(game.Id, externalProduct, ct);
        var match = await matching.FindBestMatchAsync(externalProduct, ct);
        var now = DateTime.UtcNow;
        CatalogProduct? product = null;
        var created = false;
        var canUpdateMatch = IsWritableCatalogMatch(match);

        if (match.CatalogProductId != null && canUpdateMatch)
        {
            product = await db.CatalogProducts.FirstAsync(p => p.Id == match.CatalogProductId.Value, ct);
        }
        else if (context.CreateMissingProducts)
        {
            product = new CatalogProduct { Id = Guid.NewGuid(), CreatedUtc = now };
            db.CatalogProducts.Add(product);
            created = true;
        }

        if (product == null) return "Skipped";
        if (!created && !context.UpdateExistingProducts) return "Skipped";

        product.GameId = game.Id;
        product.CardSetId = set.Id;
        product.ProductCategoryId = category.Id;
        product.Name = externalProduct.Name;
        product.NormalizedName = CatalogTextNormalizer.NormalizeName(externalProduct.Name);
        product.Slug = CatalogTextNormalizer.Slug(externalProduct.Name);
        product.ProductType = "SingleCard";
        product.CardNumber = externalProduct.CardNumber;
        product.Rarity = externalProduct.Rarity;
        product.Artist = externalProduct.Artist;
        product.ImageUrl = context.IncludeImages ? externalProduct.ImageUrl : product.ImageUrl;
        product.ReleaseDate = externalProduct.ReleaseDate;
        product.IsSingleCard = true;
        product.IsSealed = false;
        product.IsGradedEligible = true;
        product.IsActive = true;
        product.IsFeatured = product.IsFeatured || created;
        product.IsTrending = product.IsTrending || created;
        product.UpdatedUtc = now;

        await db.SaveChangesAsync(ct);
        await UpsertVariantsAsync(product.Id, externalProduct.VariantNames, ct);
        var mappingConfidence = created ? 0.95m : match.ConfidenceScore;
        await matching.CreateOrUpdateMappingAsync(product.Id, externalProduct, mappingConfidence == 0 ? 0.95m : mappingConfidence, ct);
        return created ? "Created" : "Updated";
    }

    private static bool IsWritableCatalogMatch(CatalogProductMatchResult match)
        => match.CatalogProductId != null &&
           (match.MatchReason == "ExistingMapping" || match.MatchReason == "GameSetNumberName");

    private async Task<CardSet> EnsureSetAsync(Guid gameId, ExternalCatalogProductDto externalProduct, CancellationToken ct)
    {
        var setName = string.IsNullOrWhiteSpace(externalProduct.SetName) ? "Unknown Set" : externalProduct.SetName;
        var normalized = CatalogTextNormalizer.NormalizeName(setName);
        var set = await db.CardSets.FirstOrDefaultAsync(s => s.GameId == gameId && s.NormalizedName == normalized, ct);
        if (set != null) return set;

        set = new CardSet
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = setName,
            NormalizedName = normalized,
            Slug = CatalogTextNormalizer.Slug(setName),
            Code = externalProduct.SetCode,
            ReleaseDate = externalProduct.ReleaseDate,
            IsActive = true,
            IsUpcoming = externalProduct.ReleaseDate > DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        db.CardSets.Add(set);
        await db.SaveChangesAsync(ct);
        return set;
    }

    private async Task UpsertVariantsAsync(Guid productId, IReadOnlyList<string> variantNames, CancellationToken ct)
    {
        foreach (var variantName in variantNames.DefaultIfEmpty("Normal").Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await db.ProductVariants.AnyAsync(v => v.CatalogProductId == productId && v.VariantName == variantName, ct)) continue;
            db.ProductVariants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                CatalogProductId = productId,
                VariantName = variantName,
                Language = "English",
                IsFoil = variantName.Contains("foil", StringComparison.OrdinalIgnoreCase),
                IsReverseHolo = variantName.Contains("reverse", StringComparison.OrdinalIgnoreCase),
                IsFirstEdition = variantName.Contains("first", StringComparison.OrdinalIgnoreCase),
                IsPromo = variantName.Contains("promo", StringComparison.OrdinalIgnoreCase),
                CreatedUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static CatalogImportRunDto ToDto(CatalogImportRun run) => new()
    {
        CatalogImportRunId = run.Id,
        SourceName = run.SourceName,
        ImportType = run.ImportType,
        StartedUtc = run.StartedUtc,
        FinishedUtc = run.FinishedUtc,
        RecordsProcessed = run.RecordsProcessed,
        RecordsCreated = run.RecordsCreated,
        RecordsUpdated = run.RecordsUpdated,
        RecordsSkipped = run.RecordsSkipped,
        ErrorCount = run.ErrorCount,
        Status = run.Status,
        Notes = run.Notes
    };
}

public sealed class CatalogProductMatchingService(CardsDbContext db) : ICatalogProductMatchingService
{
    public async Task<CatalogProductMatchResult> FindBestMatchAsync(ExternalCatalogProductDto externalProduct, CancellationToken ct)
    {
        var mapping = await db.ExternalProductMappings.Include(m => m.CatalogProduct)
            .FirstOrDefaultAsync(m => m.SourceName == externalProduct.SourceName && m.ExternalId == externalProduct.ExternalId, ct);
        if (mapping != null)
        {
            return new CatalogProductMatchResult { CatalogProductId = mapping.CatalogProductId, CatalogProductName = mapping.CatalogProduct?.Name, ConfidenceScore = 1.00m, MatchReason = "ExistingMapping" };
        }

        var game = await db.Games.FirstOrDefaultAsync(g => g.Slug == externalProduct.GameSlug, ct);
        if (game == null) return new CatalogProductMatchResult { ConfidenceScore = 0m, MatchReason = "NoGame" };
        var name = CatalogTextNormalizer.NormalizeName(externalProduct.Name);
        var setName = CatalogTextNormalizer.NormalizeName(externalProduct.SetName ?? "");

        var query = db.CatalogProducts.Include(p => p.CardSet).Where(p => p.GameId == game.Id && p.NormalizedName == name);
        var exact = await query.FirstOrDefaultAsync(p => p.CardSet != null && p.CardSet.NormalizedName == setName && p.CardNumber == externalProduct.CardNumber, ct);
        if (exact != null) return Match(exact, 0.95m, "GameSetNumberName");
        var setMatch = await query.FirstOrDefaultAsync(p => p.CardSet != null && p.CardSet.NormalizedName == setName, ct);
        if (setMatch != null) return Match(setMatch, 0.85m, "GameSetName");
        var nameMatch = await query.FirstOrDefaultAsync(ct);
        if (nameMatch != null) return Match(nameMatch, 0.70m, "GameName");
        return new CatalogProductMatchResult { ConfidenceScore = 0m, MatchReason = "None" };
    }

    public async Task<ExternalProductMappingDto> CreateOrUpdateMappingAsync(Guid catalogProductId, ExternalCatalogProductDto externalProduct, decimal confidenceScore, CancellationToken ct)
    {
        var mapping = await db.ExternalProductMappings.FirstOrDefaultAsync(m => m.SourceName == externalProduct.SourceName && m.ExternalId == externalProduct.ExternalId, ct);
        if (mapping == null)
        {
            mapping = new ExternalProductMapping { Id = Guid.NewGuid(), SourceName = externalProduct.SourceName, ExternalId = externalProduct.ExternalId, CreatedUtc = DateTime.UtcNow };
            db.ExternalProductMappings.Add(mapping);
        }

        mapping.CatalogProductId = catalogProductId;
        mapping.ExternalUrl = externalProduct.ExternalUrl;
        mapping.ExternalSlug = CatalogTextNormalizer.Slug(externalProduct.Name);
        mapping.ConfidenceScore = confidenceScore;
        mapping.MappingStatus = confidenceScore >= 0.85m ? "AutoMatched" : "NeedsReview";
        mapping.LastVerifiedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ExternalProductMappingDto { SourceName = mapping.SourceName, ExternalId = mapping.ExternalId, ExternalUrl = mapping.ExternalUrl, ExternalSlug = mapping.ExternalSlug, ConfidenceScore = mapping.ConfidenceScore };
    }

    private static CatalogProductMatchResult Match(CatalogProduct product, decimal confidence, string reason) => new() { CatalogProductId = product.Id, CatalogProductName = product.Name, ConfidenceScore = confidence, MatchReason = reason };
}

public sealed class ImportCheckpointService(CardsDbContext db) : IImportCheckpointService
{
    public async Task<CatalogImportCheckpointDto?> GetCheckpointAsync(string sourceName, string importType, CancellationToken ct)
        => await db.CatalogImportCheckpoints.Where(c => c.SourceName == sourceName && c.ImportType == importType).Select(c => new CatalogImportCheckpointDto { SourceName = c.SourceName, ImportType = c.ImportType, CheckpointValue = c.CheckpointValue, UpdatedUtc = c.UpdatedUtc }).FirstOrDefaultAsync(ct);

    public async Task SaveCheckpointAsync(string sourceName, string importType, string checkpointValue, CancellationToken ct)
    {
        var checkpoint = await db.CatalogImportCheckpoints.FirstOrDefaultAsync(c => c.SourceName == sourceName && c.ImportType == importType, ct);
        if (checkpoint == null)
        {
            checkpoint = new CatalogImportCheckpoint { Id = Guid.NewGuid(), SourceName = sourceName, ImportType = importType };
            db.CatalogImportCheckpoints.Add(checkpoint);
        }
        checkpoint.CheckpointValue = checkpointValue;
        checkpoint.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
