using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;

namespace P2W.DealFinder.Infrastructure.Import;

public sealed class SqlPriceChartingSnapshotImporter
{
    public async Task<PriceChartingSnapshotImportResult> ImportAsync(PriceChartingSnapshotImportRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) throw new ArgumentException("PriceCharting token is required.");
        if (string.IsNullOrWhiteSpace(request.TargetConnectionString)) throw new ArgumentException("Target connection string is required.");
        if (string.IsNullOrWhiteSpace(request.Category)) throw new ArgumentException("Category is required.");

        var capturedAtUtc = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid();
        var rows = await DownloadRowsAsync(request.Token, request.Category, ct);
        var skipCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var accepted = new List<PriceChartingCsvRow>();

        foreach (var row in rows)
        {
            var skipReason = SkipReason(row, request);
            if (skipReason is not null)
            {
                skipCounts[skipReason] = skipCounts.GetValueOrDefault(skipReason) + 1;
                continue;
            }

            accepted.Add(row);
            if (request.Limit is not null && accepted.Count >= request.Limit.Value)
            {
                break;
            }
        }

        var skippedRows = skipCounts.Values.Sum();
        var targetDatabase = new SqlConnectionStringBuilder(request.TargetConnectionString).InitialCatalog;
        var previewRows = accepted.Take(12).Select(ToPreview).ToArray();

        if (request.DryRun)
        {
            return new PriceChartingSnapshotImportResult(
                runId,
                request.Category,
                rows.Count,
                accepted.Count,
                skippedRows,
                0,
                0,
                true,
                targetDatabase,
                capturedAtUtc,
                skipCounts.Select(pair => new PriceChartingSkippedReason(pair.Key, pair.Value)).OrderByDescending(x => x.Count).ToArray(),
                previewRows);
        }

        if (request.CreateTargetDatabase)
        {
            await EnsureDatabaseAsync(request.TargetConnectionString, ct);
        }

        await using var connection = new SqlConnection(request.TargetConnectionString);
        await connection.OpenAsync(ct);
        await EnsureSchemaAsync(connection, ct);
        await CreateRunAsync(connection, runId, request, rows.Count, accepted.Count, capturedAtUtc, ct);

        var productsWritten = 0;
        var snapshotsWritten = 0;
        foreach (var row in accepted)
        {
            await UpsertProductAsync(connection, row, request.Category, capturedAtUtc, ct);
            productsWritten++;
            await InsertSnapshotAsync(connection, runId, row, request.Category, capturedAtUtc, ct);
            snapshotsWritten++;
        }

        await FinishRunAsync(connection, runId, productsWritten, snapshotsWritten, ct);

        return new PriceChartingSnapshotImportResult(
            runId,
            request.Category,
            rows.Count,
            accepted.Count,
            skippedRows,
            productsWritten,
            snapshotsWritten,
            false,
            targetDatabase,
            capturedAtUtc,
            skipCounts.Select(pair => new PriceChartingSkippedReason(pair.Key, pair.Value)).OrderByDescending(x => x.Count).ToArray(),
            previewRows);
    }

    private static async Task<IReadOnlyList<PriceChartingCsvRow>> DownloadRowsAsync(string token, string category, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        var url = $"https://www.pricecharting.com/price-guide/download-custom?t={WebUtility.UrlEncode(token)}&category={WebUtility.UrlEncode(category)}";
        var csv = await http.GetStringAsync(url, ct);
        return Parse(csv);
    }

    private static string? SkipReason(PriceChartingCsvRow row, PriceChartingSnapshotImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(row.Text("id"))) return "missing PriceCharting id";
        if (request.EnglishOnly && !LooksEnglishPokemonCard(row)) return "non-English or non-Pokemon card signal";
        if (request.RequirePsa10 && row.Price("manual-only-price") is null) return "missing PSA 10 price";
        return null;
    }

    private static bool LooksEnglishPokemonCard(PriceChartingCsvRow row)
    {
        var value = $"{row.Text("product-name")} {row.Text("console-name")} {row.Text("genre")}";
        if (!value.Contains("Pokemon", StringComparison.OrdinalIgnoreCase)) return false;

        var nonEnglishSignals = new[]
        {
            "Japanese", "Korean", "Chinese", "German", "French", "Spanish", "Italian", "Thai", "Indonesian", "Portuguese", "JPN", "JP "
        };

        return !nonEnglishSignals.Any(signal => value.Contains(signal, StringComparison.OrdinalIgnoreCase));
    }

    private static PriceChartingSnapshotPreview ToPreview(PriceChartingCsvRow row)
        => new(
            row.Text("id") ?? "",
            row.Text("product-name") ?? "",
            row.Text("console-name") ?? "",
            row.Price("loose-price"),
            row.Price("graded-price"),
            row.Price("manual-only-price"),
            row.Price("bgs-10-price"),
            row.Price("condition-17-price"),
            row.Price("condition-18-price"),
            row.Int("sales-volume"));

    private static async Task EnsureDatabaseAsync(string targetConnectionString, CancellationToken ct)
    {
        var builder = new SqlConnectionStringBuilder(targetConnectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName)) throw new InvalidOperationException("Target connection string needs a database name.");

        builder.InitialCatalog = "master";
        var escapedDatabase = databaseName.Replace("]", "]]", StringComparison.Ordinal);
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
IF OBJECT_ID('dbo.PriceChartingImportRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceChartingImportRuns
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_PriceChartingImportRuns PRIMARY KEY,
        Category nvarchar(120) NOT NULL,
        Status nvarchar(40) NOT NULL,
        StartedUtc datetime2 NOT NULL,
        FinishedUtc datetime2 NULL,
        TotalRows int NOT NULL,
        AcceptedRows int NOT NULL,
        ProductsWritten int NOT NULL,
        SnapshotsWritten int NOT NULL,
        Notes nvarchar(max) NULL
    );
END;

IF OBJECT_ID('dbo.PriceChartingProducts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceChartingProducts
    (
        ProductId nvarchar(80) NOT NULL CONSTRAINT PK_PriceChartingProducts PRIMARY KEY,
        Category nvarchar(120) NOT NULL,
        ProductName nvarchar(320) NOT NULL,
        ConsoleName nvarchar(220) NULL,
        Genre nvarchar(120) NULL,
        ReleaseDate date NULL,
        TcgId nvarchar(120) NULL,
        Upc nvarchar(120) NULL,
        IsLikelyEnglish bit NOT NULL,
        FirstSeenUtc datetime2 NOT NULL,
        LastSeenUtc datetime2 NOT NULL,
        RawProductJson nvarchar(max) NULL
    );
    CREATE INDEX IX_PriceChartingProducts_CategoryConsole ON dbo.PriceChartingProducts(Category, ConsoleName, ProductName);
    CREATE INDEX IX_PriceChartingProducts_TcgId ON dbo.PriceChartingProducts(TcgId) WHERE TcgId IS NOT NULL;
END;

IF OBJECT_ID('dbo.PriceChartingPriceSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceChartingPriceSnapshots
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_PriceChartingPriceSnapshots PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        ProductId nvarchar(80) NOT NULL,
        Category nvarchar(120) NOT NULL,
        CapturedAtUtc datetime2 NOT NULL,
        Currency nvarchar(12) NOT NULL,
        UngradedPrice decimal(18,2) NULL,
        Grade9Price decimal(18,2) NULL,
        Psa10Price decimal(18,2) NULL,
        Bgs10Price decimal(18,2) NULL,
        Cgc10Price decimal(18,2) NULL,
        Sgc10Price decimal(18,2) NULL,
        NewPrice decimal(18,2) NULL,
        CompleteInBoxPrice decimal(18,2) NULL,
        BoxOnlyPrice decimal(18,2) NULL,
        RetailLooseBuy decimal(18,2) NULL,
        RetailLooseSell decimal(18,2) NULL,
        RetailNewBuy decimal(18,2) NULL,
        RetailNewSell decimal(18,2) NULL,
        RetailCibBuy decimal(18,2) NULL,
        RetailCibSell decimal(18,2) NULL,
        SalesVolume int NULL,
        EstimatedThirtyDayVolume decimal(18,2) NULL,
        EstimatedNinetyDayVolume decimal(18,2) NULL,
        RawRowJson nvarchar(max) NULL
    );
    CREATE INDEX IX_PriceChartingPriceSnapshots_ProductCaptured ON dbo.PriceChartingPriceSnapshots(ProductId, CapturedAtUtc DESC);
    CREATE INDEX IX_PriceChartingPriceSnapshots_Psa10Volume ON dbo.PriceChartingPriceSnapshots(Psa10Price, SalesVolume);
END;
""";
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task CreateRunAsync(SqlConnection connection, Guid runId, PriceChartingSnapshotImportRequest request, int totalRows, int acceptedRows, DateTimeOffset startedUtc, CancellationToken ct)
    {
        const string sql = """
INSERT dbo.PriceChartingImportRuns (Id, Category, Status, StartedUtc, TotalRows, AcceptedRows, ProductsWritten, SnapshotsWritten, Notes)
VALUES (@Id, @Category, 'Started', @StartedUtc, @TotalRows, @AcceptedRows, 0, 0, @Notes);
""";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", runId);
        command.Parameters.AddWithValue("@Category", request.Category);
        command.Parameters.AddWithValue("@StartedUtc", startedUtc.UtcDateTime);
        command.Parameters.AddWithValue("@TotalRows", totalRows);
        command.Parameters.AddWithValue("@AcceptedRows", acceptedRows);
        command.Parameters.AddWithValue("@Notes", $"englishOnly={request.EnglishOnly}; requirePsa10={request.RequirePsa10}; limit={request.Limit?.ToString(CultureInfo.InvariantCulture) ?? "all"}");
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task FinishRunAsync(SqlConnection connection, Guid runId, int productsWritten, int snapshotsWritten, CancellationToken ct)
    {
        const string sql = """
UPDATE dbo.PriceChartingImportRuns
SET Status = 'Completed', FinishedUtc = SYSUTCDATETIME(), ProductsWritten = @ProductsWritten, SnapshotsWritten = @SnapshotsWritten
WHERE Id = @Id;
""";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", runId);
        command.Parameters.AddWithValue("@ProductsWritten", productsWritten);
        command.Parameters.AddWithValue("@SnapshotsWritten", snapshotsWritten);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertProductAsync(SqlConnection connection, PriceChartingCsvRow row, string category, DateTimeOffset capturedAtUtc, CancellationToken ct)
    {
        const string sql = """
IF EXISTS (SELECT 1 FROM dbo.PriceChartingProducts WHERE ProductId = @ProductId)
BEGIN
    UPDATE dbo.PriceChartingProducts
    SET Category = @Category,
        ProductName = @ProductName,
        ConsoleName = @ConsoleName,
        Genre = @Genre,
        ReleaseDate = @ReleaseDate,
        TcgId = @TcgId,
        Upc = @Upc,
        IsLikelyEnglish = @IsLikelyEnglish,
        LastSeenUtc = @LastSeenUtc,
        RawProductJson = @RawProductJson
    WHERE ProductId = @ProductId;
END
ELSE
BEGIN
    INSERT dbo.PriceChartingProducts
    (ProductId, Category, ProductName, ConsoleName, Genre, ReleaseDate, TcgId, Upc, IsLikelyEnglish, FirstSeenUtc, LastSeenUtc, RawProductJson)
    VALUES
    (@ProductId, @Category, @ProductName, @ConsoleName, @Genre, @ReleaseDate, @TcgId, @Upc, @IsLikelyEnglish, @LastSeenUtc, @LastSeenUtc, @RawProductJson);
END;
""";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProductId", row.Text("id") ?? "");
        command.Parameters.AddWithValue("@Category", category);
        command.Parameters.AddWithValue("@ProductName", row.Text("product-name") ?? "");
        Add(command, "@ConsoleName", row.Text("console-name"));
        Add(command, "@Genre", row.Text("genre"));
        Add(command, "@ReleaseDate", row.Date("release-date"));
        Add(command, "@TcgId", row.Text("tcg-id"));
        Add(command, "@Upc", row.Text("upc"));
        command.Parameters.AddWithValue("@IsLikelyEnglish", LooksEnglishPokemonCard(row));
        command.Parameters.AddWithValue("@LastSeenUtc", capturedAtUtc.UtcDateTime);
        command.Parameters.AddWithValue("@RawProductJson", row.ToJson());
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertSnapshotAsync(SqlConnection connection, Guid runId, PriceChartingCsvRow row, string category, DateTimeOffset capturedAtUtc, CancellationToken ct)
    {
        const string sql = """
INSERT dbo.PriceChartingPriceSnapshots
(
    Id, RunId, ProductId, Category, CapturedAtUtc, Currency, UngradedPrice, Grade9Price, Psa10Price, Bgs10Price, Cgc10Price, Sgc10Price,
    NewPrice, CompleteInBoxPrice, BoxOnlyPrice, RetailLooseBuy, RetailLooseSell, RetailNewBuy, RetailNewSell, RetailCibBuy, RetailCibSell,
    SalesVolume, EstimatedThirtyDayVolume, EstimatedNinetyDayVolume, RawRowJson
)
VALUES
(
    NEWID(), @RunId, @ProductId, @Category, @CapturedAtUtc, 'USD', @UngradedPrice, @Grade9Price, @Psa10Price, @Bgs10Price, @Cgc10Price, @Sgc10Price,
    @NewPrice, @CompleteInBoxPrice, @BoxOnlyPrice, @RetailLooseBuy, @RetailLooseSell, @RetailNewBuy, @RetailNewSell, @RetailCibBuy, @RetailCibSell,
    @SalesVolume, @EstimatedThirtyDayVolume, @EstimatedNinetyDayVolume, @RawRowJson
);
""";
        var yearlyVolume = row.Int("sales-volume");
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@ProductId", row.Text("id") ?? "");
        command.Parameters.AddWithValue("@Category", category);
        command.Parameters.AddWithValue("@CapturedAtUtc", capturedAtUtc.UtcDateTime);
        Add(command, "@UngradedPrice", row.Price("loose-price"));
        Add(command, "@Grade9Price", row.Price("graded-price"));
        Add(command, "@Psa10Price", row.Price("manual-only-price"));
        Add(command, "@Bgs10Price", row.Price("bgs-10-price"));
        Add(command, "@Cgc10Price", row.Price("condition-17-price"));
        Add(command, "@Sgc10Price", row.Price("condition-18-price"));
        Add(command, "@NewPrice", row.Price("new-price"));
        Add(command, "@CompleteInBoxPrice", row.Price("cib-price"));
        Add(command, "@BoxOnlyPrice", row.Price("box-only-price"));
        Add(command, "@RetailLooseBuy", row.Price("retail-loose-buy"));
        Add(command, "@RetailLooseSell", row.Price("retail-loose-sell"));
        Add(command, "@RetailNewBuy", row.Price("retail-new-buy"));
        Add(command, "@RetailNewSell", row.Price("retail-new-sell"));
        Add(command, "@RetailCibBuy", row.Price("retail-cib-buy"));
        Add(command, "@RetailCibSell", row.Price("retail-cib-sell"));
        Add(command, "@SalesVolume", yearlyVolume);
        Add(command, "@EstimatedThirtyDayVolume", EstimateVolume(yearlyVolume, 30));
        Add(command, "@EstimatedNinetyDayVolume", EstimateVolume(yearlyVolume, 90));
        command.Parameters.AddWithValue("@RawRowJson", row.ToJson());
        await command.ExecuteNonQueryAsync(ct);
    }

    private static decimal? EstimateVolume(int? yearlyVolume, int days)
        => yearlyVolume is null ? null : Math.Round(yearlyVolume.Value * days / 365m, 2);

    private static IReadOnlyList<PriceChartingCsvRow> Parse(string csv)
    {
        using var reader = new StringReader(csv);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine)) return Array.Empty<PriceChartingCsvRow>();

        var headers = ParseLine(headerLine).Select(header => header.Trim()).ToArray();
        var rows = new List<PriceChartingCsvRow>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = ParseLine(line);
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                fields[headers[i]] = i < values.Count ? values[i] : null;
            }
            rows.Add(new PriceChartingCsvRow(fields));
        }

        return rows;
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }

    private static void Add(SqlCommand command, string name, object? value)
        => command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private sealed record PriceChartingCsvRow(IReadOnlyDictionary<string, string?> Fields)
    {
        public string? Text(string key)
        {
            if (!Fields.TryGetValue(key, out var value)) return null;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public int? Int(string key)
        {
            var value = Text(key);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        public DateTime? Date(string key)
        {
            var value = Text(key);
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed.Date : null;
        }

        public decimal? Price(string key)
        {
            var value = Text(key);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value, NumberStyles.Currency, CultureInfo.GetCultureInfo("en-US"), out var parsed)
                ? parsed
                : null;
        }

        public string ToJson()
        {
            var pairs = Fields
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"\"{Escape(pair.Key)}\":{(pair.Value is null ? "null" : $"\"{Escape(pair.Value)}\"")}");
            return "{" + string.Join(",", pairs) + "}";
        }

        private static string Escape(string value)
            => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}


