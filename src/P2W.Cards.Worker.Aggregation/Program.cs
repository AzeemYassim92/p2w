using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using P2W.Cards.Infrastructure;
using P2W.Cards.Worker.Aggregation;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCardsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MarketAggregationWorker>();

await builder.Build().RunAsync();
