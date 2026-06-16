using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController(LocalSessionLog sessionLog, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("health")]
    public ActionResult<LocalHealthDto> Health()
    {
        sessionLog.Info("api", "api.health", "Frontend/API health check received.", new
        {
            Environment = environment.EnvironmentName,
            sessionLog.LogPath
        });

        return new LocalHealthDto
        {
            Status = "Ready",
            Environment = environment.EnvironmentName,
            ServerTimeUtc = DateTime.UtcNow,
            SessionStartedUtc = sessionLog.StartedUtc,
            LogPath = sessionLog.LogPath
        };
    }

    [HttpGet("session")]
    public ActionResult<LocalSessionDto> Session()
        => new LocalSessionDto
        {
            StartedUtc = sessionLog.StartedUtc,
            LogDirectory = sessionLog.LogDirectory,
            LogPath = sessionLog.LogPath
        };

    [HttpPost("client-log")]
    public IActionResult ClientLog(ClientDiagnosticEventRequest request)
    {
        var level = string.IsNullOrWhiteSpace(request.Level) ? "Information" : request.Level.Trim();
        sessionLog.Write(level, "frontend", request.EventName, request.Message ?? "Frontend event.", request.Data);
        return NoContent();
    }
}

public sealed class ClientDiagnosticEventRequest
{
    public string Level { get; set; } = "Information";
    public string EventName { get; set; } = "frontend.event";
    public string? Message { get; set; }
    public object? Data { get; set; }
}

public sealed class LocalHealthDto
{
    public string Status { get; set; } = "";
    public string Environment { get; set; } = "";
    public DateTime ServerTimeUtc { get; set; }
    public DateTime SessionStartedUtc { get; set; }
    public string LogPath { get; set; } = "";
}

public sealed class LocalSessionDto
{
    public DateTime StartedUtc { get; set; }
    public string LogDirectory { get; set; } = "";
    public string LogPath { get; set; } = "";
}
