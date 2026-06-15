using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Options;

namespace P2W.Cards.Infrastructure.Services;

public sealed class MarketDiagnosticTrail(IOptions<MarketDiagnosticsOptions> options, ILogger<MarketDiagnosticTrail> logger)
{
    private readonly List<MarketDiagnosticEventDto> events = new();

    public bool Enabled => options.Value.Enabled;
    public bool IncludeSearchQueries => options.Value.IncludeSearchQueries;
    public bool IncludeMatchCandidates => options.Value.IncludeMatchCandidates;
    public bool IncludeProviderPayloadHints => options.Value.IncludeProviderPayloadHints;

    public IReadOnlyList<MarketDiagnosticEventDto> Events => events.ToArray();

    public void Debug(string stage, string message, object? data = null) => Add(LogLevel.Debug, stage, message, data);
    public void Info(string stage, string message, object? data = null) => Add(LogLevel.Information, stage, message, data);
    public void Warning(string stage, string message, object? data = null) => Add(LogLevel.Warning, stage, message, data);
    public void Error(string stage, string message, Exception? exception = null, object? data = null) => Add(LogLevel.Error, stage, message, data, exception);

    private void Add(LogLevel level, string stage, string message, object? data, Exception? exception = null)
    {
        var dataJson = data == null ? null : JsonSerializer.Serialize(data);
        logger.Log(level, exception, "{MarketStage}: {MarketMessage} {MarketData}", stage, message, dataJson);

        if (!Enabled || events.Count >= Math.Clamp(options.Value.MaxEventsPerRun, 1, 500))
        {
            return;
        }

        events.Add(new MarketDiagnosticEventDto
        {
            AtUtc = DateTime.UtcNow,
            Level = level.ToString(),
            Stage = stage,
            Message = message,
            Data = dataJson
        });
    }
}
