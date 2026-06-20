namespace P2W.DealFinder.Infrastructure.Import;

public sealed record CatalogBridgeImportRequest(
    string SourceConnectionString,
    string TargetConnectionString,
    string GameSlug,
    string SetNameOrCode,
    bool DryRun,
    bool CreateTargetDatabase = true);

public sealed record CatalogBridgeImportResult(
    string GameSlug,
    string SetName,
    string? SetCode,
    int ProductsFound,
    int ProductsWritten,
    int VariantsFound,
    int VariantsWritten,
    int IdentifiersFound,
    int IdentifiersWritten,
    bool DryRun,
    string TargetDatabase);
