namespace P2W.DealFinder.Infrastructure.Import;

public sealed record PriceChartingSnapshotImportRequest(
    string Token,
    string TargetConnectionString,
    string Category,
    bool DryRun,
    int? Limit,
    bool EnglishOnly,
    bool RequirePsa10,
    bool CreateTargetDatabase = true);

public sealed record PriceChartingSnapshotImportResult(
    Guid RunId,
    string Category,
    int TotalRows,
    int AcceptedRows,
    int SkippedRows,
    int ProductsWritten,
    int SnapshotsWritten,
    bool DryRun,
    string TargetDatabase,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<PriceChartingSkippedReason> SkipReasons,
    IReadOnlyList<PriceChartingSnapshotPreview> PreviewRows);

public sealed record PriceChartingSkippedReason(string Reason, int Count);

public sealed record PriceChartingSnapshotPreview(
    string ProductId,
    string ProductName,
    string ConsoleName,
    decimal? UngradedPrice,
    decimal? Grade9Price,
    decimal? Psa10Price,
    decimal? Bgs10Price,
    decimal? Cgc10Price,
    decimal? Sgc10Price,
    int? SalesVolume);
