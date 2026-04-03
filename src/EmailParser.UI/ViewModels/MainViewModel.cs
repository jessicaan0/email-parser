using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmailParser.Core.Services;
using EmailParser.UI.Models;
using EmailParser.UI.Services;
using Microsoft.UI.Dispatching;
using Serilog;

namespace EmailParser.UI.ViewModels;

/// <summary>
/// Main view model for the Email Parser desktop application.
/// Manages source/output configuration, processing execution, and log display.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private static readonly ILogger Log = Serilog.Log.ForContext<MainViewModel>();

    private readonly SettingsService _settingsService = new();
    private readonly DispatcherQueue _dispatcherQueue;

    private const int MaxLogEntries = 5000;

    // ── Source configuration ──────────────────────────────────────────

    [ObservableProperty]
    private bool _isOutlookSource = true;

    [ObservableProperty]
    private string _outlookFolder = "Inbox";

    [ObservableProperty]
    private string _msgDirectory = "";

    // ── Output configuration ──────────────────────────────────────────

    [ObservableProperty]
    private string _outputBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmailParser");

    [ObservableProperty]
    private string _reportsDir = "";

    [ObservableProperty]
    private string _dataDictionaryDir = "";

    // ── Processing state ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isProgressIndeterminate;

    [ObservableProperty]
    private bool _isProgressError;

    // ── Log display ───────────────────────────────────────────────────

    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    /// <summary>
    /// Fired when a new log entry is added so the view can auto-scroll.
    /// </summary>
    public event Action? LogEntryAdded;

    // ── Constructor ───────────────────────────────────────────────────

    public MainViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
        LoadSettings();
    }

    // ── Commands ──────────────────────────────────────────────────────

    private bool CanRun() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        // Validate inputs before starting.
        string source = IsOutlookSource ? OutlookFolder : MsgDirectory;
        if (string.IsNullOrWhiteSpace(source))
        {
            StatusText = IsOutlookSource
                ? "Please enter an Outlook folder path"
                : "Please select a .msg file directory";
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputBaseDir))
        {
            StatusText = "Please specify an output directory";
            return;
        }

        // Persist settings before running.
        SaveSettings();

        IsRunning = true;
        IsProgressIndeterminate = true;
        IsProgressError = false;
        StatusText = "Processing emails...";

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                var service = new EmailProcessingService();
                service.Run(
                    source,
                    OutputBaseDir,
                    string.IsNullOrWhiteSpace(ReportsDir) ? OutputBaseDir : ReportsDir,
                    string.IsNullOrWhiteSpace(DataDictionaryDir) ? OutputBaseDir : DataDictionaryDir);
            });

            stopwatch.Stop();
            StatusText = $"Completed successfully in {stopwatch.Elapsed.TotalSeconds:F1}s";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            StatusText = $"Failed: {ex.Message}";
            IsProgressError = true;
            Log.Error(ex, "Email processing failed after {Elapsed}s", stopwatch.Elapsed.TotalSeconds);
        }
        finally
        {
            IsRunning = false;
            IsProgressIndeterminate = false;
        }
    }

    // ── Log entry callback (called from UiLogSink) ───────────────────

    /// <summary>
    /// Adds a log entry to the UI collection. Called from the Serilog sink;
    /// dispatches to the UI thread automatically.
    /// </summary>
    public void AddLogEntry(LogEntry entry)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            LogEntries.Add(entry);

            // Cap the number of displayed log entries to prevent unbounded memory growth.
            while (LogEntries.Count > MaxLogEntries)
                LogEntries.RemoveAt(0);

            LogEntryAdded?.Invoke();
        });
    }

    /// <summary>Clears all displayed log entries.</summary>
    public void ClearLog() => LogEntries.Clear();

    // ── Settings persistence ──────────────────────────────────────────

    public void SaveSettings()
    {
        _settingsService.Save(new AppSettings
        {
            IsOutlookSource = IsOutlookSource,
            OutlookFolder = OutlookFolder,
            MsgDirectory = MsgDirectory,
            OutputBaseDir = OutputBaseDir,
            ReportsDir = ReportsDir,
            DataDictionaryDir = DataDictionaryDir
        });
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();

        IsOutlookSource = settings.IsOutlookSource;
        OutlookFolder = settings.OutlookFolder;
        MsgDirectory = settings.MsgDirectory;
        OutputBaseDir = settings.OutputBaseDir;
        ReportsDir = settings.ReportsDir;
        DataDictionaryDir = settings.DataDictionaryDir;
    }

    // ── Folder helpers ────────────────────────────────────────────────

    /// <summary>
    /// Opens the given directory in Windows Explorer. No-op if the path is empty.
    /// </summary>
    public static void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not open folder {Path}", path);
        }
    }

    public string LogsDir => Path.Combine(OutputBaseDir, "Logs");
}
