using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using P2W.Cards.Infrastructure;
using P2W.Cards.Worker.Aggregation;

var builder = Host.CreateApplicationBuilder(args);
var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
var apiConfigDirectory = Path.Combine(repoRoot, "src", "P2W.Cards.Api");

builder.Configuration
    .AddJsonFile(Path.Combine(apiConfigDirectory, "appsettings.json"), optional: true, reloadOnChange: false)
    .AddJsonFile(Path.Combine(apiConfigDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: false)
    .AddJsonFile(Path.Combine(apiConfigDirectory, "appsettings.Local.json"), optional: true, reloadOnChange: false)
    .AddJsonFile(Path.Combine(apiConfigDirectory, $"appsettings.{builder.Environment.EnvironmentName}.local.json"), optional: true, reloadOnChange: false);

builder.Services.AddCardsInfrastructure(builder.Configuration);
builder.Services.AddTransient<CatalogSyncJobRunner>();

var command = args.FirstOrDefault();
if (CatalogSyncJobRunner.IsCatalogCommand(command))
{
    using var host = builder.Build();
    var runner = host.Services.GetRequiredService<CatalogSyncJobRunner>();
    return await runner.RunAsync(args, CancellationToken.None);
}

builder.Services.AddHostedService<MarketAggregationWorker>();
await builder.Build().RunAsync();
return 0;

static string FindRepoRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "P2W.Cards.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return start;
}
