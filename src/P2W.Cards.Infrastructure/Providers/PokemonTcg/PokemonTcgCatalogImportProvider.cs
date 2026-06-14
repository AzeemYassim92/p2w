using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;

namespace P2W.Cards.Infrastructure.Providers.PokemonTcg;

public sealed class PokemonTcgCatalogImportProvider(PokemonTcgApiClient client, IOptions<PokemonTcgOptions> options) : IExternalCatalogProvider
{
    public string SourceName => "PokemonTCG";
    public bool IsEnabled => options.Value.Enabled;

    public async Task<ExternalCatalogImportResult> ImportAsync(CatalogImportContext context, CancellationToken ct)
    {
        var preview = await PreviewAsync(context, ct);
        return new ExternalCatalogImportResult { Products = preview.Products, Sets = preview.Sets, NextCheckpointValue = preview.NextCheckpointValue, HasMore = preview.HasMore };
    }

    public async Task<ExternalCatalogImportPreview> PreviewAsync(CatalogImportContext context, CancellationToken ct)
    {
        if (!context.GameSlug.Equals("pokemon", StringComparison.OrdinalIgnoreCase))
        {
            return new ExternalCatalogImportPreview();
        }

        var cardResult = context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase)
            ? new PokemonTcgPagedResult<PokemonTcgCardDto>()
            : await client.GetCardsAsync(context.MaxRecords, context.CheckpointValue, ct);
        var setResult = context.ImportType.Equals("Cards", StringComparison.OrdinalIgnoreCase)
            ? new PokemonTcgPagedResult<PokemonTcgSetDto>()
            : await client.GetSetsAsync(context.MaxRecords, context.CheckpointValue, ct);

        var products = cardResult.Data.Select(PokemonTcgNormalizer.ToExternalProduct).ToArray();
        var sets = setResult.Data.Select(PokemonTcgNormalizer.ToExternalSet).ToArray();
        var nextCheckpoint = context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase) ? setResult.NextCheckpointValue : cardResult.NextCheckpointValue;
        var hasMore = context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase) ? setResult.HasMore : cardResult.HasMore;

        return new ExternalCatalogImportPreview { Products = products, Sets = sets, NextCheckpointValue = nextCheckpoint, HasMore = hasMore };
    }
}
