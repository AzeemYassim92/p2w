using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Providers.OnePiece;
using P2W.Cards.Infrastructure.Providers.PokemonTcg;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Worker.Aggregation;

public sealed class CatalogSyncJobRunner(IServiceScopeFactory scopeFactory)
{
    public async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var command = args.FirstOrDefault() ?? "";
        var game = ResolveGame(command, args);
        var batchSize = ReadInt(args, "--batch-size", 5000);
        var fromStart = !args.Contains("--resume", StringComparer.OrdinalIgnoreCase);
        var validateOnly = command.Equals("validate-catalog", StringComparison.OrdinalIgnoreCase)
            || args.Contains("--validate-only", StringComparer.OrdinalIgnoreCase);

        using var scope = scopeFactory.CreateScope();
        var sessionLog = scope.ServiceProvider.GetRequiredService<LocalSessionLog>();
        sessionLog.StartSession();
        sessionLog.Info("catalog.job", "catalog.job.start", "Catalog sync job started.", new { command, game, batchSize, fromStart, validateOnly });

        if (!validateOnly)
        {
            await SyncGameAsync(scope.ServiceProvider, game, batchSize, fromStart, ct);
        }

        await ValidateGameAsync(scope.ServiceProvider, game, ct);
        sessionLog.Info("catalog.job", "catalog.job.complete", "Catalog sync job completed.", new { command, game });
        return 0;
    }

    public static bool IsCatalogCommand(string? command)
        => command != null
            && (command.Equals("sync-pokemon-catalog", StringComparison.OrdinalIgnoreCase)
                || command.Equals("sync-onepiece-catalog", StringComparison.OrdinalIgnoreCase)
                || command.Equals("catalog-sync", StringComparison.OrdinalIgnoreCase)
                || command.Equals("validate-catalog", StringComparison.OrdinalIgnoreCase));

    private static string ResolveGame(string command, string[] args)
    {
        if (command.Equals("sync-pokemon-catalog", StringComparison.OrdinalIgnoreCase)) return "pokemon";
        if (command.Equals("sync-onepiece-catalog", StringComparison.OrdinalIgnoreCase)) return "one-piece";
        var game = args.Skip(1).FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
        return string.IsNullOrWhiteSpace(game) ? "pokemon" : NormalizeGame(game);
    }

    private static string NormalizeGame(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "pokemon" or "pokemontcg" => "pokemon",
            "onepiece" or "one-piece" or "one_piece" or "op" => "one-piece",
            var game => game
        };

    private static async Task SyncGameAsync(IServiceProvider services, string game, int batchSize, bool fromStart, CancellationToken ct)
    {
        var (sourceName, gameSlug) = SourceForGame(game);
        Console.WriteLine($"Catalog sync: {gameSlug} from {sourceName}");
        Console.WriteLine($"Mode: {(fromStart ? "from start" : "resume checkpoint")} | batch size: {batchSize}");

        await RunImportLoopAsync(services, sourceName, gameSlug, "Sets", Math.Max(batchSize, 5000), fromStart, ct);
        await RunImportLoopAsync(services, sourceName, gameSlug, "Cards", batchSize, fromStart, ct);
    }

    private static async Task RunImportLoopAsync(IServiceProvider services, string sourceName, string gameSlug, string importType, int batchSize, bool fromStart, CancellationToken ct)
    {
        var imports = services.GetRequiredService<ICatalogImportService>();
        string? checkpoint = null;
        var loop = 0;

        while (true)
        {
            loop++;
            var request = new StartCatalogImportRequest
            {
                SourceName = sourceName,
                GameSlug = gameSlug,
                ImportType = importType,
                DryRun = false,
                MaxRecords = batchSize,
                IncludeImages = true,
                UpdateExistingProducts = true,
                CreateMissingProducts = true,
                UseCheckpoint = !fromStart && loop == 1,
                SaveCheckpoint = true,
                CheckpointValue = checkpoint
            };

            var run = await imports.StartImportAsync(request, ct);
            Console.WriteLine($"{importType} batch {loop}: {run.Status} | processed {run.RecordsProcessed} | created {run.RecordsCreated} | updated {run.RecordsUpdated} | skipped {run.RecordsSkipped} | errors {run.ErrorCount} | next {run.NextCheckpointValue ?? "complete"}");

            if (!run.HasMore)
            {
                break;
            }

            checkpoint = run.NextCheckpointValue;
            if (string.IsNullOrWhiteSpace(checkpoint))
            {
                break;
            }
        }
    }

    private static async Task ValidateGameAsync(IServiceProvider services, string game, CancellationToken ct)
    {
        if (game == "pokemon")
        {
            await ValidatePokemonAsync(services, ct);
            return;
        }

        if (game == "one-piece")
        {
            await ValidateOnePieceAsync(services, ct);
            return;
        }

        throw new InvalidOperationException($"Unsupported catalog validation game '{game}'.");
    }

    private static async Task ValidatePokemonAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<CardsDbContext>();
        var pokemon = services.GetRequiredService<PokemonTcgApiClient>();
        var cardProbe = await pokemon.GetCardsAsync(1, null, ct);
        var setProbe = await pokemon.GetSetsAsync(250, null, ct);

        var localProducts = await db.CatalogProducts.CountAsync(p => p.Game != null && p.Game.Slug == "pokemon" && p.IsActive && p.IsSingleCard, ct);
        var localSets = await db.CardSets.CountAsync(s => s.Game != null && s.Game.Slug == "pokemon" && s.IsActive, ct);
        Console.WriteLine("");
        Console.WriteLine("Pokemon catalog validation");
        Console.WriteLine($"Provider cards: {cardProbe.TotalCount:N0} | Local single-card products: {localProducts:N0} | Delta: {localProducts - cardProbe.TotalCount:N0}");
        Console.WriteLine($"Provider sets:  {setProbe.TotalCount:N0} | Local sets:     {localSets:N0} | Delta: {localSets - setProbe.TotalCount:N0}");
        Console.WriteLine("Recent set spot-checks:");

        foreach (var set in setProbe.Data.Take(12))
        {
            var code = set.Id.ToUpperInvariant();
            var expected = set.Total ?? set.PrintedTotal;
            var local = await db.CatalogProducts.CountAsync(p => p.CardSet != null && p.CardSet.Game != null && p.CardSet.Game.Slug == "pokemon" && p.CardSet.Code == code && p.IsActive && p.IsSingleCard, ct);
            var localByName = local == 0
                ? await db.CatalogProducts.CountAsync(p => p.CardSet != null && p.CardSet.Game != null && p.CardSet.Game.Slug == "pokemon" && p.CardSet.NormalizedName == CatalogTextNormalizer.NormalizeName(set.Name) && p.IsActive && p.IsSingleCard, ct)
                : local;
            var marker = expected.HasValue && expected.Value != local ? "CHECK" : "OK";
            var hint = local == 0 && localByName > 0 ? $" | local by name {localByName,5} (run sync to repair set code)" : "";
            Console.WriteLine($"{marker} {code,-10} {set.Name,-36} expected {Display(expected),5} | local {local,5}{hint}");
        }

        await PrintExtraLocalSetsAsync(db, "pokemon", setProbe.Data.Select(s => s.Id.ToUpperInvariant()), ct);
    }

    private static async Task ValidateOnePieceAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<CardsDbContext>();
        var provider = services.GetRequiredService<OnePieceOfficialCatalogImportProvider>();
        var reports = await provider.GetOfficialSetReportsAsync(ct);
        var localProducts = await db.CatalogProducts.CountAsync(p => p.Game != null && p.Game.Slug == "one-piece" && p.IsActive && p.IsSingleCard, ct);
        var localSets = await db.CardSets.CountAsync(s => s.Game != null && s.Game.Slug == "one-piece" && s.IsActive, ct);
        var expectedCanonical = reports.Sum(r => r.CanonicalCardCount);

        Console.WriteLine("");
        Console.WriteLine("One Piece catalog validation");
        Console.WriteLine($"Official sets: {reports.Count:N0} | Local sets: {localSets:N0} | Delta: {localSets - reports.Count:N0}");
        Console.WriteLine($"Official canonical cards: {expectedCanonical:N0} | Local single-card products: {localProducts:N0} | Delta: {localProducts - expectedCanonical:N0}");
        Console.WriteLine("Set spot-checks:");

        foreach (var report in reports.Take(20))
        {
            var local = await db.CatalogProducts.CountAsync(p => p.CardSet != null && p.CardSet.Game != null && p.CardSet.Game.Slug == "one-piece" && p.CardSet.Code == report.Code && p.IsActive && p.IsSingleCard, ct);
            var marker = report.CanonicalCardCount != local ? "CHECK" : "OK";
            Console.WriteLine($"{marker} {report.Code,-10} {report.Name,-42} official rows {report.OfficialRowCount,4} | canonical {report.CanonicalCardCount,4} | local {local,4}");
        }

        await PrintExtraLocalSetsAsync(db, "one-piece", reports.Select(r => r.Code), ct);
    }

    private static async Task PrintExtraLocalSetsAsync(CardsDbContext db, string gameSlug, IEnumerable<string> providerCodes, CancellationToken ct)
    {
        var providerCodeSet = providerCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var extraSets = await db.CardSets
            .Where(s => s.Game != null && s.Game.Slug == gameSlug && s.IsActive)
            .Where(s => s.Code == null || !providerCodeSet.Contains(s.Code))
            .OrderBy(s => s.Name)
            .Select(s => new { s.Name, s.Code })
            .Take(10)
            .ToListAsync(ct);

        if (extraSets.Count == 0)
        {
            return;
        }

        Console.WriteLine("Local sets not present in provider validation codes:");
        foreach (var set in extraSets)
        {
            Console.WriteLine($"CHECK {set.Code ?? "no-code",-10} {set.Name}");
        }
    }

    private static (string SourceName, string GameSlug) SourceForGame(string game)
        => game switch
        {
            "pokemon" => ("PokemonTCG", "pokemon"),
            "one-piece" => ("OnePieceOfficial", "one-piece"),
            _ => throw new InvalidOperationException($"Unsupported catalog sync game '{game}'.")
        };

    private static int ReadInt(string[] args, string key, int defaultValue)
    {
        var index = Array.FindIndex(args, arg => arg.Equals(key, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value)
            ? value
            : defaultValue;
    }

    private static string Display(int? value) => value.HasValue ? value.Value.ToString() : "n/a";
}
