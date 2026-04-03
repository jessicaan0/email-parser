using EmailParser.UI.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace EmailParser.UI;

/// <summary>
/// Main application window. Handles folder picking, radio button state,
/// log auto-scrolling, and window lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        Title = "Email Parser";
        SetWindowSize(960, 820);

        ViewModel = new MainViewModel(DispatcherQueue);

        // Wire up the UI log sink so real-time logs appear in the ListView.
        App.UiSink.SetCallback(ViewModel.AddLogEntry);

        // Auto-scroll the log viewer when new entries arrive.
        ViewModel.LogEntryAdded += OnLogEntryAdded;

        // Set initial radio button state from saved settings.
        OutlookRadio.IsChecked = ViewModel.IsOutlookSource;
        MsgRadio.IsChecked = !ViewModel.IsOutlookSource;
        UpdateSourcePanelVisibility();

        // Save settings when the window closes.
        this.Closed += OnWindowClosed;
    }

    // ── Window helpers ────────────────────────────────────────────────

    private void SetWindowSize(int width, int height)
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(width, height));
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        ViewModel.SaveSettings();
        Log.CloseAndFlush();
    }

    // ── Source type radio buttons ─────────────────────────────────────

    private void SourceType_Checked(object sender, RoutedEventArgs e)
    {
        ViewModel.IsOutlookSource = OutlookRadio.IsChecked == true;
        UpdateSourcePanelVisibility();
    }

    private void UpdateSourcePanelVisibility()
    {
        if (OutlookPanel == null || MsgPanel == null) return;

        OutlookPanel.Visibility = ViewModel.IsOutlookSource
            ? Visibility.Visible
            : Visibility.Collapsed;
        MsgPanel.Visibility = ViewModel.IsOutlookSource
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    // ── Folder browse buttons ─────────────────────────────────────────

    private async void BrowseMsgDir_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path != null)
            ViewModel.MsgDirectory = path;
    }

    private async void BrowseOutputDir_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path != null)
            ViewModel.OutputBaseDir = path;
    }

    private async void BrowseReportsDir_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path != null)
            ViewModel.ReportsDir = path;
    }

    private async void BrowseDictDir_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path != null)
            ViewModel.DataDictionaryDir = path;
    }

    private async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        // WinUI 3 unpackaged: must initialize picker with the window handle.
        var hWnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hWnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    // ── Footer buttons ────────────────────────────────────────────────

    private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
        => MainViewModel.OpenFolder(ViewModel.OutputBaseDir);

    private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        => MainViewModel.OpenFolder(ViewModel.LogsDir);

    private void ClearLog_Click(object sender, RoutedEventArgs e)
        => ViewModel.ClearLog();

    // ── Log auto-scroll ───────────────────────────────────────────────

    private void OnLogEntryAdded()
    {
        if (LogListView.Items.Count > 0)
        {
            LogListView.ScrollIntoView(LogListView.Items[^1]);
        }
    }
}
