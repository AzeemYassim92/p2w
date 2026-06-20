using System.Globalization;
using System.Net;
using System.Text;

namespace P2W.DealFinder.Api;

public static class PriceChartingScanProvider
{
    private static readonly SemaphoreSlim CacheGate = new(1, 1);
    private static readonly Dictionary<string, PriceChartingCsvCache> Caches = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public static async Task<PriceChartingScanPayload> ScanAsync(
        string token,
        string category,
        string grade,
        decimal minPrice,
        decimal maxPrice,
        int limit,
        CancellationToken ct)
    {
        if (maxPrice < minPrice)
        {
            (minPrice, maxPrice) = (maxPrice, minPrice);
        }

        var gradeSpec = PriceChartingGradeSpec.For(grade);
        var cache = await GetCsvRowsAsync(token, category, ct);
        var candidates = cache.Rows
            .Select(row => ToCandidate(row, gradeSpec))
            .Where(candidate => candidate.ExpectedMarketValue is not null
                && candidate.ExpectedMarketValue >= minPrice
                && candidate.ExpectedMarketValue <= maxPrice)
            .OrderByDescending(candidate => candidate.SalesVolume ?? 0)
            .ThenBy(candidate => candidate.ExpectedMarketValue)
            .ToArray();

        var results = candidates
            .Take(limit)
            .Select((candidate, index) => candidate with { Rank = index + 1 })
            .ToArray();

        return new PriceChartingScanPayload(
            Source: "pricecharting",
            Status: "live",
            ErrorMessage: null,
            Category: category,
            Grade: gradeSpec.Label,
            GradeField: gradeSpec.Field,
            MinPrice: minPrice,
            MaxPrice: maxPrice,
            Limit: limit,
            TotalSourceRows: cache.Rows.Count,
            CandidateCount: candidates.Length,
            ReturnedCount: results.Length,
            SourceCapturedAtUtc: cache.CapturedAtUtc,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            EbayStatus: "not-requested",
            EbaySearchedCount: 0,
            Results: results);
    }

    private static async Task<PriceChartingCsvCache> GetCsvRowsAsync(string token, string category, CancellationToken ct)
    {
        await CacheGate.WaitAsync(ct);
        try
        {
            if (Caches.TryGetValue(category, out var existing)
                && DateTimeOffset.UtcNow - existing.CapturedAtUtc < CacheDuration)
            {
                return existing;
            }

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            var url = $"https://www.pricecharting.com/price-guide/download-custom?t={WebUtility.UrlEncode(token)}&category={WebUtility.UrlEncode(category)}";
            var csv = await http.GetStringAsync(url, ct);
            var rows = Parse(csv);
            var cache = new PriceChartingCsvCache(category, DateTimeOffset.UtcNow, rows);
            Caches[category] = cache;
            return cache;
        }
        finally
        {
            CacheGate.Release();
        }
    }

    private static PriceChartingScanResult ToCandidate(PriceChartingCsvRow row, PriceChartingGradeSpec gradeSpec)
    {
        var expectedMarketValue = row.Price(gradeSpec.Field);
        decimal? estimatedFees = expectedMarketValue is null ? null : Math.Round(expectedMarketValue.Value * 0.1325m + 0.30m, 2);
        decimal? maxEffectiveBuy = expectedMarketValue is null || estimatedFees is null
            ? null
            : Math.Round(expectedMarketValue.Value * 0.90m - estimatedFees.Value - 5.00m - 1.00m - 2.00m, 2);
        var raw = row.Price("loose-price");
        decimal? psa10ToRawMultiple = expectedMarketValue is not null && raw is not null && raw > 0
            ? Math.Round(expectedMarketValue.Value / raw.Value, 2)
            : null;
        var yearlyVolume = row.Int("sales-volume");
        var estimatedThirtyDayVolume = EstimatePeriodVolume(yearlyVolume, 30);
        var estimatedNinetyDayVolume = EstimatePeriodVolume(yearlyVolume, 90);

        return new PriceChartingScanResult(
            Rank: 0,
            ProductId: row.Text("id") ?? "",
            ProductName: row.Text("product-name") ?? "Unknown",
            ConsoleName: row.Text("console-name") ?? "Unknown",
            Genre: row.Text("genre"),
            ReleaseDate: row.Text("release-date"),
            TcgId: row.Text("tcg-id"),
            UngradedPrice: raw,
            Grade9Price: row.Price("graded-price"),
            Psa10Price: row.Price("manual-only-price"),
            Bgs10Price: row.Price("bgs-10-price"),
            Cgc10Price: row.Price("condition-17-price"),
            Sgc10Price: row.Price("condition-18-price"),
            SalesVolume: yearlyVolume,
            EstimatedThirtyDayVolume: estimatedThirtyDayVolume,
            EstimatedNinetyDayVolume: estimatedNinetyDayVolume,
            ExpectedMarketValue: expectedMarketValue,
            EstimatedFeesAtMarket: estimatedFees,
            MaxEffectiveBuyForTenPercentMargin: maxEffectiveBuy,
            Psa10ToRawMultiple: psa10ToRawMultiple,
            SearchUrl: $"https://www.pricecharting.com/search-products?q={WebUtility.UrlEncode(row.Text("product-name") ?? "")}&type=prices",
            LowestBuyNow: null,
            LowestAuction: null,
            BuyNowStats: null,
            AuctionStats: null);
    }


    private static decimal? EstimatePeriodVolume(int? yearlyVolume, int days)
        => yearlyVolume is null ? null : Math.Round(yearlyVolume.Value * days / 365m, 1);
    private static IReadOnlyList<PriceChartingCsvRow> Parse(string csv)
    {
        using var reader = new StringReader(csv);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return Array.Empty<PriceChartingCsvRow>();
        }

        var headers = ParseLine(headerLine)
            .Select(header => header.Trim())
            .ToArray();

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

    private sealed record PriceChartingCsvCache(string Category, DateTimeOffset CapturedAtUtc, IReadOnlyList<PriceChartingCsvRow> Rows);

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

        public decimal? Price(string key)
        {
            var value = Text(key);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value, NumberStyles.Currency, CultureInfo.GetCultureInfo("en-US"), out var parsed)
                ? parsed
                : null;
        }
    }

    private sealed record PriceChartingGradeSpec(string Grade, string Label, string Field)
    {
        public static PriceChartingGradeSpec For(string grade)
        {
            var normalized = grade.Replace(" ", "", StringComparison.OrdinalIgnoreCase).Replace("-", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            return normalized switch
            {
                "ungraded" or "raw" => new(grade, "Ungraded", "loose-price"),
                "grade9" or "graded9" or "psa9" => new(grade, "Grade 9", "graded-price"),
                "bgs10" => new(grade, "BGS 10", "bgs-10-price"),
                "cgc10" => new(grade, "CGC 10", "condition-17-price"),
                "sgc10" => new(grade, "SGC 10", "condition-18-price"),
                _ => new(grade, "PSA 10", "manual-only-price")
            };
        }
    }
}

public sealed record PriceChartingScanPayload(
    string Source,
    string Status,
    string? ErrorMessage,
    string Category,
    string Grade,
    string GradeField,
    decimal MinPrice,
    decimal MaxPrice,
    int Limit,
    int TotalSourceRows,
    int CandidateCount,
    int ReturnedCount,
    DateTimeOffset SourceCapturedAtUtc,
    DateTimeOffset CapturedAtUtc,
    string? EbayStatus,
    int EbaySearchedCount,
    PriceChartingScanResult[] Results)
{
    public static PriceChartingScanPayload Blocked(string category, string grade, decimal minPrice, decimal maxPrice, int limit, string errorMessage)
        => new(
            Source: "pricecharting",
            Status: "blocked",
            ErrorMessage: errorMessage,
            Category: category,
            Grade: grade,
            GradeField: "",
            MinPrice: minPrice,
            MaxPrice: maxPrice,
            Limit: limit,
            TotalSourceRows: 0,
            CandidateCount: 0,
            ReturnedCount: 0,
            SourceCapturedAtUtc: DateTimeOffset.UtcNow,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            EbayStatus: null,
            EbaySearchedCount: 0,
            Results: Array.Empty<PriceChartingScanResult>());
}

public sealed record PriceChartingScanResult(
    int Rank,
    string ProductId,
    string ProductName,
    string ConsoleName,
    string? Genre,
    string? ReleaseDate,
    string? TcgId,
    decimal? UngradedPrice,
    decimal? Grade9Price,
    decimal? Psa10Price,
    decimal? Bgs10Price,
    decimal? Cgc10Price,
    decimal? Sgc10Price,
    int? SalesVolume,
    decimal? EstimatedThirtyDayVolume,
    decimal? EstimatedNinetyDayVolume,
    decimal? ExpectedMarketValue,
    decimal? EstimatedFeesAtMarket,
    decimal? MaxEffectiveBuyForTenPercentMargin,
    decimal? Psa10ToRawMultiple,
    string SearchUrl,
    EbayListingView? LowestBuyNow,
    EbayListingView? LowestAuction,
    EbaySearchStats? BuyNowStats,
    EbaySearchStats? AuctionStats);








