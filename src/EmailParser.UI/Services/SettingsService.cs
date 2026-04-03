using System.Text.Json;
using Serilog;

namespace EmailParser.UI.Services;

/// <summary>
/// Persists user settings to a JSON file in %LocalAppData%\EmailParser.
/// </summary>
public sealed class SettingsService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<SettingsService>();

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EmailParser");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Log.Debug("No settings file found at {Path}, using defaults", SettingsPath);
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            Log.Debug("Loaded settings from {Path}", SettingsPath);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load settings from {Path}, using defaults", SettingsPath);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
            Log.Debug("Saved settings to {Path}", SettingsPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save settings to {Path}", SettingsPath);
        }
    }
}

/// <summary>
/// User-configurable application settings.
/// </summary>
public sealed class AppSettings
{
    public bool IsOutlookSource { get; set; } = true;
    public string OutlookFolder { get; set; } = "Inbox";
    public string MsgDirectory { get; set; } = "";

    public string OutputBaseDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmailParser");

    public string ReportsDir { get; set; } = "";
    public string DataDictionaryDir { get; set; } = "";
}
