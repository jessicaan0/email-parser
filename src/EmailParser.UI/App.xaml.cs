using System.Text;
using EmailParser.UI.Services;
using Microsoft.UI.Xaml;
using Serilog;

namespace EmailParser.UI;

/// <summary>
/// Application entry point. Configures Serilog logging and launches the main window.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Shared Serilog sink that forwards log entries to the UI for real-time display.
    /// </summary>
    public static UiLogSink UiSink { get; } = new();

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;

        ConfigureLogging();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Required for MsgReader and iText7 encoding support.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var window = new MainWindow();
        window.Activate();
    }

    private static void ConfigureLogging()
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "EmailParser", "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(UiSink)
            .WriteTo.File(
                Path.Combine(logDir, "EmailParser-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}" +
                    "  {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Email Parser UI starting");
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception in application");
        e.Handled = true;
    }
}
