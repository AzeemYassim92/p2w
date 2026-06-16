using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace P2W.Cards.Infrastructure.Services;

public sealed class LocalSessionLog(ILogger<LocalSessionLog> logger)
{
    private readonly object gate = new();
    private bool started;
    private string sessionId = "";

    public string LogDirectory { get; private set; } = "";
    public string LogPath { get; private set; } = "";
    public DateTime StartedUtc { get; private set; }

    public void StartSession()
    {
        lock (gate)
        {
            if (started)
            {
                return;
            }

            StartedUtc = DateTime.UtcNow;
            sessionId = StartedUtc.ToString("yyyyMMddTHHmmssZ");
            LogDirectory = Path.Combine(FindWorkspaceRoot(), "logs");
            LogPath = Path.Combine(LogDirectory, "session.log");
            Directory.CreateDirectory(LogDirectory);

            foreach (var file in Directory.EnumerateFiles(LogDirectory, "*.log"))
            {
                File.Delete(file);
            }

            started = true;
            WriteCore("Information", "session", "session.start", "Local diagnostics session started.", new
            {
                SessionId = sessionId,
                ProcessId = Environment.ProcessId,
                Machine = Environment.MachineName,
                Workspace = FindWorkspaceRoot()
            }, null);
        }
    }

    public void Debug(string category, string eventName, string message, object? data = null)
        => Write("Debug", category, eventName, message, data);

    public void Info(string category, string eventName, string message, object? data = null)
        => Write("Information", category, eventName, message, data);

    public void Warning(string category, string eventName, string message, object? data = null)
        => Write("Warning", category, eventName, message, data);

    public void Error(string category, string eventName, string message, Exception? exception = null, object? data = null)
        => Write("Error", category, eventName, message, data, exception);

    public void Write(string level, string category, string eventName, string message, object? data = null, Exception? exception = null)
    {
        lock (gate)
        {
            if (!started)
            {
                StartSession();
            }

            WriteCore(level, category, eventName, message, data, exception);
        }
    }

    private void WriteCore(string level, string category, string eventName, string message, object? data, Exception? exception)
    {
        try
        {
            var dataJson = SafeSerialize(data);
            var exceptionText = exception == null ? "" : $" | exception={exception.GetType().Name}: {exception.Message}";
            var line = $"{DateTime.UtcNow:O} | {level} | {category} | {eventName} | {message}";
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                line += $" | data={dataJson}";
            }

            line += exceptionText;
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch (Exception logException)
        {
            logger.LogWarning(logException, "Unable to write local session diagnostics log.");
        }
    }

    private static string? SafeSerialize(object? data)
    {
        if (data == null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(data);
        return json.Length <= 4000 ? json : json[..4000] + "...";
    }

    private static string FindWorkspaceRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
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
        }

        return Directory.GetCurrentDirectory();
    }
}
