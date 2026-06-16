using System.Diagnostics;
using P2W.Cards.Infrastructure;
using P2W.Cards.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddCardsInfrastructure(builder.Configuration);

var app = builder.Build();
var sessionLog = app.Services.GetRequiredService<LocalSessionLog>();
sessionLog.StartSession();
sessionLog.Info("api", "api.start", "API application started.", new
{
    Environment = app.Environment.EnvironmentName,
    ContentRoot = app.Environment.ContentRootPath
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/api/diagnostics/client-log"))
    {
        await next();
        return;
    }

    var stopwatch = Stopwatch.StartNew();
    sessionLog.Info("api", "api.request.start", "API request started.", new
    {
        context.TraceIdentifier,
        Method = context.Request.Method,
        Path = context.Request.Path.Value,
        Query = context.Request.QueryString.Value
    });

    try
    {
        await next();
        stopwatch.Stop();
        sessionLog.Info("api", "api.request.complete", "API request completed.", new
        {
            context.TraceIdentifier,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            StatusCode = context.Response.StatusCode,
            ElapsedMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        sessionLog.Error("api", "api.request.failed", "API request failed.", ex, new
        {
            context.TraceIdentifier,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            ElapsedMs = stopwatch.ElapsedMilliseconds
        });
        throw;
    }
});
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));

app.Run();
