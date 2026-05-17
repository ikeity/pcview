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
        ApplyLocalization();
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
        StatusText.Text = refresh ? AppText.Get("Status.Rebuilding") : AppText.Get("Status.Scanning");
        try
        {
            _scanResult = await _scanner.ScanAsync(new ScanOptions { Refresh = refresh });
            AppCountText.Text = _scanResult.AppCount.ToString();
            ReviewCountText.Text = _scanResult.ReviewCount.ToString();
            FolderCountText.Text = _scanResult.Apps.Count(app => app.Source == AppSource.Folder).ToString();
            DurationText.Text = $"{_scanResult.Duration.TotalMilliseconds:N0} ms";
            CacheText.Text = _scanResult.CachePath;
            StatusText.Text = AppText.Format("Status.Completed", _scanResult.GeneratedUtc.LocalDateTime);
            UpdateOverview();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("Status.ScanFailed", ex.Message);
            System.Windows.MessageBox.Show(this, ex.Message, AppText.Get("Dialog.ScanFailed.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                ? AppText.Get("Page.Subtitle.Default")
                : AppText.Get("Page.Subtitle.NoScan");
            return;
        }

        var query = SearchBox.Text.Trim();
        var rows = _scanResult.Apps.Select(app => new AppRow(app)).Where(row => MatchesFilter(row, query));
        foreach (var row in rows)
        {
            _visibleApps.Add(row);
        }

        SubtitleText.Text = _visibleApps.Count == _scanResult.Apps.Count
            ? AppText.Get("Page.Subtitle.All")
            : AppText.Format("Page.Subtitle.Filtered", _visibleApps.Count, _scanResult.Apps.Count);
    }

    private void ApplyLocalization()
    {
        AppSubtitleText.Text = AppText.Get("App.Subtitle");
        ScanButton.Content = AppText.Get("Action.Scan");
        RefreshButton.Content = AppText.Get("Action.Rebuild");
        ViewsLabelText.Text = AppText.Get("Nav.Views");
        OverviewFilter.Content = AppText.Get("Nav.Overview");
        ReviewFilter.Content = AppText.Get("Nav.Review");
        AllFilter.Content = AppText.Get("Nav.All");
        FolderFilter.Content = AppText.Get("Nav.Folders");
        UnknownFilter.Content = AppText.Get("Nav.Unknown");
        SafetyTitleText.Text = AppText.Get("Safety.Title");
        SafetyBodyText.Text = AppText.Get("Safety.Body");

        PageTitleText.Text = AppText.Get("Page.Title");
        SubtitleText.Text = AppText.Get("Page.Subtitle.Default");
        SearchLabelText.Text = AppText.Get("Search.Label");
        SearchHint.Text = AppText.Get("Search.Hint");
        AppsMetricLabelText.Text = AppText.Get("Metric.Apps");
        ReviewMetricLabelText.Text = AppText.Get("Metric.Review");
        FoldersMetricLabelText.Text = AppText.Get("Metric.Folders");
        DurationMetricLabelText.Text = AppText.Get("Metric.Duration");

        ReviewFocusTitleText.Text = AppText.Get("Overview.ReviewFocus");
        EvidenceHealthTitleText.Text = AppText.Get("Overview.EvidenceHealth");
        WorkflowTitleText.Text = AppText.Get("Overview.Workflow");
        WorkflowStep1Text.Text = AppText.Get("Overview.Workflow.1");
        WorkflowStep2Text.Text = AppText.Get("Overview.Workflow.2");
        WorkflowStep3Text.Text = AppText.Get("Overview.Workflow.3");
        SafeBoundaryTitleText.Text = AppText.Get("Overview.SafeBoundary");
        SafeBoundaryBodyText.Text = AppText.Get("Overview.SafeBoundary.Body");

        NameColumn.Header = AppText.Get("Column.Name");
        PublisherColumn.Header = AppText.Get("Column.Publisher");
        SourceColumn.Header = AppText.Get("Column.Source");
        EvidenceColumn.Header = AppText.Get("Column.Evidence");
        ReasonColumn.Header = AppText.Get("Column.Reason");

        InspectorTitleText.Text = AppText.Get("Inspector.Title");
        EvidenceSectionTitleText.Text = AppText.Get("Inspector.Evidence");
        LocationSectionTitleText.Text = AppText.Get("Inspector.Location");
        ReasonsSectionTitleText.Text = AppText.Get("Inspector.Reasons");
        ExecutablesSectionTitleText.Text = AppText.Get("Inspector.Executables");
        ActionsSectionTitleText.Text = AppText.Get("Inspector.Actions");
        OpenFolderButton.Content = AppText.Get("Action.OpenFolder");
        SelectExeButton.Content = AppText.Get("Action.LocateExecutable");
        UninstallButton.Content = AppText.Get("Action.RunUninstall");
        StatusText.Text = AppText.Get("Status.Ready");
    }

    private void UpdateOverview()
    {
        if (_scanResult is null)
        {
            ReviewFocusText.Text = AppText.Get("Overview.ReviewFocus.Empty");
            EvidenceHealthText.Text = AppText.Get("Overview.EvidenceHealth.Empty");
            TopReasonsList.ItemsSource = null;
            return;
        }

        int folderCount = _scanResult.Apps.Count(app => app.Source == AppSource.Folder);
        int unknownEvidenceCount = _scanResult.Apps.Count(app => app.LastRunEvidence.Confidence == EvidenceConfidence.Unknown);
        int lowConfidenceCount = _scanResult.Apps.Count(app => app.LastRunEvidence.Confidence == EvidenceConfidence.Low);
        int mediumConfidenceCount = _scanResult.Apps.Count(app => app.LastRunEvidence.Confidence == EvidenceConfidence.Medium);

        ReviewFocusText.Text = _scanResult.ReviewCount == 0
            ? AppText.Get("Overview.ReviewFocus.None")
            : AppText.Format("Overview.ReviewFocus.Some", _scanResult.ReviewCount);

        EvidenceHealthText.Text = AppText.Format("Overview.EvidenceHealth.Value", mediumConfidenceCount, lowConfidenceCount, unknownEvidenceCount);

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
            DetailNameText.Text = AppText.Get("Inspector.EmptyName");
            DetailMetaText.Text = AppText.Get("Inspector.EmptyMeta");
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
        LocationText.Text = app.InstallLocation ?? AppText.Get("Inspector.NoLocation");
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
            AppText.Format("Dialog.Uninstall.Body", command),
            AppText.Get("Dialog.Uninstall.Title"),
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
            EvidenceConfidence.Low => AppText.Get("Confidence.Low"),
            EvidenceConfidence.Medium => AppText.Get("Confidence.Medium"),
            EvidenceConfidence.High => AppText.Get("Confidence.High"),
            _ => AppText.Get("Confidence.Unknown")
        };
    }

    private sealed class AppRow(AppEntry entry)
    {
        public AppEntry Entry { get; } = entry;
        public string Name => Entry.Name;
        public string Publisher => string.IsNullOrWhiteSpace(Entry.Publisher) ? AppText.Get("Publisher.Unknown") : Entry.Publisher;
        public string SourceLabel => Entry.Source switch
        {
            AppSource.Folder => AppText.Get("Source.Folder"),
            AppSource.Shortcut => AppText.Get("Source.Shortcut"),
            _ => AppText.Get("Source.UninstallList")
        };
        public string LastRunLabel => Entry.LastRunEvidence.TimestampUtc.HasValue
            ? Entry.LastRunEvidence.TimestampUtc.Value.LocalDateTime.ToString("yyyy-MM-dd")
            : AppText.Get("Evidence.Unknown");
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
        if (reason == "Not in uninstall list") return AppText.Get("Reason.NotInUninstallList");
        if (reason == "Potential uninstall entry residue") return AppText.Get("Reason.UninstallResidue");
        if (reason == "Unknown publisher") return AppText.Get("Reason.UnknownPublisher");
        if (reason == "Installed under user profile") return AppText.Get("Reason.UserProfile");
        if (reason == "Last-run evidence unavailable") return AppText.Get("Reason.NoLastRun");
        if (reason == "Recent or clearly registered") return AppText.Get("Reason.Clear");
        if (reason.StartsWith("Not run for ", StringComparison.Ordinal))
        {
            var days = reason.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(3);
            return AppText.Format("Reason.Stale", days ?? "?");
        }

        return reason;
    }
}
