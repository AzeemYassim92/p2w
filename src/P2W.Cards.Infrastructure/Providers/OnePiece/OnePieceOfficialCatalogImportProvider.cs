using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Infrastructure.Providers.OnePiece;

public sealed partial class OnePieceOfficialCatalogImportProvider(
    HttpClient http,
    IOptions<OnePieceOfficialOptions> options,
    LocalSessionLog sessionLog) : IExternalCatalogProvider
{
    private const string Source = "OnePieceOfficial";
    private const string Game = "one-piece";

    public string SourceName => Source;
    public bool IsEnabled => options.Value.Enabled;

    public async Task<ExternalCatalogImportResult> ImportAsync(CatalogImportContext context, CancellationToken ct)
    {
        var preview = await PreviewAsync(context, ct);
        return new ExternalCatalogImportResult
        {
            Products = preview.Products,
            Sets = preview.Sets,
            NextCheckpointValue = preview.NextCheckpointValue,
            HasMore = preview.HasMore
        };
    }

    public async Task<ExternalCatalogImportPreview> PreviewAsync(CatalogImportContext context, CancellationToken ct)
    {
        if (!context.GameSlug.Equals(Game, StringComparison.OrdinalIgnoreCase))
        {
            return new ExternalCatalogImportPreview();
        }

        var setOptions = await GetSetOptionsAsync(ct);
        var checkpoint = ParseCheckpoint(context.CheckpointValue);

        if (context.ImportType.Equals("Sets", StringComparison.OrdinalIgnoreCase))
        {
            var sets = setOptions
                .Skip(checkpoint)
                .Take(context.MaxRecords)
                .Select(s => ToExternalSet(s))
                .ToArray();
            var next = checkpoint + sets.Length;
            return new ExternalCatalogImportPreview
            {
                Sets = sets,
                NextCheckpointValue = next < setOptions.Count ? next.ToString() : null,
                HasMore = next < setOptions.Count
            };
        }

        var products = new List<ExternalCatalogProductDto>();
        var touchedSets = new List<ExternalCatalogSetDto>();
        var nextCheckpoint = checkpoint;

        for (var i = checkpoint; i < setOptions.Count; i++)
        {
            var set = setOptions[i];
            var cards = await GetCardsForSetAsync(set, ct);
            if (products.Count > 0 && products.Count + cards.Count > context.MaxRecords)
            {
                nextCheckpoint = i;
                break;
            }

            products.AddRange(cards);
            touchedSets.Add(ToExternalSet(set));
            nextCheckpoint = i + 1;

            if (products.Count >= context.MaxRecords)
            {
                break;
            }

            await DelayAsync(ct);
        }

        return new ExternalCatalogImportPreview
        {
            Products = products.Take(context.MaxRecords).ToArray(),
            Sets = touchedSets.DistinctBy(s => s.ExternalId).ToArray(),
            NextCheckpointValue = nextCheckpoint < setOptions.Count ? nextCheckpoint.ToString() : null,
            HasMore = nextCheckpoint < setOptions.Count
        };
    }

    public async Task<IReadOnlyList<OnePieceOfficialSetReport>> GetOfficialSetReportsAsync(CancellationToken ct)
    {
        var setOptions = await GetSetOptionsAsync(ct);
        var reports = new List<OnePieceOfficialSetReport>();
        for (var i = 0; i < setOptions.Count; i++)
        {
            var set = setOptions[i];
            var cards = await GetCardRowsForSetAsync(set, ct);
            reports.Add(new OnePieceOfficialSetReport(
                set.ExternalId,
                set.Name,
                set.Code,
                cards.Count,
                cards.DistinctBy(card => $"{card.SetCode}:{card.CardNumber}:{card.Name}", StringComparer.OrdinalIgnoreCase).Count()));
            await DelayAsync(ct);
        }

        return reports;
    }

    private async Task<IReadOnlyList<OnePieceOfficialSetOption>> GetSetOptionsAsync(CancellationToken ct)
    {
        var html = await ReadPageAsync(BaseUrl, ct);
        var options = OptionRegex().Matches(html)
            .Select(match => ParseSetOption(match.Groups["id"].Value, Clean(match.Groups["label"].Value)))
            .Where(option => option != null)
            .Select(option => option!)
            .DistinctBy(option => option.ExternalId)
            .ToArray();

        sessionLog.Info("catalog.onepiece", "onepiece.set-options", "One Piece official set selector parsed.", new { Count = options.Length });
        return options;
    }

    private async Task<IReadOnlyList<ExternalCatalogProductDto>> GetCardsForSetAsync(OnePieceOfficialSetOption set, CancellationToken ct)
    {
        var rows = await GetCardRowsForSetAsync(set, ct);
        var cards = rows
            .DistinctBy(card => $"{card.SetCode}:{card.CardNumber}:{card.Name}", StringComparer.OrdinalIgnoreCase)
            .ToArray();

        sessionLog.Info("catalog.onepiece", "onepiece.cards.canonicalized", "One Piece official set cards canonicalized.", new
        {
            set.ExternalId,
            set.Name,
            set.Code,
            OfficialRows = rows.Count,
            CanonicalCount = cards.Length
        });
        return cards;
    }

    private async Task<IReadOnlyList<ExternalCatalogProductDto>> GetCardRowsForSetAsync(OnePieceOfficialSetOption set, CancellationToken ct)
    {
        var html = await ReadPageAsync($"{BaseUrl}?series={Uri.EscapeDataString(set.ExternalId)}", ct);
        var cards = CardBlockRegex().Matches(html)
            .Select(match => ParseCard(set, match.Groups["id"].Value, match.Groups["body"].Value))
            .Where(card => card != null)
            .Select(card => card!)
            .ToArray();

        sessionLog.Info("catalog.onepiece", "onepiece.cards", "One Piece official set cards parsed.", new
        {
            set.ExternalId,
            set.Name,
            set.Code,
            Count = cards.Length
        });
        return cards;
    }

    private ExternalCatalogProductDto? ParseCard(OnePieceOfficialSetOption fallbackSet, string externalId, string body)
    {
        var spans = SpanRegex().Matches(body)
            .Select(match => Clean(match.Groups["value"].Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (spans.Length < 3)
        {
            return null;
        }

        var parsedSet = ParseCardSet(body);
        var cardSet = parsedSet == null
            ? fallbackSet
            : parsedSet with { ExternalId = string.IsNullOrWhiteSpace(parsedSet.ExternalId) ? fallbackSet.ExternalId : parsedSet.ExternalId };
        var name = FirstMatch(body, CardNameRegex());
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var image = FirstMatch(body, ImageRegex());
        var cardType = spans[2];
        var rarity = spans[1];
        var effect = FirstMatch(body, EffectRegex());
        var feature = FirstMatch(body, FeatureRegex());
        var variant = externalId.Contains("_p", StringComparison.OrdinalIgnoreCase) ? "parallel art" : "normal";

        return new ExternalCatalogProductDto
        {
            SourceName = Source,
            ExternalId = externalId,
            Name = name,
            GameSlug = Game,
            SetName = cardSet.Name,
            SetCode = cardSet.Code,
            CardNumber = spans[0],
            Rarity = rarity,
            Description = BuildDescription(cardType, feature, effect),
            ImageUrl = AbsoluteImageUrl(image),
            ExternalUrl = $"{BaseUrl}?series={Uri.EscapeDataString(cardSet.ExternalId)}#{externalId}",
            VariantNames = new[] { variant },
            RawSourceJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                source = Source,
                externalId,
                cardNumber = spans[0],
                rarity,
                cardType,
                set = cardSet.Name,
                setCode = cardSet.Code
            })
        };
    }

    private OnePieceOfficialSetOption? ParseCardSet(string body)
    {
        var raw = FirstMatch(body, CardSetRegex());
        return string.IsNullOrWhiteSpace(raw) ? null : ParseSetOption("", raw);
    }

    private static OnePieceOfficialSetOption? ParseSetOption(string externalId, string label)
    {
        var code = CodeRegex().Match(label).Groups["code"].Value;
        if (string.IsNullOrWhiteSpace(code))
        {
            if (label.Contains("Promotion card", StringComparison.OrdinalIgnoreCase))
            {
                return new OnePieceOfficialSetOption(externalId, "Promotion Card", "PROMO");
            }

            if (label.Contains("Other Product Card", StringComparison.OrdinalIgnoreCase))
            {
                return new OnePieceOfficialSetOption(externalId, "Other Product Card", "OTHER");
            }

            return null;
        }

        var nameMatch = NameRegex().Match(label);
        var name = nameMatch.Success ? nameMatch.Groups["name"].Value : label.Replace($"[{code}]", "", StringComparison.OrdinalIgnoreCase);
        name = name.Trim(' ', '-', '\r', '\n', '\t');
        return new OnePieceOfficialSetOption(externalId, NormalizeTitle(name), code.Replace("-", "", StringComparison.OrdinalIgnoreCase));
    }

    private static ExternalCatalogSetDto ToExternalSet(OnePieceOfficialSetOption set) => new()
    {
        SourceName = Source,
        ExternalId = set.ExternalId,
        Name = set.Name,
        GameSlug = Game,
        Code = set.Code
    };

    private async Task<string> ReadPageAsync(string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task DelayAsync(CancellationToken ct)
    {
        var delay = Math.Clamp(options.Value.RequestDelayMilliseconds, 0, 5000);
        if (delay > 0)
        {
            await Task.Delay(delay, ct);
        }
    }

    private string BaseUrl
        => string.IsNullOrWhiteSpace(options.Value.BaseUrl)
            ? "https://en.onepiece-cardgame.com/cardlist/"
            : EnsureTrailingSlash(options.Value.BaseUrl);

    private static int ParseCheckpoint(string? checkpointValue)
        => int.TryParse(checkpointValue, out var value) && value > 0 ? value : 0;

    private static string FirstMatch(string html, Regex regex)
    {
        var match = regex.Match(html);
        return match.Success ? Clean(match.Groups["value"].Value) : "";
    }

    private static string Clean(string value)
    {
        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = TagRegex().Replace(decoded, " ");
        return WhitespaceRegex().Replace(withoutTags, " ").Trim();
    }

    private static string NormalizeTitle(string value)
        => string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part =>
        {
            if (part.Any(char.IsDigit))
            {
                return part.ToUpperInvariant();
            }

            var lower = part.ToLowerInvariant();
            return lower.Length == 0 ? lower : char.ToUpperInvariant(lower[0]) + lower[1..];
        }));

    private static string EnsureTrailingSlash(string value)
    {
        var trimmed = value.Trim();
        return trimmed.EndsWith("/", StringComparison.Ordinal) ? trimmed : trimmed + "/";
    }

    private static string BuildDescription(string cardType, string feature, string effect)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(cardType)) parts.Add($"Card type: {cardType}");
        if (!string.IsNullOrWhiteSpace(feature)) parts.Add($"Type: {feature}");
        if (!string.IsNullOrWhiteSpace(effect)) parts.Add(effect);
        return string.Join(Environment.NewLine, parts);
    }

    private static string? AbsoluteImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = value.Split('?', 2)[0].Replace("../", "/", StringComparison.Ordinal);
        return clean.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? clean
            : $"https://en.onepiece-cardgame.com{clean}";
    }

    [GeneratedRegex("<option[^>]+value=\"(?<id>\\d+)\"[^>]*>(?<label>.*?)</option>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex OptionRegex();

    [GeneratedRegex("<dl class=\"modalCol\" id=\"(?<id>[^\"]+)\"[^>]*>(?<body>.*?)</dl>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CardBlockRegex();

    [GeneratedRegex("<span>(?<value>.*?)</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SpanRegex();

    [GeneratedRegex("<div class=\"cardName\">(?<value>.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CardNameRegex();

    [GeneratedRegex("<img[^>]+data-src=\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ImageRegex();

    [GeneratedRegex("<div class=\"text\"><h3>Effect</h3>(?<value>.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex EffectRegex();

    [GeneratedRegex("<div class=\"feature\"><h3>Type</h3>(?<value>.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FeatureRegex();

    [GeneratedRegex("<div class=\"getInfo\"><h3>Card Set\\(s\\)</h3>(?<value>.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CardSetRegex();

    [GeneratedRegex("\\[(?<code>[A-Z0-9-]+)\\]")]
    private static partial Regex CodeRegex();

    [GeneratedRegex("-(?<name>[^\\[]+)-\\s*\\[[A-Z0-9-]+\\]", RegexOptions.IgnoreCase)]
    private static partial Regex NameRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed record OnePieceOfficialSetReport(string ExternalId, string Name, string Code, int OfficialRowCount, int CanonicalCardCount);

internal sealed record OnePieceOfficialSetOption(string ExternalId, string Name, string Code);
