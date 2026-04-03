namespace EmailParser.UI.Models;

/// <summary>
/// Represents a single log entry displayed in the UI log viewer.
/// </summary>
public sealed class LogEntry
{
    public required string Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
}
