using System.Data;
using Microsoft.Data.SqlClient;

namespace P2W.DealFinder.Infrastructure.Import;

public sealed class SqlCatalogBridgeImporter
{
    public async Task<CatalogBridgeImportResult> ImportSetAsync(CatalogBridgeImportRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SourceConnectionString)) throw new ArgumentException("Source connection string is required.");
        if (string.IsNullOrWhiteSpace(request.TargetConnectionString)) throw new ArgumentException("Target connection string is required.");
        if (string.IsNullOrWhiteSpace(request.GameSlug)) throw new ArgumentException("Game slug is required.");
        if (string.IsNullOrWhiteSpace(request.SetNameOrCode)) throw new ArgumentException("Set name or code is required.");

        var products = await LoadProductsAsync(request.SourceConnectionString, request.GameSlug, request.SetNameOrCode, ct);
        var productIds = products.AsEnumerable().Select(r => r.Field<Guid>("SourceCatalogProductId")).ToArray();
        var variants = productIds.Length == 0 ? new DataTable() : await LoadVariantsAsync(request.SourceConnectionString, productIds, ct);
        var identifiers = productIds.Length == 0 ? new DataTable() : await LoadIdentifiersAsync(request.SourceConnectionString, productIds, ct);
        var setName = products.AsEnumerable().Select(r => r.Field<string?>("SetName")).FirstOrDefault() ?? request.SetNameOrCode;
        var setCode = products.AsEnumerable().Select(r => r.Field<string?>("SetCode")).FirstOrDefault();
        var targetDatabase = new SqlConnectionStringBuilder(request.TargetConnectionString).InitialCatalog;

        if (request.DryRun)
        {
            return new CatalogBridgeImportResult(request.GameSlug, setName, setCode, products.Rows.Count, 0, variants.Rows.Count, 0, identifiers.Rows.Count, 0, true, targetDatabase);
        }

        if (request.CreateTargetDatabase)
        {
            await EnsureDatabaseAsync(request.TargetConnectionString, ct);
        }

        await using var target = new SqlConnection(request.TargetConnectionString);
        await target.OpenAsync(ct);
        await EnsureSchemaAsync(target, ct);

        var productsWritten = 0;
        foreach (DataRow row in products.Rows)
        {
            await UpsertProductAsync(target, row, ct);
            await UpsertCoverageAsync(target, row.Field<Guid>("SourceCatalogProductId"), "SourceCatalog", identityResolved: true, metadataComplete: HasCoreMetadata(row), ct);
            productsWritten++;
        }

        var variantsWritten = 0;
        foreach (DataRow row in variants.Rows)
        {
            await UpsertVariantAsync(target, row, ct);
            variantsWritten++;
        }

        var identifiersWritten = 0;
        foreach (DataRow row in identifiers.Rows)
        {
            await UpsertIdentifierAsync(target, row, ct);
            await UpsertCoverageAsync(target, row.Field<Guid>("CatalogProductId"), row.Field<string?>("SourceName") ?? "Unknown", identityResolved: true, metadataComplete: true, ct);
            identifiersWritten++;
        }

        return new CatalogBridgeImportResult(request.GameSlug, setName, setCode, products.Rows.Count, productsWritten, variants.Rows.Count, variantsWritten, identifiers.Rows.Count, identifiersWritten, false, targetDatabase);
    }

    private static async Task<DataTable> LoadProductsAsync(string connectionString, string gameSlug, string setNameOrCode, CancellationToken ct)
    {
        const string sql = """
SELECT
    p.Id AS SourceCatalogProductId,
    p.ProductType,
    COALESCE(pc.Name, CASE WHEN p.IsSingleCard = 1 THEN 'Raw Singles' ELSE 'Unknown' END) AS Category,
    g.Name AS GameOrBrand,
    p.Name,
    p.NormalizedName,
    s.Name AS SetName,
    s.Code AS SetCode,
    p.CardNumber,
    p.Rarity,
    p.Artist,
    p.Description,
    p.ImageUrl,
    COALESCE(p.ReleaseDate, s.ReleaseDate) AS ReleaseDate,
    p.IsSingleCard,
    p.IsSealed,
    p.IsActive,
    p.CreatedUtc,
    p.UpdatedUtc
FROM CatalogProducts AS p
JOIN Games AS g ON g.Id = p.GameId
LEFT JOIN CardSets AS s ON s.Id = p.CardSetId
LEFT JOIN ProductCategories AS pc ON pc.Id = p.ProductCategoryId
WHERE g.Slug = @gameSlug
  AND p.IsActive = 1
  AND p.IsSingleCard = 1
  AND (
      s.Name = @setNameOrCode
      OR s.Code = @setNameOrCode
      OR s.Slug = @setNameOrCode
      OR s.NormalizedName = LOWER(REPLACE(@setNameOrCode, ' ', '-'))
  )
ORDER BY
    TRY_CONVERT(int, REPLACE(REPLACE(p.CardNumber, 'TG', ''), 'GG', '')),
    p.CardNumber,
    p.Name;
""";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@gameSlug", gameSlug);
        command.Parameters.AddWithValue("@setNameOrCode", setNameOrCode);
        return await LoadTableAsync(command, ct);
    }

    private static async Task<DataTable> LoadVariantsAsync(string connectionString, Guid[] productIds, CancellationToken ct)
    {
        const string sql = """
SELECT
    v.Id AS SourceProductVariantId,
    v.CatalogProductId,
    v.VariantName,
    v.Language,
    NULL AS Printing,
    v.IsFoil,
    v.IsReverseHolo,
    v.IsFirstEdition,
    CAST(0 AS bit) AS IsGraded,
    CAST(NULL AS decimal(18,2)) AS Grade,
    v.CreatedUtc,
    COALESCE(p.UpdatedUtc, v.CreatedUtc) AS UpdatedUtc
FROM ProductVariants AS v
JOIN CatalogProducts AS p ON p.Id = v.CatalogProductId
WHERE v.CatalogProductId IN ({0})
ORDER BY v.CatalogProductId, v.VariantName;
""";
        return await LoadByIdsAsync(connectionString, string.Format(sql, BuildParameterList(productIds.Length)), productIds, ct);
    }

    private static async Task<DataTable> LoadIdentifiersAsync(string connectionString, Guid[] productIds, CancellationToken ct)
    {
        const string sql = """
SELECT
    m.Id AS SourceIdentifierId,
    m.CatalogProductId,
    CAST(NULL AS uniqueidentifier) AS ProductVariantId,
    m.SourceName,
    m.ExternalId,
    m.ExternalSlug AS ExternalSku,
    m.ExternalUrl,
    COALESCE(m.ConfidenceScore, 0) AS IdentityConfidence,
    m.MappingStatus,
    m.MappingNotes,
    m.LastVerifiedUtc,
    m.CreatedUtc,
    COALESCE(m.LastVerifiedUtc, m.CreatedUtc) AS UpdatedUtc
FROM ExternalProductMappings AS m
WHERE m.CatalogProductId IN ({0})
ORDER BY m.CatalogProductId, m.SourceName;
""";
        return await LoadByIdsAsync(connectionString, string.Format(sql, BuildParameterList(productIds.Length)), productIds, ct);
    }

    private static async Task<DataTable> LoadByIdsAsync(string connectionString, string sql, Guid[] ids, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        for (var i = 0; i < ids.Length; i++)
        {
            command.Parameters.AddWithValue($"@id{i}", ids[i]);
        }
        return await LoadTableAsync(command, ct);
    }

    private static async Task<DataTable> LoadTableAsync(SqlCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private static string BuildParameterList(int count)
        => string.Join(", ", Enumerable.Range(0, count).Select(i => $"@id{i}"));

    private static async Task EnsureDatabaseAsync(string targetConnectionString, CancellationToken ct)
    {
        var builder = new SqlConnectionStringBuilder(targetConnectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName)) throw new InvalidOperationException("Target connection string needs a database name.");

        builder.InitialCatalog = "master";
        var escapedDatabase = databaseName.Replace("]", "]]");
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@databaseName) IS NULL EXEC('CREATE DATABASE [{escapedDatabase}]');";
        command.Parameters.AddWithValue("@databaseName", databaseName);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
IF OBJECT_ID('dbo.CatalogProducts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogProducts
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_CatalogProducts PRIMARY KEY,
        SourceCatalogProductId uniqueidentifier NOT NULL,
        ProductType nvarchar(80) NOT NULL,
        Category nvarchar(120) NOT NULL,
        GameOrBrand nvarchar(120) NOT NULL,
        Name nvarchar(240) NOT NULL,
        NormalizedName nvarchar(240) NOT NULL,
        SetName nvarchar(180) NULL,
        SetCode nvarchar(80) NULL,
        CardNumber nvarchar(80) NULL,
        Rarity nvarchar(120) NULL,
        Artist nvarchar(160) NULL,
        Description nvarchar(max) NULL,
        ImageUrl nvarchar(1000) NULL,
        ReleaseDate datetime2 NULL,
        IsSingleCard bit NOT NULL,
        IsSealed bit NOT NULL,
        IsActive bit NOT NULL,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_CatalogProducts_SourceCatalogProductId ON dbo.CatalogProducts(SourceCatalogProductId);
    CREATE INDEX IX_CatalogProducts_GameSet ON dbo.CatalogProducts(GameOrBrand, SetName, SetCode);
END;

IF OBJECT_ID('dbo.ProductVariants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductVariants
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ProductVariants PRIMARY KEY,
        CatalogProductId uniqueidentifier NOT NULL,
        SourceProductVariantId uniqueidentifier NOT NULL,
        VariantName nvarchar(120) NOT NULL,
        Language nvarchar(80) NULL,
        Printing nvarchar(80) NULL,
        IsFoil bit NOT NULL,
        IsReverseHolo bit NOT NULL,
        IsFirstEdition bit NOT NULL,
        IsGraded bit NOT NULL,
        Grade decimal(18,2) NULL,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_ProductVariants_SourceProductVariantId ON dbo.ProductVariants(SourceProductVariantId);
    CREATE INDEX IX_ProductVariants_CatalogProductId ON dbo.ProductVariants(CatalogProductId);
END;

IF OBJECT_ID('dbo.ProductIdentifiers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductIdentifiers
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ProductIdentifiers PRIMARY KEY,
        CatalogProductId uniqueidentifier NOT NULL,
        ProductVariantId uniqueidentifier NULL,
        SourceName nvarchar(120) NOT NULL,
        ExternalId nvarchar(240) NOT NULL,
        ExternalSku nvarchar(240) NULL,
        ExternalUrl nvarchar(1000) NULL,
        IdentityConfidence decimal(18,4) NOT NULL,
        MatchStatus int NOT NULL,
        MatchNotes nvarchar(max) NULL,
        LastVerifiedUtc datetime2 NULL,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_ProductIdentifiers_SourceExternal ON dbo.ProductIdentifiers(SourceName, ExternalId);
    CREATE INDEX IX_ProductIdentifiers_CatalogProductId ON dbo.ProductIdentifiers(CatalogProductId);
END;

IF OBJECT_ID('dbo.CatalogProductProviderCoverage', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogProductProviderCoverage
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_CatalogProductProviderCoverage PRIMARY KEY,
        CatalogProductId uniqueidentifier NOT NULL,
        SourceName nvarchar(120) NOT NULL,
        IdentityResolved bit NOT NULL,
        MetadataComplete bit NOT NULL,
        ReferencePricePresent bit NOT NULL,
        ActiveListingsPresent bit NOT NULL,
        SoldCompsPresent bit NOT NULL,
        LastSuccessUtc datetime2 NULL,
        LastAttemptUtc datetime2 NULL,
        LastNoDataReason nvarchar(max) NULL,
        LastError nvarchar(max) NULL,
        CreatedUtc datetime2 NOT NULL,
        UpdatedUtc datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_CatalogProductProviderCoverage_ProductSource ON dbo.CatalogProductProviderCoverage(CatalogProductId, SourceName);
END;
""";
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertProductAsync(SqlConnection connection, DataRow row, CancellationToken ct)
    {
        const string sql = """
IF EXISTS (SELECT 1 FROM dbo.CatalogProducts WHERE SourceCatalogProductId = @SourceCatalogProductId)
BEGIN
    UPDATE dbo.CatalogProducts
    SET ProductType = @ProductType,
        Category = @Category,
        GameOrBrand = @GameOrBrand,
        Name = @Name,
        NormalizedName = @NormalizedName,
        SetName = @SetName,
        SetCode = @SetCode,
        CardNumber = @CardNumber,
        Rarity = @Rarity,
        Artist = @Artist,
        Description = @Description,
        ImageUrl = @ImageUrl,
        ReleaseDate = @ReleaseDate,
        IsSingleCard = @IsSingleCard,
        IsSealed = @IsSealed,
        IsActive = @IsActive,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE SourceCatalogProductId = @SourceCatalogProductId;
END
ELSE
BEGIN
    INSERT dbo.CatalogProducts
    (
        Id, SourceCatalogProductId, ProductType, Category, GameOrBrand, Name, NormalizedName, SetName, SetCode, CardNumber, Rarity, Artist, Description, ImageUrl, ReleaseDate, IsSingleCard, IsSealed, IsActive, CreatedUtc, UpdatedUtc
    )
    VALUES
    (
        @Id, @SourceCatalogProductId, @ProductType, @Category, @GameOrBrand, @Name, @NormalizedName, @SetName, @SetCode, @CardNumber, @Rarity, @Artist, @Description, @ImageUrl, @ReleaseDate, @IsSingleCard, @IsSealed, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
END;
""";
        await using var command = new SqlCommand(sql, connection);
        var productId = row.Field<Guid>("SourceCatalogProductId");
        command.Parameters.AddWithValue("@Id", productId);
        command.Parameters.AddWithValue("@SourceCatalogProductId", productId);
        Add(command, "@ProductType", row["ProductType"]);
        Add(command, "@Category", row["Category"]);
        Add(command, "@GameOrBrand", row["GameOrBrand"]);
        Add(command, "@Name", row["Name"]);
        Add(command, "@NormalizedName", row["NormalizedName"]);
        Add(command, "@SetName", row["SetName"]);
        Add(command, "@SetCode", row["SetCode"]);
        Add(command, "@CardNumber", row["CardNumber"]);
        Add(command, "@Rarity", row["Rarity"]);
        Add(command, "@Artist", row["Artist"]);
        Add(command, "@Description", row["Description"]);
        Add(command, "@ImageUrl", row["ImageUrl"]);
        Add(command, "@ReleaseDate", row["ReleaseDate"]);
        Add(command, "@IsSingleCard", row["IsSingleCard"]);
        Add(command, "@IsSealed", row["IsSealed"]);
        Add(command, "@IsActive", row["IsActive"]);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertVariantAsync(SqlConnection connection, DataRow row, CancellationToken ct)
    {
        const string sql = """
IF EXISTS (SELECT 1 FROM dbo.ProductVariants WHERE SourceProductVariantId = @SourceProductVariantId)
BEGIN
    UPDATE dbo.ProductVariants
    SET VariantName = @VariantName,
        Language = @Language,
        Printing = @Printing,
        IsFoil = @IsFoil,
        IsReverseHolo = @IsReverseHolo,
        IsFirstEdition = @IsFirstEdition,
        IsGraded = @IsGraded,
        Grade = @Grade,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE SourceProductVariantId = @SourceProductVariantId;
END
ELSE
BEGIN
    INSERT dbo.ProductVariants
    (Id, CatalogProductId, SourceProductVariantId, VariantName, Language, Printing, IsFoil, IsReverseHolo, IsFirstEdition, IsGraded, Grade, CreatedUtc, UpdatedUtc)
    VALUES
    (@Id, @CatalogProductId, @SourceProductVariantId, @VariantName, @Language, @Printing, @IsFoil, @IsReverseHolo, @IsFirstEdition, @IsGraded, @Grade, SYSUTCDATETIME(), SYSUTCDATETIME());
END;
""";
        await using var command = new SqlCommand(sql, connection);
        Add(command, "@Id", row["SourceProductVariantId"]);
        Add(command, "@CatalogProductId", row["CatalogProductId"]);
        Add(command, "@SourceProductVariantId", row["SourceProductVariantId"]);
        Add(command, "@VariantName", row["VariantName"]);
        Add(command, "@Language", row["Language"]);
        Add(command, "@Printing", row["Printing"]);
        Add(command, "@IsFoil", row["IsFoil"]);
        Add(command, "@IsReverseHolo", row["IsReverseHolo"]);
        Add(command, "@IsFirstEdition", row["IsFirstEdition"]);
        Add(command, "@IsGraded", row["IsGraded"]);
        Add(command, "@Grade", row["Grade"]);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertIdentifierAsync(SqlConnection connection, DataRow row, CancellationToken ct)
    {
        const string sql = """
IF EXISTS (SELECT 1 FROM dbo.ProductIdentifiers WHERE SourceName = @SourceName AND ExternalId = @ExternalId)
BEGIN
    UPDATE dbo.ProductIdentifiers
    SET CatalogProductId = @CatalogProductId,
        ProductVariantId = @ProductVariantId,
        ExternalSku = @ExternalSku,
        ExternalUrl = @ExternalUrl,
        IdentityConfidence = @IdentityConfidence,
        MatchStatus = @MatchStatus,
        MatchNotes = @MatchNotes,
        LastVerifiedUtc = @LastVerifiedUtc,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE SourceName = @SourceName AND ExternalId = @ExternalId;
END
ELSE
BEGIN
    INSERT dbo.ProductIdentifiers
    (Id, CatalogProductId, ProductVariantId, SourceName, ExternalId, ExternalSku, ExternalUrl, IdentityConfidence, MatchStatus, MatchNotes, LastVerifiedUtc, CreatedUtc, UpdatedUtc)
    VALUES
    (@Id, @CatalogProductId, @ProductVariantId, @SourceName, @ExternalId, @ExternalSku, @ExternalUrl, @IdentityConfidence, @MatchStatus, @MatchNotes, @LastVerifiedUtc, SYSUTCDATETIME(), SYSUTCDATETIME());
END;
""";
        await using var command = new SqlCommand(sql, connection);
        Add(command, "@Id", row["SourceIdentifierId"]);
        Add(command, "@CatalogProductId", row["CatalogProductId"]);
        Add(command, "@ProductVariantId", row["ProductVariantId"]);
        Add(command, "@SourceName", row["SourceName"]);
        Add(command, "@ExternalId", row["ExternalId"]);
        Add(command, "@ExternalSku", row["ExternalSku"]);
        Add(command, "@ExternalUrl", row["ExternalUrl"]);
        Add(command, "@IdentityConfidence", row["IdentityConfidence"]);
        command.Parameters.AddWithValue("@MatchStatus", MapStatus(row.Field<string?>("MappingStatus")));
        Add(command, "@MatchNotes", row["MappingNotes"]);
        Add(command, "@LastVerifiedUtc", row["LastVerifiedUtc"]);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertCoverageAsync(SqlConnection connection, Guid catalogProductId, string sourceName, bool identityResolved, bool metadataComplete, CancellationToken ct)
    {
        const string sql = """
IF EXISTS (SELECT 1 FROM dbo.CatalogProductProviderCoverage WHERE CatalogProductId = @CatalogProductId AND SourceName = @SourceName)
BEGIN
    UPDATE dbo.CatalogProductProviderCoverage
    SET IdentityResolved = @IdentityResolved,
        MetadataComplete = @MetadataComplete,
        LastSuccessUtc = SYSUTCDATETIME(),
        LastAttemptUtc = SYSUTCDATETIME(),
        LastNoDataReason = NULL,
        LastError = NULL,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE CatalogProductId = @CatalogProductId AND SourceName = @SourceName;
END
ELSE
BEGIN
    INSERT dbo.CatalogProductProviderCoverage
    (Id, CatalogProductId, SourceName, IdentityResolved, MetadataComplete, ReferencePricePresent, ActiveListingsPresent, SoldCompsPresent, LastSuccessUtc, LastAttemptUtc, CreatedUtc, UpdatedUtc)
    VALUES
    (NEWID(), @CatalogProductId, @SourceName, @IdentityResolved, @MetadataComplete, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());
END;
""";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CatalogProductId", catalogProductId);
        command.Parameters.AddWithValue("@SourceName", sourceName);
        command.Parameters.AddWithValue("@IdentityResolved", identityResolved);
        command.Parameters.AddWithValue("@MetadataComplete", metadataComplete);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static bool HasCoreMetadata(DataRow row)
        => !IsBlank(row["Name"]) && !IsBlank(row["SetName"]) && !IsBlank(row["CardNumber"]) && !IsBlank(row["ImageUrl"]);

    private static bool IsBlank(object value)
        => value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value));

    private static int MapStatus(string? status)
        => status?.Trim().ToLowerInvariant() switch
        {
            "automatched" => 1,
            "needsreview" => 2,
            "approved" => 3,
            "rejected" => 4,
            _ => 0
        };

    private static void Add(SqlCommand command, string name, object? value)
        => command.Parameters.AddWithValue(name, value == null || value == DBNull.Value ? DBNull.Value : value);
}

