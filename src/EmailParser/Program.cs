using System.Text;
using EmailParser.Core.Services;
using Serilog;

namespace EmailParser;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // PATH CONFIGURATION — change these lines to move files to a different location.
        string outputBaseDir     = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmailParser");
        string reportsDir        = @"C:\Users\JessicaAnyanwu\OneDrive - Suir Engineering Ltd\Documents\EmailParser\Email Parcer Directory";
        string dataDictionaryDir = reportsDir;  // data dictionary lives in the same folder as reports

        // Configure Serilog: write to both console and a rolling log file.
        string logDir = Path.Combine(outputBaseDir, "Logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(logDir, "EmailParser-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            string folderPath = ResolveSource(args);
            var service = new EmailProcessingService();
            service.Run(folderPath, outputBaseDir, reportsDir, dataDictionaryDir);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception — application terminating");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Resolves the email source from command-line arguments or an interactive prompt.
    /// </summary>
    private static string ResolveSource(string[] args)
    {
        string folderPath;

        if (args.Length > 0)
        {
            folderPath = args[0].Trim();
            Log.Information("Source (from argument): {Source}", folderPath);
        }
        else
        {
            Console.Write(
                "Enter an Outlook folder name (e.g. 'Inbox' or 'Inbox/Projects'),\n" +
                "or a path to a local directory containing .msg files: ");
            folderPath = Console.ReadLine()?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Log.Fatal("Source cannot be empty");
            Environment.Exit(1);
        }

        return folderPath;
    }
}
