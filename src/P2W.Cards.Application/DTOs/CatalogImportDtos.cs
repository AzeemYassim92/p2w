namespace P2W.Cards.Application.DTOs;

public sealed class StartCatalogImportRequest
{
    public string SourceName { get; set; } = "";
    public string GameSlug { get; set; } = "";
    public string ImportType { get; set; } = "";
    public bool DryRun { get; set; }
    public int MaxRecords { get; set; } = 250;
    public bool IncludeImages { get; set; } = true;
    public bool UpdateExistingProducts { get; set; } = true;
    public bool CreateMissingProducts { get; set; } = true;
    public bool UseCheckpoint { get; set; } = true;
    public bool SaveCheckpoint { get; set; } = true;
    public string? CheckpointValue { get; set; }
}

public sealed class CatalogImportPreviewDto
{
    public string SourceName { get; set; } = "";
    public string GameSlug { get; set; } = "";
    public string ImportType { get; set; } = "";
    public int ExternalRecordsRead { get; set; }
    public int ExistingMatches { get; set; }
    public int WouldCreate { get; set; }
    public int WouldUpdate { get; set; }
    public int WouldSkip { get; set; }
    public string? CheckpointValue { get; set; }
    public string? NextCheckpointValue { get; set; }
    public bool HasMore { get; set; }
    public IReadOnlyList<CatalogImportPreviewRowDto> SampleRows { get; set; } = Array.Empty<CatalogImportPreviewRowDto>();
}

public sealed class CatalogImportPreviewRowDto
{
    public string ExternalId { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? SetName { get; set; }
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string Action { get; set; } = "";
    public decimal ConfidenceScore { get; set; }
    public Guid? MatchedCatalogProductId { get; set; }
    public string? MatchedCatalogProductName { get; set; }
}

public class CatalogImportRunDto
{
    public Guid CatalogImportRunId { get; set; }
    public string SourceName { get; set; } = "";
    public string ImportType { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int ErrorCount { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public string? CheckpointValue { get; set; }
    public string? NextCheckpointValue { get; set; }
    public bool HasMore { get; set; }
}

public sealed class CatalogImportRunDetailDto : CatalogImportRunDto
{
    public IReadOnlyList<CatalogImportErrorDto> Errors { get; set; } = Array.Empty<CatalogImportErrorDto>();
}

public sealed class CatalogImportErrorDto
{
    public Guid CatalogImportErrorId { get; set; }
    public string SourceName { get; set; } = "";
    public string? ExternalId { get; set; }
    public string ErrorMessage { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}

public sealed class CatalogImportContext
{
    public string SourceName { get; set; } = "";
    public string GameSlug { get; set; } = "";
    public string ImportType { get; set; } = "";
    public int MaxRecords { get; set; }
    public bool IncludeImages { get; set; }
    public bool UpdateExistingProducts { get; set; }
    public bool CreateMissingProducts { get; set; }
    public bool UseCheckpoint { get; set; }
    public bool SaveCheckpoint { get; set; }
    public string? CheckpointValue { get; set; }
}

public class ExternalCatalogImportResult
{
    public IReadOnlyList<ExternalCatalogProductDto> Products { get; set; } = Array.Empty<ExternalCatalogProductDto>();
    public IReadOnlyList<ExternalCatalogSetDto> Sets { get; set; } = Array.Empty<ExternalCatalogSetDto>();
    public string? NextCheckpointValue { get; set; }
    public bool HasMore { get; set; }
}

public sealed class ExternalCatalogImportPreview : ExternalCatalogImportResult;

public sealed class ExternalCatalogProductDto
{
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string GameSlug { get; set; } = "";
    public string? SetName { get; set; }
    public string? SetCode { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? CardNumber { get; set; }
    public string? Rarity { get; set; }
    public string? Artist { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? ExternalUrl { get; set; }
    public IReadOnlyList<string> VariantNames { get; set; } = Array.Empty<string>();
    public string? RawSourceJson { get; set; }
}

public sealed class ExternalCatalogSetDto
{
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string GameSlug { get; set; } = "";
    public string? Code { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? LogoUrl { get; set; }
    public string? SymbolUrl { get; set; }
}

public sealed class CatalogProductMatchResult
{
    public Guid? CatalogProductId { get; set; }
    public string? CatalogProductName { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string MatchReason { get; set; } = "";
}

public sealed class CatalogImportCheckpointDto
{
    public string SourceName { get; set; } = "";
    public string ImportType { get; set; } = "";
    public string CheckpointValue { get; set; } = "";
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CatalogMetadataBackfillRequest
{
    public string GameSlug { get; set; } = "pokemon";
    public Guid? CardSetId { get; set; }
    public int Take { get; set; } = 50;
    public bool MissingOnly { get; set; } = true;
    public bool DryRun { get; set; } = true;
}

public sealed class CatalogMetadataBackfillResultDto
{
    public string GameSlug { get; set; } = "";
    public int ProductsScanned { get; set; }
    public int ProductsUpdated { get; set; }
    public int ProductsSkipped { get; set; }
    public int MissingMappings { get; set; }
    public int ProviderMisses { get; set; }
    public int Errors { get; set; }
    public IReadOnlyList<string> Notes { get; set; } = Array.Empty<string>();
}

public sealed class CatalogCompletenessDto
{
    public string GameSlug { get; set; } = "";
    public int SetCount { get; set; }
    public int ProductCount { get; set; }
    public int ProductsMissingImage { get; set; }
    public int ProductsMissingDescription { get; set; }
    public int ProductsMissingRarity { get; set; }
    public int ProductsMissingCardNumber { get; set; }
    public int ProductsWithoutExternalMapping { get; set; }
    public int SetsWithoutProducts { get; set; }
    public IReadOnlyList<CatalogSetCompletenessRowDto> RecentSets { get; set; } = Array.Empty<CatalogSetCompletenessRowDto>();
}

public sealed class CatalogSetCompletenessRowDto
{
    public Guid CardSetId { get; set; }
    public string SetName { get; set; } = "";
    public string? SetCode { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int ProductCount { get; set; }
    public int ProductsMissingImage { get; set; }
    public int ProductsMissingDescription { get; set; }
    public int ProductsWithoutExternalMapping { get; set; }
}

public sealed class MappingReviewDto
{
    public Guid MappingId { get; set; }
    public Guid CatalogProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? SetName { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? ExternalUrl { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string MappingStatus { get; set; } = "";
    public string? MappingNotes { get; set; }
}

public sealed class UpdateMappingNotesRequest
{
    public string? Notes { get; set; }
}

public sealed class CatalogPriceReferenceSnapshotDto
{
    public Guid CatalogPriceReferenceSnapshotId { get; set; }
    public Guid CatalogProductId { get; set; }
    public string SourceName { get; set; } = "";
    public decimal? MarketPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? MidPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CapturedAtUtc { get; set; }
}
