using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace P2W.DealFinder.Api;

public static class AuctionScanProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim RequestGate = new(3, 3);
    private static readonly Dictionary<string, AuctionLookupCache> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public static async Task<AuctionScanPayload> ScanAsync(
        string zyteApiKey,
        PriceChartingScanPayload candidatePayload,
        int requestedMinutes,
        int fallbackMinutes,
        decimal minAuctionPrice,
        decimal maxAuctionPrice,
        int minYearlyVolume,
        int limit,
        CancellationToken ct)
    {
        requestedMinutes = Math.Clamp(requestedMinutes, 1, 24 * 60);
        fallbackMinutes = Math.Clamp(Math.Max(fallbackMinutes, requestedMinutes), requestedMinutes, 24 * 60);
        limit = Math.Clamp(limit, 1, 25);

        var first = await ScanWindowAsync(
            zyteApiKey,
            candidatePayload,
            requestedMinutes,
            requestedMinutes,
            expandedWindow: false,
            minAuctionPrice,
            maxAuctionPrice,
            minYearlyVolume,
            limit,
            ct);

        if (first.Results.Length > 0 || fallbackMinutes == requestedMinutes)
        {
            return first;
        }

        return await ScanWindowAsync(
            zyteApiKey,
            candidatePayload,
            requestedMinutes,
            fallbackMinutes,
            expandedWindow: true,
            minAuctionPrice,
            maxAuctionPrice,
            minYearlyVolume,
            limit,
            ct);
    }

    private static async Task<AuctionScanPayload> ScanWindowAsync(
        string zyteApiKey,
        PriceChartingScanPayload candidatePayload,
        int requestedMinutes,
        int appliedMinutes,
        bool expandedWindow,
        decimal minAuctionPrice,
        decimal maxAuctionPrice,
        int minYearlyVolume,
        int limit,
        CancellationToken ct)
    {
        var rows = new List<AuctionScanResult>();
        var candidatesSearched = 0;
        var totalAuctionStats = AuctionStatsTotals.Empty;
        var totalBuyNowStats = AuctionStatsTotals.Empty;

        foreach (var candidate in candidatePayload.Results)
        {
            if ((candidate.SalesVolume ?? 0) < minYearlyVolume || !LooksEnglishPokemonCandidate(candidate))
            {
                continue;
            }

            candidatesSearched++;
            var auctionLookup = await FindListingsAsync(zyteApiKey, candidate, AuctionListingMode.AuctionEndingSoon, ct);
            totalAuctionStats = totalAuctionStats.Add(auctionLookup.Stats);

            var auctions = auctionLookup.Listings
                .Where(listing => listing.MinutesRemaining is not null
                    && listing.MinutesRemaining <= appliedMinutes
                    && listing.EffectivePrice >= minAuctionPrice
                    && listing.EffectivePrice <= maxAuctionPrice)
                .OrderBy(listing => listing.MinutesRemaining)
                .ThenBy(listing => listing.EffectivePrice)
                .ToArray();

            if (auctions.Length == 0)
            {
                continue;
            }

            var buyNowLookup = await FindListingsAsync(zyteApiKey, candidate, AuctionListingMode.BuyNowLowest, ct);
            totalBuyNowStats = totalBuyNowStats.Add(buyNowLookup.Stats);
            var lowestBuyNow = buyNowLookup.Listings
                .OrderBy(listing => listing.EffectivePrice)
                .ThenByDescending(listing => listing.MatchScore)
                .FirstOrDefault();

            foreach (var auction in auctions)
            {
                rows.Add(new AuctionScanResult(
                    Rank: 0,
                    Product: candidate,
                    Auction: auction,
                    LowestBuyNow: lowestBuyNow,
                    SpreadToMarket: candidate.ExpectedMarketValue is null ? null : Math.Round(candidate.ExpectedMarketValue.Value - auction.EffectivePrice, 2),
                    SpreadToBuyNow: lowestBuyNow is null ? null : Math.Round(lowestBuyNow.EffectivePrice - auction.EffectivePrice, 2),
                    AuctionToMarketPercent: candidate.ExpectedMarketValue is null || candidate.ExpectedMarketValue <= 0
                        ? null
                        : Math.Round(auction.EffectivePrice / candidate.ExpectedMarketValue.Value * 100m, 1),
                    AuctionStats: auctionLookup.Stats,
                    BuyNowStats: buyNowLookup.Stats));

                if (rows.Count >= limit)
                {
                    break;
                }
            }

            if (rows.Count >= limit)
            {
                break;
            }
        }

        var ranked = rows
            .OrderBy(row => row.Auction.MinutesRemaining ?? decimal.MaxValue)
            .ThenBy(row => row.Auction.EffectivePrice)
            .Take(limit)
            .Select((row, index) => row with { Rank = index + 1 })
            .ToArray();

        return new AuctionScanPayload(
            Source: "zyte-ebay-auctions",
            Status: "live",
            ErrorMessage: null,
            RequestedMinutes: requestedMinutes,
            AppliedMinutes: appliedMinutes,
            ExpandedWindow: expandedWindow,
            MinAuctionPrice: minAuctionPrice,
            MaxAuctionPrice: maxAuctionPrice,
            MinYearlyVolume: minYearlyVolume,
            CandidatePool: candidatePayload.Results.Length,
            CandidatesSearched: candidatesSearched,
            ReturnedCount: ranked.Length,
            PriceChartingCapturedAtUtc: candidatePayload.SourceCapturedAtUtc,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            AuctionStats: totalAuctionStats,
            BuyNowStats: totalBuyNowStats,
            Results: ranked);
    }

    private static async Task<AuctionLookupResult> FindListingsAsync(
        string apiKey,
        PriceChartingScanResult candidate,
        AuctionListingMode mode,
        CancellationToken ct)
    {
        var searchUrl = BuildSearchUrl(candidate, mode);
        var cacheKey = $"{mode}:{searchUrl}";
        if (Cache.TryGetValue(cacheKey, out var existing) && DateTimeOffset.UtcNow - existing.CapturedAtUtc < CacheDuration)
        {
            return existing.Result with { Stats = existing.Result.Stats with { UsedCache = true } };
        }

        await RequestGate.WaitAsync(ct);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:"));
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var request = JsonSerializer.Serialize(new ZyteExtractRequest(searchUrl, BrowserHtml: true), JsonOptions);
            using var response = await http.PostAsync(
                "https://api.zyte.com/v1/extract",
                new StringContent(request, Encoding.UTF8, "application/json"),
                ct);

            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                var failed = new AuctionLookupResult(
                    Array.Empty<AuctionListingView>(),
                    EbaySearchStats.Failed(ModeLabel(mode), searchUrl, $"Zyte returned {(int)response.StatusCode}."));
                Cache[cacheKey] = new AuctionLookupCache(DateTimeOffset.UtcNow, failed);
                return failed;
            }

            var html = ExtractHtml(json);
            var parsed = ParseListingSearch(html, candidate, mode, searchUrl);
            Cache[cacheKey] = new AuctionLookupCache(DateTimeOffset.UtcNow, parsed);
            return parsed;
        }
        catch (Exception ex)
        {
            var failed = new AuctionLookupResult(
                Array.Empty<AuctionListingView>(),
                EbaySearchStats.Failed(ModeLabel(mode), searchUrl, ex.Message));
            Cache[cacheKey] = new AuctionLookupCache(DateTimeOffset.UtcNow, failed);
            return failed;
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private static string BuildSearchUrl(PriceChartingScanResult candidate, AuctionListingMode mode)
    {
        var query = $"{candidate.ProductName} PSA 10 Pokemon English";
        var flags = mode == AuctionListingMode.BuyNowLowest ? "LH_BIN=1&_sop=15" : "LH_Auction=1&_sop=1";
        return $"https://www.ebay.com/sch/i.html?_nkw={WebUtility.UrlEncode(query)}&_sacat=0&LH_PrefLoc=2&{flags}";
    }

    private static AuctionLookupResult ParseListingSearch(
        string html,
        PriceChartingScanResult candidate,
        AuctionListingMode mode,
        string searchUrl)
    {
        var listings = new List<AuctionListingView>();
        var stats = new EbaySearchStats(
            ListingType: ModeLabel(mode),
            SearchUrl: searchUrl,
            ListingBlocksSeen: 0,
            ParsedListings: 0,
            MatchedListings: 0,
            RejectedNoTitle: 0,
            RejectedNoPrice: 0,
            RejectedNoUrl: 0,
            RejectedByMatchRules: 0,
            UsedCache: false,
            ErrorMessage: null);

        if (string.IsNullOrWhiteSpace(html))
        {
            return new AuctionLookupResult(listings, stats with { ErrorMessage = "No rendered HTML returned." });
        }

        foreach (var item in ListingBlocks(html))
        {
            stats = stats with { ListingBlocksSeen = stats.ListingBlocksSeen + 1 };

            var title = ExtractText(item, @"<span[^>]+role=""heading""[^>]*>([\s\S]*?)</span>")
                ?? ExtractText(item, @"s-item__title[^>]*>([\s\S]*?)</span>")
                ?? ExtractText(item, @"s-card__title[^>]*>([\s\S]*?)</span>")
                ?? ExtractAttribute(item, @"<img[^>]+alt=""([^"">]+)""")
                ?? ExtractAttribute(item, @"aria-label=""watch ([^"">]+)""");
            title = CleanTitle(title);
            if (string.IsNullOrWhiteSpace(title) || title.Contains("Shop on eBay", StringComparison.OrdinalIgnoreCase))
            {
                stats = stats with { RejectedNoTitle = stats.RejectedNoTitle + 1 };
                continue;
            }

            var price = ParseMoney(ExtractText(item, @"s-item__price[^>]*>([\s\S]*?)</span>")
                ?? ExtractText(item, @"s-card__price[^>]*>([\s\S]*?)</span>"));
            if (price is null)
            {
                stats = stats with { RejectedNoPrice = stats.RejectedNoPrice + 1 };
                continue;
            }

            var url = ExtractAttribute(item, @"<a[^>]+class=""[^"">]*s-item__link[^"">]*""[^>]+href=""([^"">]+)""")
                ?? ExtractAttribute(item, @"<a[^>]+class=""[^"">]*s-card__link[^"">]*""[^>]+href=""([^"">]+)""");
            if (string.IsNullOrWhiteSpace(url))
            {
                stats = stats with { RejectedNoUrl = stats.RejectedNoUrl + 1 };
                continue;
            }

            stats = stats with { ParsedListings = stats.ParsedListings + 1 };
            var match = ScoreMatch(candidate, title);
            if (match.Score < 70)
            {
                stats = stats with { RejectedByMatchRules = stats.RejectedByMatchRules + 1 };
                continue;
            }

            var shippingText = ExtractText(item, @"s-item__shipping[^>]*>([\s\S]*?)</span>");
            var shipping = ParseShipping(shippingText) ?? ParseShippingFromItem(item);
            var timeLeftText = mode == AuctionListingMode.AuctionEndingSoon ? ExtractTimeLeft(item) : null;
            var minutesRemaining = mode == AuctionListingMode.AuctionEndingSoon ? ParseTimeLeftMinutes(timeLeftText) : null;
            var effective = Math.Round(price.Value + (shipping ?? 0m), 2);

            stats = stats with { MatchedListings = stats.MatchedListings + 1 };
            listings.Add(new AuctionListingView(
                ListingId: ExtractListingId(url),
                Title: title,
                Url: CleanEbayUrl(url),
                ImageUrl: ExtractAttribute(item, @"<img[^>]+src=""([^"">]+)"""),
                ListingPrice: price.Value,
                InboundShippingPrice: shipping,
                EffectivePrice: effective,
                ListingType: mode == AuctionListingMode.BuyNowLowest ? "Buy Now" : "Auction",
                BidCount: ParseBidCount(ExtractText(item, @"s-item__bids[^>]*>([\s\S]*?)</span>")
                    ?? ExtractText(item, @"s-card__attribute-row[^>]*>([\s\S]*?bid[\s\S]*?)</div>")),
                TimeLeftText: timeLeftText,
                MinutesRemaining: minutesRemaining,
                MatchScore: match.Score,
                MatchReasons: match.Reasons));
        }

        return new AuctionLookupResult(listings, stats);
    }

    private static IEnumerable<string> ListingBlocks(string html)
    {
        var sCardMatches = Regex.Matches(html, @"<li[^>]+class=""[^"">]*s-card\b[^"">]*""[^>]*data-listingid=""[0-9]+""", RegexOptions.IgnoreCase);
        if (sCardMatches.Count > 0)
        {
            for (var i = 0; i < sCardMatches.Count; i++)
            {
                var start = sCardMatches[i].Index;
                var end = i + 1 < sCardMatches.Count ? sCardMatches[i + 1].Index : html.Length;
                yield return html[start..end];
            }
            yield break;
        }

        foreach (Match itemMatch in Regex.Matches(html, @"<li[^>]+class=""[^"">]*s-item[^"">]*""[\s\S]*?</li>", RegexOptions.IgnoreCase))
        {
            yield return itemMatch.Value;
        }
    }

    private static MatchResult ScoreMatch(PriceChartingScanResult candidate, string title)
    {
        var normalizedTitle = Normalize(title);
        var reasons = new List<string>();
        var score = 0;

        if (Regex.IsMatch(normalizedTitle, @"\bpsa\s*10\b|\bpsa10\b"))
        {
            score += 45;
            reasons.Add("PSA 10");
        }
        else
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        if (Regex.IsMatch(normalizedTitle, @"\b(psa\s*9|psa9|bgs|cgc|sgc|raw|ungraded|proxy|custom|reprint|digital|metal)\b"))
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        if (Regex.IsMatch(normalizedTitle, @"\b(lot|bundle|collection)\b"))
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        if (Regex.IsMatch(normalizedTitle, @"\b(japanese|jpn|jp|korean|chinese|german|french|spanish|italian|thai|indonesian|portuguese)\b"))
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        var cardName = candidate.ProductName.Split('#')[0].Trim();
        var nameTokens = Tokenize(cardName).Where(token => token.Length > 1).Distinct().ToArray();
        var matchedTokens = nameTokens.Count(normalizedTitle.Contains);
        if (nameTokens.Length > 0 && matchedTokens == nameTokens.Length)
        {
            score += 25;
            reasons.Add("name");
        }
        else if (matchedTokens >= Math.Min(2, nameTokens.Length))
        {
            score += 15;
            reasons.Add("partial name");
        }
        else
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        var number = ExtractCardNumber(candidate.ProductName);
        if (!string.IsNullOrWhiteSpace(number) && normalizedTitle.Contains(Normalize(number)))
        {
            score += 15;
            reasons.Add("number");
        }

        if (normalizedTitle.Contains("pokemon"))
        {
            score += 10;
            reasons.Add("pokemon");
        }

        if (normalizedTitle.Contains("english"))
        {
            score += 5;
            reasons.Add("english");
        }

        var setTokens = Tokenize(candidate.ConsoleName.Replace("Pokemon", "", StringComparison.OrdinalIgnoreCase))
            .Where(token => token.Length > 2)
            .Take(3)
            .ToArray();
        if (setTokens.Any(normalizedTitle.Contains))
        {
            score += 5;
            reasons.Add("set hint");
        }

        return new MatchResult(score, reasons.ToArray());
    }

    private static bool LooksEnglishPokemonCandidate(PriceChartingScanResult candidate)
    {
        var value = $"{candidate.ProductName} {candidate.ConsoleName}";
        return !Regex.IsMatch(value, @"\b(japanese|korean|chinese|german|french|spanish|italian|thai|indonesian|portuguese)\b", RegexOptions.IgnoreCase);
    }

    private static string? ExtractTimeLeft(string item)
    {
        var direct = ExtractText(item, @"s-item__time-left[^>]*>([\s\S]*?)</span>")
            ?? ExtractText(item, @"s-card__time-left[^>]*>([\s\S]*?)</span>");
        if (!string.IsNullOrWhiteSpace(direct)) return direct;

        var text = WebUtility.HtmlDecode(StripTags(item)).Replace("\u00a0", " ");
        text = Regex.Replace(text, @"\s+", " ");
        var match = Regex.Match(
            text,
            @"(?:(?:ends\s+in|time\s+left)\s*)?((?:\d+\s*(?:d|day|days|h|hr|hrs|hour|hours|m|min|mins|minute|minutes|s|sec|secs|second|seconds)\s*){1,4})\s*(?:left|remaining)?",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static decimal? ParseTimeLeftMinutes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Contains("ended", StringComparison.OrdinalIgnoreCase)) return null;

        decimal minutes = 0;
        foreach (Match match in Regex.Matches(value, @"(\d+)\s*(d|day|days|h|hr|hrs|hour|hours|m|min|mins|minute|minutes|s|sec|secs|second|seconds)\b", RegexOptions.IgnoreCase))
        {
            var amount = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var unit = match.Groups[2].Value.ToLowerInvariant();
            minutes += unit switch
            {
                "d" or "day" or "days" => amount * 1440m,
                "h" or "hr" or "hrs" or "hour" or "hours" => amount * 60m,
                "m" or "min" or "mins" or "minute" or "minutes" => amount,
                _ => amount / 60m
            };
        }

        return minutes > 0 ? Math.Round(minutes, 1) : null;
    }

    private static string ExtractHtml(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("httpResponseBody", out var body) && body.ValueKind == JsonValueKind.String)
        {
            var bytes = Convert.FromBase64String(body.GetString() ?? "");
            return Encoding.UTF8.GetString(bytes);
        }

        if (root.TryGetProperty("browserHtml", out var html) && html.ValueKind == JsonValueKind.String)
        {
            return html.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? ExtractText(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(StripTags(match.Groups[1].Value)).Trim() : null;
    }

    private static string? ExtractAttribute(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }

    private static string StripTags(string value) => Regex.Replace(value, "<.*?>", string.Empty);

    private static string? CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        var cleaned = WebUtility.HtmlDecode(title).Replace("\u00a0", " ").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+Image\s+\d+\s+of\s+\d+\s*$", string.Empty, RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static decimal? ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = Regex.Match(value, @"\$\s*([0-9,]+(?:\.[0-9]{2})?)");
        return match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseShipping(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Contains("free", StringComparison.OrdinalIgnoreCase)) return 0m;
        return ParseMoney(value);
    }

    private static decimal? ParseShippingFromItem(string item)
    {
        if (Regex.IsMatch(item, @"free\s+(shipping|delivery)", RegexOptions.IgnoreCase)) return 0m;
        var match = Regex.Match(item, @"\+\$\s*([0-9,]+(?:\.[0-9]{2})?)\s*(shipping|delivery)", RegexOptions.IgnoreCase);
        return match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static int? ParseBidCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = Regex.Match(value, @"([0-9,]+)");
        return match.Success && int.TryParse(match.Groups[1].Value.Replace(",", ""), out var parsed) ? parsed : null;
    }

    private static string ExtractListingId(string url)
    {
        var match = Regex.Match(url, @"/itm/(?:[^/?]+/)?([0-9]{9,})", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string CleanEbayUrl(string url)
    {
        var decoded = WebUtility.HtmlDecode(url);
        var queryIndex = decoded.IndexOf('?');
        return queryIndex > 0 ? decoded[..queryIndex] : decoded;
    }

    private static string? ExtractCardNumber(string productName)
    {
        var match = Regex.Match(productName, @"#\s*([A-Za-z0-9/-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string Normalize(string value)
        => Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();

    private static IEnumerable<string> Tokenize(string value)
        => Normalize(value).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ModeLabel(AuctionListingMode mode)
        => mode == AuctionListingMode.BuyNowLowest ? "Buy Now" : "Auction Ending Soon";

    private sealed record ZyteExtractRequest(string Url, bool BrowserHtml);
    private sealed record AuctionLookupCache(DateTimeOffset CapturedAtUtc, AuctionLookupResult Result);
    private sealed record AuctionLookupResult(IReadOnlyList<AuctionListingView> Listings, EbaySearchStats Stats);
    private sealed record MatchResult(int Score, string[] Reasons);
    private enum AuctionListingMode { BuyNowLowest, AuctionEndingSoon }
}

public sealed record AuctionScanPayload(
    string Source,
    string Status,
    string? ErrorMessage,
    int RequestedMinutes,
    int AppliedMinutes,
    bool ExpandedWindow,
    decimal MinAuctionPrice,
    decimal MaxAuctionPrice,
    int MinYearlyVolume,
    int CandidatePool,
    int CandidatesSearched,
    int ReturnedCount,
    DateTimeOffset PriceChartingCapturedAtUtc,
    DateTimeOffset CapturedAtUtc,
    AuctionStatsTotals AuctionStats,
    AuctionStatsTotals BuyNowStats,
    AuctionScanResult[] Results)
{
    public static AuctionScanPayload Blocked(string errorMessage, int requestedMinutes, int fallbackMinutes, decimal minPrice, decimal maxPrice, int minYearlyVolume)
        => new(
            Source: "zyte-ebay-auctions",
            Status: "blocked",
            ErrorMessage: errorMessage,
            RequestedMinutes: requestedMinutes,
            AppliedMinutes: fallbackMinutes,
            ExpandedWindow: false,
            MinAuctionPrice: minPrice,
            MaxAuctionPrice: maxPrice,
            MinYearlyVolume: minYearlyVolume,
            CandidatePool: 0,
            CandidatesSearched: 0,
            ReturnedCount: 0,
            PriceChartingCapturedAtUtc: DateTimeOffset.UtcNow,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            AuctionStats: AuctionStatsTotals.Empty,
            BuyNowStats: AuctionStatsTotals.Empty,
            Results: Array.Empty<AuctionScanResult>());
}

public sealed record AuctionScanResult(
    int Rank,
    PriceChartingScanResult Product,
    AuctionListingView Auction,
    AuctionListingView? LowestBuyNow,
    decimal? SpreadToMarket,
    decimal? SpreadToBuyNow,
    decimal? AuctionToMarketPercent,
    EbaySearchStats AuctionStats,
    EbaySearchStats? BuyNowStats);

public sealed record AuctionListingView(
    string ListingId,
    string Title,
    string Url,
    string? ImageUrl,
    decimal ListingPrice,
    decimal? InboundShippingPrice,
    decimal EffectivePrice,
    string ListingType,
    int? BidCount,
    string? TimeLeftText,
    decimal? MinutesRemaining,
    int MatchScore,
    string[] MatchReasons);

public sealed record AuctionStatsTotals(
    int ListingBlocksSeen,
    int ParsedListings,
    int MatchedListings,
    int RejectedNoTitle,
    int RejectedNoPrice,
    int RejectedNoUrl,
    int RejectedByMatchRules,
    int ErrorCount)
{
    public static AuctionStatsTotals Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    public AuctionStatsTotals Add(EbaySearchStats stats)
        => new(
            ListingBlocksSeen + stats.ListingBlocksSeen,
            ParsedListings + stats.ParsedListings,
            MatchedListings + stats.MatchedListings,
            RejectedNoTitle + stats.RejectedNoTitle,
            RejectedNoPrice + stats.RejectedNoPrice,
            RejectedNoUrl + stats.RejectedNoUrl,
            RejectedByMatchRules + stats.RejectedByMatchRules,
            ErrorCount + (string.IsNullOrWhiteSpace(stats.ErrorMessage) ? 0 : 1));
}
