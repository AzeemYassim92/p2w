using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;

namespace P2W.Cards.Infrastructure.Providers.Scryfall;

public sealed class ScryfallCatalogImportProvider(ScryfallApiClient client, IOptions<ScryfallOptions> options) : IExternalCatalogProvider
{
    public string SourceName => "Scryfall";
    public bool IsEnabled => options.Value.Enabled;

    public async Task<ExternalCatalogImportResult> ImportAsync(CatalogImportContext context, CancellationToken ct)
    {
        var preview = await PreviewAsync(context, ct);
        return new ExternalCatalogImportResult { Products = preview.Products, Sets = preview.Sets, NextCheckpointValue = preview.NextCheckpointValue, HasMore = preview.HasMore };
    }

    public async Task<ExternalCatalogImportPreview> PreviewAsync(CatalogImportContext context, CancellationToken ct)
    {
        if (!context.GameSlug.Equals("magic-the-gathering", StringComparison.OrdinalIgnoreCase))
        {
            return new ExternalCatalogImportPreview();
        }

        var cardResult = context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase)
            ? new ScryfallPagedResult<ScryfallCardDto>()
            : await client.GetCardsAsync(context.MaxRecords, context.CheckpointValue, ct);
        var setResult = context.ImportType.Equals("Cards", StringComparison.OrdinalIgnoreCase)
            ? new ScryfallPagedResult<ScryfallSetDto>()
            : await client.GetSetsAsync(context.MaxRecords, context.CheckpointValue, ct);

        var products = cardResult.Data.Select(ScryfallNormalizer.ToExternalProduct).ToArray();
        var sets = setResult.Data.Select(ScryfallNormalizer.ToExternalSet).ToArray();
        var nextCheckpoint = context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase) ? setResult.NextCheckpointValue : cardResult.NextCheckpointValue;
        var hasMore = context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase) ? setResult.HasMore : cardResult.HasMore;

        return new ExternalCatalogImportPreview { Products = products, Sets = sets, NextCheckpointValue = nextCheckpoint, HasMore = hasMore };
    }
}
