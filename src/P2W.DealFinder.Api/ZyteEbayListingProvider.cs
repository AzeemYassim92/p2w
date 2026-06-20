using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace P2W.DealFinder.Api;

public static class ZyteEbayListingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim RequestGate = new(3, 3);
    private static readonly Dictionary<string, EbayLookupCache> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(12);

    public static async Task<PriceChartingScanPayload> EnrichAsync(
        string apiKey,
        PriceChartingScanPayload payload,
        bool includeBuyNow,
        bool includeAuction,
        CancellationToken ct)
    {
        var searched = 0;
        var enriched = new List<PriceChartingScanResult>();

        foreach (var candidate in payload.Results)
        {
            EbayLookupResult? buyNow = null;
            EbayLookupResult? auction = null;

            if (includeBuyNow)
            {
                buyNow = await FindLowestAsync(apiKey, candidate, EbayListingMode.BuyNow, ct);
                searched++;
            }

            if (includeAuction)
            {
                auction = await FindLowestAsync(apiKey, candidate, EbayListingMode.Auction, ct);
                searched++;
            }

            enriched.Add(candidate with
            {
                LowestBuyNow = buyNow?.Listing,
                LowestAuction = auction?.Listing,
                BuyNowStats = buyNow?.Stats,
                AuctionStats = auction?.Stats
            });
        }

        return payload with
        {
            EbayStatus = "live",
            EbaySearchedCount = searched,
            Results = enriched.ToArray()
        };
    }


    public static async Task<EbayProbeResult> ProbeAsync(
        string apiKey,
        PriceChartingScanResult candidate,
        string listingType,
        int take,
        CancellationToken ct)
    {
        var mode = listingType.Equals("auction", StringComparison.OrdinalIgnoreCase)
            ? EbayListingMode.Auction
            : EbayListingMode.BuyNow;
        var searchUrl = BuildSearchUrl(candidate, mode);
        var pageSize = 240;

        await RequestGate.WaitAsync(ct);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
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
                var failed = EbaySearchStats.Failed(mode.ToString(), searchUrl, $"Zyte returned {(int)response.StatusCode}.");
                return new EbayProbeResult(candidate, failed.ListingType, searchUrl, pageSize, DateTimeOffset.UtcNow, failed, Array.Empty<EbayListingView>());
            }

            var parsed = ParseListingSearch(ExtractHtml(json), candidate, mode, searchUrl);
            var listings = parsed.Listings
                .OrderBy(item => item.EffectivePrice)
                .ThenByDescending(item => item.MatchScore)
                .Take(Math.Clamp(take, 1, 100))
                .ToArray();

            return new EbayProbeResult(candidate, parsed.Stats.ListingType, searchUrl, pageSize, DateTimeOffset.UtcNow, parsed.Stats, listings);
        }
        catch (Exception ex)
        {
            var failed = EbaySearchStats.Failed(mode.ToString(), searchUrl, ex.Message);
            return new EbayProbeResult(candidate, failed.ListingType, searchUrl, pageSize, DateTimeOffset.UtcNow, failed, Array.Empty<EbayListingView>());
        }
        finally
        {
            RequestGate.Release();
        }
    }
    private static async Task<EbayLookupResult> FindLowestAsync(
        string apiKey,
        PriceChartingScanResult candidate,
        EbayListingMode mode,
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
                var failed = new EbayLookupResult(null, EbaySearchStats.Failed(mode.ToString(), searchUrl, $"Zyte returned {(int)response.StatusCode}."));
                Cache[cacheKey] = new EbayLookupCache(DateTimeOffset.UtcNow, failed);
                return failed;
            }

            var html = ExtractHtml(json);
            var parsed = ParseListingSearch(html, candidate, mode, searchUrl);
            var listing = parsed.Listings
                .OrderBy(item => item.EffectivePrice)
                .ThenByDescending(item => item.MatchScore)
                .FirstOrDefault();

            var result = new EbayLookupResult(listing, parsed.Stats);
            Cache[cacheKey] = new EbayLookupCache(DateTimeOffset.UtcNow, result);
            return result;
        }
        catch (Exception ex)
        {
            var failed = new EbayLookupResult(null, EbaySearchStats.Failed(mode.ToString(), searchUrl, ex.Message));
            Cache[cacheKey] = new EbayLookupCache(DateTimeOffset.UtcNow, failed);
            return failed;
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private static string BuildSearchUrl(PriceChartingScanResult candidate, EbayListingMode mode)
    {
        var cardName = candidate.ProductName.Split('#')[0].Trim();
        var number = ExtractCardNumber(candidate.ProductName);
        var setName = candidate.ConsoleName.Replace("Pokemon", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        var queryParts = new[]
            {
                cardName,
                string.IsNullOrWhiteSpace(number) ? null : $"#{number}",
                setName,
                "Pokemon",
                "PSA 10"
            }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        var query = string.Join(" ", queryParts);
        var flags = mode == EbayListingMode.BuyNow ? "LH_BIN=1" : "LH_Auction=1";
        return $"https://www.ebay.com/sch/i.html?_nkw={WebUtility.UrlEncode(query)}&_sacat=0&LH_PrefLoc=2&{flags}&_sop=15&_ipg=240";
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

    private static EbayParseResult ParseListingSearch(string html, PriceChartingScanResult candidate, EbayListingMode mode, string searchUrl)
    {
        var listings = new List<EbayListingView>();
        var stats = new EbaySearchStats(
            ListingType: mode == EbayListingMode.BuyNow ? "Buy Now" : "Auction",
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
            return new EbayParseResult(listings, stats with { ErrorMessage = "No rendered HTML returned." });
        }

        foreach (var item in ListingBlocks(html))
        {
            stats = stats with { ListingBlocksSeen = stats.ListingBlocksSeen + 1 };
            var title = ExtractText(item, @"<span[^>]+role=\""heading\""[^>]*>([\s\S]*?)</span>")
                ?? ExtractText(item, @"s-item__title[^>]*>([\s\S]*?)</span>")
                ?? ExtractText(item, @"s-card__title[^>]*>([\s\S]*?)</span>")
                ?? ExtractAttribute(item, @"<img[^>]+alt=\""([^\"">]+)\""")
                ?? ExtractAttribute(item, @"aria-label=\""watch ([^\"">]+)\""");
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

            var shippingText = ExtractText(item, @"s-item__shipping[^>]*>([\s\S]*?)</span>");
            var shipping = ParseShipping(shippingText) ?? ParseShippingFromItem(item);
            var effective = Math.Round(price.Value + (shipping ?? 0m), 2);
            var url = ExtractAttribute(item, @"<a[^>]+class=\""[^\"">]*s-item__link[^\"">]*\""[^>]+href=\""([^\"">]+)\""")
                ?? ExtractAttribute(item, @"<a[^>]+class=\""[^\"">]*s-card__link[^\"">]*\""[^>]+href=\""([^\"">]+)\""");
            if (string.IsNullOrWhiteSpace(url))
            {
                stats = stats with { RejectedNoUrl = stats.RejectedNoUrl + 1 };
                continue;
            }

            stats = stats with { ParsedListings = stats.ParsedListings + 1 };
            var match = ScoreMatch(candidate, title, mode);
            if (match.Score < 70)
            {
                stats = stats with { RejectedByMatchRules = stats.RejectedByMatchRules + 1 };
                continue;
            }

            stats = stats with { MatchedListings = stats.MatchedListings + 1 };
            listings.Add(new EbayListingView(
                ListingId: ExtractListingId(url),
                Title: title,
                Url: CleanEbayUrl(url),
                ImageUrl: ExtractAttribute(item, @"<img[^>]+src=\""([^\"">]+)\"""),
                ListingPrice: price.Value,
                InboundShippingPrice: shipping,
                EffectivePrice: effective,
                ListingType: mode == EbayListingMode.BuyNow ? "Buy Now" : "Auction",
                BidCount: ParseBidCount(ExtractText(item, @"s-item__bids[^>]*>([\s\S]*?)</span>")
                    ?? ExtractText(item, @"s-card__attribute-row[^>]*>([\s\S]*?bid[\s\S]*?)</div>")),
                MatchScore: match.Score,
                MatchReasons: match.Reasons));
        }

        return new EbayParseResult(listings, stats);
    }

    private static IEnumerable<string> ListingBlocks(string html)
    {
        var sCardMatches = Regex.Matches(html, @"<li[^>]+class=\""[^\"">]*s-card\b[^\"">]*\""[^>]*data-listingid=\""[0-9]+\""", RegexOptions.IgnoreCase);
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

        foreach (Match itemMatch in Regex.Matches(html, @"<li[^>]+class=\""[^\"">]*s-item[^\"">]*\""[\s\S]*?</li>", RegexOptions.IgnoreCase))
        {
            yield return itemMatch.Value;
        }
    }

    private static string? CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        var cleaned = WebUtility.HtmlDecode(title).Replace("\u00a0", " ").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+Image\s+\d+\s+of\s+\d+\s*$", string.Empty, RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static decimal? ParseShippingFromItem(string item)
    {
        if (Regex.IsMatch(item, @"free\s+(shipping|delivery)", RegexOptions.IgnoreCase)) return 0m;
        var match = Regex.Match(item, @"\+\$\s*([0-9,]+(?:\.[0-9]{2})?)\s*(shipping|delivery)", RegexOptions.IgnoreCase);
        return match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static MatchResult ScoreMatch(PriceChartingScanResult candidate, string title, EbayListingMode mode)
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

        if (Regex.IsMatch(normalizedTitle, @"\b(japanese|japan|jpn|korean|chinese|german|french|spanish|italian|thai|indonesian|portuguese)\b"))
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        if (Regex.IsMatch(normalizedTitle, @"\b(lot|bundle|collection)\b"))
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
        if (!string.IsNullOrWhiteSpace(number))
        {
            if (!ContainsNumberToken(normalizedTitle, number))
            {
                return new MatchResult(0, Array.Empty<string>());
            }

            score += 15;
            reasons.Add("number");
        }

        if (normalizedTitle.Contains("pokemon"))
        {
            score += 10;
            reasons.Add("pokemon");
        }

        var setTokens = Tokenize(candidate.ConsoleName.Replace("Pokemon", "", StringComparison.OrdinalIgnoreCase))
            .Where(token => token.Length > 2 && !int.TryParse(token, out _))
            .Distinct()
            .Take(4)
            .ToArray();
        var matchedSetTokens = setTokens.Count(normalizedTitle.Contains);
        if (setTokens.Length > 0 && matchedSetTokens == 0)
        {
            return new MatchResult(0, Array.Empty<string>());
        }

        if (matchedSetTokens > 0)
        {
            score += Math.Min(10, matchedSetTokens * 5);
            reasons.Add("set hint");
        }

        return new MatchResult(score, reasons.ToArray());
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

    private static bool ContainsNumberToken(string normalizedTitle, string number)
    {
        var primaryNumber = Normalize(number)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(primaryNumber)) return false;

        return normalizedTitle
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => token.Equals(primaryNumber, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
        => Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();

    private static IEnumerable<string> Tokenize(string value)
        => Normalize(value).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed record ZyteExtractRequest(string Url, bool BrowserHtml);
    private sealed record EbayLookupCache(DateTimeOffset CapturedAtUtc, EbayLookupResult Result);
    private sealed record EbayLookupResult(EbayListingView? Listing, EbaySearchStats Stats);
    private sealed record EbayParseResult(IReadOnlyList<EbayListingView> Listings, EbaySearchStats Stats);
    private sealed record MatchResult(int Score, string[] Reasons);
    private enum EbayListingMode { BuyNow, Auction }
}

public sealed record EbayProbeResult(
    PriceChartingScanResult Candidate,
    string ListingType,
    string SearchUrl,
    int RequestedPageSize,
    DateTimeOffset CapturedAtUtc,
    EbaySearchStats Stats,
    EbayListingView[] Listings);
public sealed record EbayListingView(
    string ListingId,
    string Title,
    string Url,
    string? ImageUrl,
    decimal ListingPrice,
    decimal? InboundShippingPrice,
    decimal EffectivePrice,
    string ListingType,
    int? BidCount,
    int MatchScore,
    string[] MatchReasons);

public sealed record EbaySearchStats(
    string ListingType,
    string SearchUrl,
    int ListingBlocksSeen,
    int ParsedListings,
    int MatchedListings,
    int RejectedNoTitle,
    int RejectedNoPrice,
    int RejectedNoUrl,
    int RejectedByMatchRules,
    bool UsedCache,
    string? ErrorMessage)
{
    public static EbaySearchStats Failed(string listingType, string searchUrl, string errorMessage)
        => new(
            ListingType: listingType,
            SearchUrl: searchUrl,
            ListingBlocksSeen: 0,
            ParsedListings: 0,
            MatchedListings: 0,
            RejectedNoTitle: 0,
            RejectedNoPrice: 0,
            RejectedNoUrl: 0,
            RejectedByMatchRules: 0,
            UsedCache: false,
            ErrorMessage: errorMessage);
}

