using System.Text.Json;
using Microsoft.Data.SqlClient;
using P2W.DealFinder.Infrastructure.Providers.JustTcg;

public static class JustTcgWorkerCommands
{
    public static bool Handles(string command)
        => command is "justtcg-games" or "justtcg-sets" or "justtcg-cards" or "justtcg-range";

    public static async Task<int> RunAsync(string command, string[] args)
        => command switch
        {
            "justtcg-games" => await Games(args),
            "justtcg-sets" => await Sets(args),
            "justtcg-cards" => await Cards(args),
            "justtcg-range" => await Range(args),
            _ => 1
        };

    public static void PrintHelp()
    {
        Console.WriteLine("  deal-finder justtcg-games");
        Console.WriteLine("  deal-finder justtcg-sets --game pokemon");
        Console.WriteLine("  deal-finder justtcg-cards --game pokemon --name Charizard --price-history 180d --limit 3");
        Console.WriteLine("  deal-finder justtcg-range --game pokemon --set \"Chaos Rising\" --min 15 --max 25 --price-history 180d --take 40");
    }

    private static async Task<int> Games(string[] args)
    {
        var client = CreateClient(args);
        var games = await client.GetGamesAsync(CancellationToken.None);
        Console.WriteLine($"JustTCG games: {games.Count}");
        foreach (var game in games)
        {
            Console.WriteLine($"{game.Name ?? game.DisplayName ?? game.Id} | id={game.Id ?? "-"} | slug={game.Slug ?? "-"}");
        }

        return 0;
    }

    private static async Task<int> Sets(string[] args)
    {
        var client = CreateClient(args);
        var game = ReadOption(args, "--game");
        var limit = ReadIntOption(args, "--limit", 50);
        var offset = ReadIntOption(args, "--offset", 0);
        var sets = await client.GetSetsAsync(game, limit, offset, CancellationToken.None);
        Console.WriteLine($"JustTCG sets: {sets.Count} (game={game ?? "all"}, limit={limit}, offset={offset})");
        foreach (var set in sets)
        {
            Console.WriteLine($"{set.Name ?? set.Id} | game={set.Game ?? "-"} | code={set.Code ?? "-"} | cards={set.CardCount?.ToString() ?? "-"} | released={set.ReleaseDate ?? set.ReleaseDateSnake ?? "-"}");
        }

        return 0;
    }

    private static async Task<int> Cards(string[] args)
    {
        var search = ReadSearch(args);
        var client = CreateClient(args);

        if (HasFlag(args, "--raw"))
        {
            using var raw = await client.GetRawCardsAsync(search, CancellationToken.None);
            Console.WriteLine(JsonSerializer.Serialize(raw.RootElement, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        var cards = await client.SearchCardsAsync(search, CancellationToken.None);
        Console.WriteLine($"JustTCG cards: {cards.Count}");
        foreach (var card in cards)
        {
            PrintCard(card);
        }

        return 0;
    }

    private static async Task<int> Range(string[] args)
    {
        var target = ResolveTargetConnection(args);
        if (string.IsNullOrWhiteSpace(target))
        {
            Console.Error.WriteLine("No target connection found. Pass --target-connection or set DEALFINDER_TARGET_CONNECTION.");
            return 1;
        }

        var game = ReadOption(args, "--game") ?? "pokemon";
        var set = ReadOption(args, "--set") ?? "Chaos Rising";
        var min = ReadDecimalOption(args, "--min", 15m);
        var max = ReadDecimalOption(args, "--max", 25m);
        var take = ReadIntOption(args, "--take", 40);
        var priceHistory = ReadOption(args, "--price-history") ?? "180d";
        var client = CreateClient(args);
        var products = await ReadCatalogProductsForSet(target, game, set, take);

        Console.WriteLine($"JustTCG catalog range probe: game={game}, set={set}, range={min:C}-{max:C}, products={products.Count}, priceHistory={priceHistory}");
        var matches = 0;
        foreach (var product in products)
        {
            try
            {
                var cards = await client.SearchCardsAsync(new JustTcgCardSearch(
                    Game: ToJustTcgGameName(product.GameOrBrand),
                    Set: product.SetName,
                    Name: product.Name,
                    Number: product.CardNumber,
                    Condition: "Near Mint",
                    PriceHistoryDuration: priceHistory,
                    Limit: 5), CancellationToken.None);

                var priced = cards
                    .SelectMany(card => card.Variants.Select(variant => new { card, variant }))
                    .Where(row => row.variant.BestKnownPrice is not null
                        && row.variant.BestKnownPrice >= min
                        && row.variant.BestKnownPrice <= max)
                    .OrderBy(row => row.variant.BestKnownPrice)
                    .ToArray();

                foreach (var row in priced)
                {
                    matches++;
                    Console.WriteLine($"{matches}. {row.card.Name} | {row.card.SetName ?? row.card.Set} | #{row.card.Number} | {row.card.Rarity ?? product.Rarity ?? "-"} | {row.variant.Condition ?? "-"} / {row.variant.Printing ?? "-"} | {row.variant.BestKnownPrice:C} | history={row.variant.PriceHistory.Count}");
                    PrintPotentialGradedFields(row.variant);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Probe skipped {product.Name} #{product.CardNumber}: {ex.Message}");
            }
        }

        Console.WriteLine($"Range matches: {matches}");
        return matches == 0 ? 2 : 0;
    }

    private static JustTcgCardSearch ReadSearch(string[] args)
        => new(
            Game: ReadOption(args, "--game"),
            Set: ReadOption(args, "--set"),
            Name: ReadOption(args, "--name"),
            Number: ReadOption(args, "--number"),
            TcgPlayerId: ReadOption(args, "--tcgplayer-id"),
            TcgPlayerSkuId: ReadOption(args, "--tcgplayer-sku-id"),
            MinPrice: ReadDecimalNullableOption(args, "--min-price"),
            IncludeNullPrices: ReadBoolNullableOption(args, "--include-null-prices"),
            UpdatedAfter: ReadOption(args, "--updated-after"),
            OrderBy: ReadOption(args, "--order-by"),
            Order: ReadOption(args, "--order"),
            IncludePriceHistory: ReadBoolNullableOption(args, "--include-price-history"),
            IncludeStatistics: ReadBoolNullableOption(args, "--include-statistics"),
            Condition: ReadOption(args, "--condition"),
            Printing: ReadOption(args, "--printing"),
            PriceHistoryDuration: ReadOption(args, "--price-history") ?? "180d",
            Limit: ReadIntOption(args, "--limit", 20),
            Offset: ReadIntOption(args, "--offset", 0));

    private static JustTcgApiClient CreateClient(string[] args)
    {
        var legacy = ReadLegacyOptions();
        var options = new JustTcgOptions
        {
            BaseUrl = ReadOption(args, "--justtcg-base-url")
                ?? Environment.GetEnvironmentVariable("JUSTTCG_BASE_URL")
                ?? legacy.BaseUrl,
            ApiKey = ReadOption(args, "--justtcg-key")
                ?? Environment.GetEnvironmentVariable("JUSTTCG_API_KEY")
                ?? Environment.GetEnvironmentVariable("DEALFINDER_JUSTTCG_API_KEY")
                ?? legacy.ApiKey
        };

        return new JustTcgApiClient(new HttpClient { Timeout = TimeSpan.FromSeconds(45) }, options);
    }

    private static JustTcgOptions ReadLegacyOptions()
    {
        var options = new JustTcgOptions();
        var candidates = new[]
        {
            @"C:\Repos\2026\ecom\ecompt2\src\P2W.Cards.Api\appsettings.Local.json",
            @"C:\Repos\2026\ecom\ecompt2\src\P2W.Cards.Api\appsettings.json"
        };

        foreach (var path in candidates.Where(File.Exists))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Providers", out var providers)
                || !providers.TryGetProperty("JustTcg", out var justTcg))
            {
                continue;
            }

            var baseUrl = justTcg.TryGetProperty("BaseUrl", out var baseUrlElement) && baseUrlElement.ValueKind == JsonValueKind.String
                ? baseUrlElement.GetString()
                : options.BaseUrl;
            var apiKey = justTcg.TryGetProperty("ApiKey", out var apiKeyElement) && apiKeyElement.ValueKind == JsonValueKind.String
                ? apiKeyElement.GetString()
                : options.ApiKey;

            options = new JustTcgOptions
            {
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? options.BaseUrl : baseUrl!,
                ApiKey = string.IsNullOrWhiteSpace(apiKey) ? options.ApiKey : apiKey
            };

            if (!string.IsNullOrWhiteSpace(options.ApiKey)) break;
        }

        return options;
    }

    private static string? ResolveTargetConnection(string[] args)
    {
        var explicitTarget = ReadOption(args, "--target-connection") ?? Environment.GetEnvironmentVariable("DEALFINDER_TARGET_CONNECTION");
        if (!string.IsNullOrWhiteSpace(explicitTarget)) return explicitTarget;

        var source = ReadOption(args, "--source-connection")
            ?? Environment.GetEnvironmentVariable("DEALFINDER_SOURCE_CONNECTION")
            ?? ReadLegacyConnectionString();

        return string.IsNullOrWhiteSpace(source) ? null : DeriveTargetConnection(source, "P2WDealFinderDb");
    }

    private static string? ReadLegacyConnectionString()
    {
        var candidates = new[]
        {
            @"C:\Repos\2026\ecom\ecompt2\src\P2W.Cards.Api\appsettings.Local.json",
            @"C:\Repos\2026\ecom\ecompt2\src\P2W.Cards.Api\appsettings.json"
        };

        foreach (var path in candidates.Where(File.Exists))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection)
                && defaultConnection.ValueKind == JsonValueKind.String)
            {
                var value = defaultConnection.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }

        return null;
    }

    private static string DeriveTargetConnection(string sourceConnection, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(sourceConnection)
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }

    private static async Task<IReadOnlyList<CatalogProbeProduct>> ReadCatalogProductsForSet(string targetConnection, string game, string set, int take)
    {
        var products = new List<CatalogProbeProduct>();
        await using var connection = new SqlConnection(targetConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@take)
    Id,
    Name,
    GameOrBrand,
    SetName,
    SetCode,
    CardNumber,
    Rarity
FROM CatalogProducts
WHERE IsActive = 1
  AND (LOWER(GameOrBrand) = @game OR LOWER(REPLACE(GameOrBrand, ' ', '-')) = @game)
  AND (LOWER(SetName) = @set OR LOWER(SetCode) = @set)
ORDER BY
    TRY_CONVERT(int, NULLIF(CardNumber, '')),
    CardNumber,
    Name;";
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 500));
        command.Parameters.AddWithValue("@game", game.ToLowerInvariant());
        command.Parameters.AddWithValue("@set", set.ToLowerInvariant());
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(new CatalogProbeProduct(
                reader.GetGuid(reader.GetOrdinal("Id")),
                reader["Name"]?.ToString() ?? "",
                reader["GameOrBrand"]?.ToString(),
                reader["SetName"]?.ToString(),
                reader["SetCode"]?.ToString(),
                reader["CardNumber"]?.ToString(),
                reader["Rarity"]?.ToString()));
        }

        return products;
    }

    private static void PrintCard(JustTcgCardDto card)
    {
        Console.WriteLine();
        Console.WriteLine($"{card.Name ?? card.Id} | game={card.Game ?? "-"} | set={card.SetName ?? card.Set ?? "-"} | number={card.Number ?? "-"} | rarity={card.Rarity ?? "-"} | tcgplayerId={card.TcgPlayerId ?? "-"}");
        Console.WriteLine($"Variants: {card.Variants.Count}");
        foreach (var variant in card.Variants.Take(12))
        {
            Console.WriteLine($"  - {variant.Condition ?? "-"} / {variant.Printing ?? "-"} / {variant.Language ?? "-"} | price={variant.BestKnownPrice?.ToString("C") ?? "-"} | 24h={variant.PriceChange24h?.ToString() ?? "-"} | 7d={variant.PriceChange7d?.ToString() ?? "-"} | 30d={variant.PriceChange30d?.ToString() ?? "-"} | 90d={variant.PriceChange90d?.ToString() ?? "-"} | history={variant.PriceHistory.Count}");
            PrintPotentialGradedFields(variant);
        }
    }

    private static void PrintPotentialGradedFields(JustTcgVariantDto variant)
    {
        var gradedKeys = variant.ExtensionData.Keys
            .Where(key => key.Contains("psa", StringComparison.OrdinalIgnoreCase)
                || key.Contains("grade", StringComparison.OrdinalIgnoreCase)
                || key.Contains("graded", StringComparison.OrdinalIgnoreCase)
                || key.Contains("ungraded", StringComparison.OrdinalIgnoreCase))
            .OrderBy(key => key)
            .ToArray();

        if (gradedKeys.Length > 0)
        {
            Console.WriteLine($"    graded-like fields: {string.Join(", ", gradedKeys)}");
        }
    }

    private static string? ToJustTcgGameName(string? game)
        => game?.Contains("pokemon", StringComparison.OrdinalIgnoreCase) == true ? "pokemon"
            : game?.Contains("magic", StringComparison.OrdinalIgnoreCase) == true ? "magic"
            : game?.Contains("one", StringComparison.OrdinalIgnoreCase) == true ? "one-piece"
            : game;

    private static string? ReadOption(string[] args, string name)
    {
        var index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static bool HasFlag(string[] args, string name)
        => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    private static int ReadIntOption(string[] args, string name, int defaultValue)
        => int.TryParse(ReadOption(args, name), out var value) ? value : defaultValue;

    private static decimal ReadDecimalOption(string[] args, string name, decimal defaultValue)
        => decimal.TryParse(ReadOption(args, name), out var value) ? value : defaultValue;

    private static decimal? ReadDecimalNullableOption(string[] args, string name)
        => decimal.TryParse(ReadOption(args, name), out var value) ? value : null;

    private static bool? ReadBoolNullableOption(string[] args, string name)
        => bool.TryParse(ReadOption(args, name), out var value) ? value : null;

    private sealed record CatalogProbeProduct(
        Guid Id,
        string Name,
        string? GameOrBrand,
        string? SetName,
        string? SetCode,
        string? CardNumber,
        string? Rarity);
}
