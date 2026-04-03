using EmailParser.UI.Models;
using Serilog.Core;
using Serilog.Events;

namespace EmailParser.UI.Services;

/// <summary>
/// Custom Serilog sink that forwards log entries to the UI for real-time display.
/// Early log entries are buffered until a callback is registered.
/// </summary>
public sealed class UiLogSink : ILogEventSink
{
    private Action<LogEntry>? _addEntry;
    private readonly List<LogEntry> _buffer = [];
    private readonly object _lock = new();

    /// <summary>
    /// Registers the callback that adds log entries to the UI.
    /// Any buffered entries are immediately replayed.
    /// </summary>
    public void SetCallback(Action<LogEntry> addEntry)
    {
        lock (_lock)
        {
            _addEntry = addEntry;
            foreach (var entry in _buffer)
                _addEntry(entry);
            _buffer.Clear();
        }
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        if (logEvent.Exception != null)
            message += Environment.NewLine + logEvent.Exception;

        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.ToString("HH:mm:ss.fff"),
            Level = FormatLevel(logEvent.Level),
            Message = message
        };

        lock (_lock)
        {
            if (_addEntry != null)
                _addEntry(entry);
            else
                _buffer.Add(entry);
        }
    }

    private static string FormatLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => "???"
    };
}
