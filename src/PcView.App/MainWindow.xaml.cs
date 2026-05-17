using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PcView.Core;

namespace PcView.App;

public partial class MainWindow : Window
{
    private readonly PcViewScanner _scanner = new();
    private readonly ObservableCollection<AppRow> _visibleApps = [];
    private ScanResult? _scanResult;
    private AppRow? _selectedApp;
    private bool _isInitialized;

    public MainWindow()
    {
        InitializeComponent();
        AppsGrid.ItemsSource = _visibleApps;
        _isInitialized = true;
        UpdateOverview();
        ApplyFilter();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        await ScanAsync(refresh: false);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ScanAsync(refresh: true);
    }

    public Task RunScanAsync(bool refresh)
    {
        return ScanAsync(refresh);
    }

    private async Task ScanAsync(bool refresh)
    {
        SetBusy(true);
        StatusText.Text = refresh ? "Rebuilding the local index..." : "Scanning and reusing the local cache...";
        try
        {
            _scanResult = await _scanner.ScanAsync(new ScanOptions { Refresh = refresh });
            AppCountText.Text = _scanResult.AppCount.ToString();
            ReviewCountText.Text = _scanResult.ReviewCount.ToString();
            FolderCountText.Text = _scanResult.Apps.Count(app => app.Source == AppSource.Folder).ToString();
            DurationText.Text = $"{_scanResult.Duration.TotalMilliseconds:N0} ms";
            CacheText.Text = _scanResult.CachePath;
            StatusText.Text = $"Completed. Generated at {_scanResult.GeneratedUtc.LocalDateTime:G}.";
            UpdateOverview();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Scan failed: {ex.Message}";
            System.Windows.MessageBox.Show(this, ex.Message, "Scan failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        ScanButton.IsEnabled = !busy;
        RefreshButton.IsEnabled = !busy;
        SearchBox.IsEnabled = !busy;
    }

    private void Filter_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ApplyFilter();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        SearchHint.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!_isInitialized)
        {
            return;
        }

        _visibleApps.Clear();
        bool isOverview = OverviewFilter.IsChecked == true && string.IsNullOrWhiteSpace(SearchBox.Text);
        OverviewPanel.Visibility = isOverview ? Visibility.Visible : Visibility.Collapsed;
        AppsTableCard.Visibility = isOverview ? Visibility.Collapsed : Visibility.Visible;

        if (_scanResult is null)
        {
            SubtitleText.Text = isOverview
                ? "Scan registered apps, shortcuts, common app folders, and local evidence."
                : "Run a scan to populate this view.";
            return;
        }

        var query = SearchBox.Text.Trim();
        var rows = _scanResult.Apps.Select(app => new AppRow(app)).Where(row => MatchesFilter(row, query));
        foreach (var row in rows)
        {
            _visibleApps.Add(row);
        }

        SubtitleText.Text = _visibleApps.Count == _scanResult.Apps.Count
            ? "Showing all scanned applications."
            : $"Showing {_visibleApps.Count} of {_scanResult.Apps.Count} applications.";
    }

    private void UpdateOverview()
    {
        if (_scanResult is null)
        {
            ReviewFocusText.Text = "Run a scan to build the review queue.";
            EvidenceHealthText.Text = "Evidence confidence will appear after scanning.";
            TopReasonsList.ItemsSource = null;
            return;
        }

        int folderCount = _scanResult.Apps.Count(app => app.Source == AppSource.Folder);
        int unknownEvidenceCount = _scanResult.Apps.Count(app => app.LastRunEvidence.Confidence == EvidenceConfidence.Unknown);
        int lowConfidenceCount = _scanResult.Apps.Count(app => app.LastRunEvidence.Confidence == EvidenceConfidence.Low);
        int mediumConfidenceCount = _scanResult.Apps.Count(app => app.LastRunEvidence.Confidence == EvidenceConfidence.Medium);

        ReviewFocusText.Text = _scanResult.ReviewCount == 0
            ? "No review-worthy applications were found. Browse all apps if you want to inspect the full inventory."
            : $"{_scanResult.ReviewCount} applications need review. Start with folder discoveries and unknown publishers.";

        EvidenceHealthText.Text =
            $"{mediumConfidenceCount} apps have medium-confidence run evidence, {lowConfidenceCount} rely on low-confidence file access evidence, and {unknownEvidenceCount} have no run evidence.";

        TopReasonsList.ItemsSource = _scanResult.Apps
            .SelectMany(app => app.Recommendation.Reasons)
            .GroupBy(ReasonLabel)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .Select(group => $"{group.Key} ({group.Count()})")
            .ToArray();
    }

    private bool MatchesFilter(AppRow row, string query)
    {
        if (ReviewFilter.IsChecked == true && row.Entry.Recommendation.Level != RecommendationLevel.Review)
        {
            return false;
        }

        if (FolderFilter.IsChecked == true && row.Entry.Source != AppSource.Folder)
        {
            return false;
        }

        if (UnknownFilter.IsChecked == true && row.Entry.LastRunEvidence.Confidence != EvidenceConfidence.Unknown)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(query) || row.Contains(query);
    }

    private void AppsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedApp = AppsGrid.SelectedItem as AppRow;
        RenderDetails(_selectedApp);
    }

    private void RenderDetails(AppRow? row)
    {
        if (row is null)
        {
            DetailNameText.Text = "Select an app";
            DetailMetaText.Text = "Run a scan, then choose an item to inspect evidence and actions.";
            EvidenceText.Text = "-";
            LocationText.Text = "-";
            ReasonsList.ItemsSource = null;
            ExecutablesList.ItemsSource = null;
            OpenFolderButton.IsEnabled = false;
            SelectExeButton.IsEnabled = false;
            UninstallButton.IsEnabled = false;
            return;
        }

        var app = row.Entry;
        DetailNameText.Text = app.Name;
        DetailMetaText.Text = string.Join(" / ", new[] { row.Publisher, row.SourceLabel, app.Version }.Where(value => !string.IsNullOrWhiteSpace(value)));
        EvidenceText.Text = $"{row.LastRunLabel}; source: {app.LastRunEvidence.Source}; confidence: {ConfidenceLabel(app.LastRunEvidence.Confidence)}";
        LocationText.Text = app.InstallLocation ?? "No install directory is available.";
        ReasonsList.ItemsSource = app.Recommendation.Reasons.Select(ReasonLabel).ToArray();
        ExecutablesList.ItemsSource = app.Executables.Select(exe => $"{exe.Name}  -  {exe.Path}").Take(30);
        OpenFolderButton.IsEnabled = !string.IsNullOrWhiteSpace(app.InstallLocation);
        SelectExeButton.IsEnabled = !string.IsNullOrWhiteSpace(app.PrimaryExecutable);
        UninstallButton.IsEnabled = !string.IsNullOrWhiteSpace(app.UninstallCommand);
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedApp?.Entry.InstallLocation is { Length: > 0 } path && Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
    }

    private void SelectExeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedApp?.Entry.PrimaryExecutable is { Length: > 0 } path && File.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
    }

    private void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        var command = _selectedApp?.Entry.UninstallCommand;
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            $"PcView will run the official uninstall entry registered in Windows:\n\n{command}",
            "Confirm uninstall entry",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.OK)
        {
            Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"")
            {
                UseShellExecute = true
            });
        }
    }

    private static string ConfidenceLabel(EvidenceConfidence confidence)
    {
        return confidence switch
        {
            EvidenceConfidence.Low => "low",
            EvidenceConfidence.Medium => "medium",
            EvidenceConfidence.High => "high",
            _ => "unknown"
        };
    }

    private sealed class AppRow(AppEntry entry)
    {
        public AppEntry Entry { get; } = entry;
        public string Name => Entry.Name;
        public string Publisher => string.IsNullOrWhiteSpace(Entry.Publisher) ? "Unknown publisher" : Entry.Publisher;
        public string SourceLabel => Entry.Source switch
        {
            AppSource.Folder => "Folder",
            AppSource.Shortcut => "Shortcut",
            _ => "Uninstall list"
        };
        public string LastRunLabel => Entry.LastRunEvidence.TimestampUtc.HasValue
            ? Entry.LastRunEvidence.TimestampUtc.Value.LocalDateTime.ToString("yyyy-MM-dd")
            : "Unknown";
        public string ReasonSummary => string.Join(", ", Entry.Recommendation.Reasons.Select(ReasonLabel));
        public string PrimaryReason => ReasonLabel(Entry.Recommendation.Reasons.FirstOrDefault() ?? "Recent or clearly registered");
        public string ExtraReasonCount => Entry.Recommendation.Reasons.Count > 1
            ? $"+{Entry.Recommendation.Reasons.Count - 1}"
            : "";

        public bool Contains(string query)
        {
            return new[]
            {
                Entry.Name,
                Entry.Publisher,
                Entry.InstallLocation,
                Entry.PrimaryExecutable,
                Entry.Version,
                ReasonSummary
            }.Where(value => !string.IsNullOrWhiteSpace(value))
             .Any(value => value!.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    private static string ReasonLabel(string reason)
    {
        if (reason == "Not in uninstall list") return "Not in uninstall list";
        if (reason == "Unknown publisher") return "Unknown publisher";
        if (reason == "Installed under user profile") return "Installed under user profile";
        if (reason == "Last-run evidence unavailable") return "Last-run evidence unavailable";
        if (reason == "Recent or clearly registered") return "Recent or clearly registered";
        if (reason.StartsWith("Not run for ", StringComparison.Ordinal))
        {
            var days = reason.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(3);
            return $"{days} days without run evidence";
        }

        return reason;
    }
}
