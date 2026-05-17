using System.Diagnostics;
using Microsoft.Win32;

namespace PcView.Core;

public sealed class PcViewScanner
{
    private readonly RecommendationEngine _recommendations = new();

    public string DataRoot { get; }
    public string CachePath => Path.Combine(DataRoot, "cache.json");

    public PcViewScanner(string? dataRoot = null)
    {
        DataRoot = dataRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PcView");
    }

    public Task<ScanResult> ScanAsync(ScanOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Scan(options ?? new ScanOptions(), cancellationToken), cancellationToken);
    }

    private ScanResult Scan(ScanOptions options, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cache = DirectoryInventoryCache.Load(CachePath);
        var prefetch = ReadPrefetchEvidence();
        var registryApps = AppEntryMerger.MergeRegistryDuplicates(ReadRegistryApps());
        var shortcuts = ReadStartMenuShortcuts();
        var apps = MergeApps(registryApps, shortcuts, cache, prefetch, options, cancellationToken);
        cache.Save(CachePath);

        return new ScanResult(DateTimeOffset.UtcNow, stopwatch.Elapsed, CachePath, apps);
    }

    private IReadOnlyList<AppEntry> MergeApps(
        IReadOnlyList<AppEntry> registryApps,
        IReadOnlyList<ShortcutEntry> shortcuts,
        DirectoryInventoryCache cache,
        IReadOnlyDictionary<string, DateTimeOffset> prefetch,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        var result = new List<AppEntry>();
        var knownDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var app in registryApps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var executables = InventoryDirectory(app.InstallLocation, cache, prefetch, options, cancellationToken);
            var residue = IsPotentialUninstallEntryResidue(app, executables);
            if (!string.IsNullOrWhiteSpace(app.InstallLocation))
            {
                knownDirectories.Add(app.InstallLocation);
            }

            result.Add(FinalizeApp(app with
            {
                Executables = executables,
                PrimaryExecutable = executables.FirstOrDefault()?.Path,
                LastRunEvidence = BestEvidence(executables),
                LastRunDays = DaysSince(BestEvidence(executables).TimestampUtc),
                IsPotentialUninstallEntryResidue = residue
            }));
        }

        foreach (var directory in CandidateDirectories(registryApps, shortcuts, options.ExtraRoots))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (knownDirectories.Contains(directory))
            {
                continue;
            }

            var executables = InventoryDirectory(directory, cache, prefetch, options with { ExecutableLimitPerDirectory = 40 }, cancellationToken);
            if (executables.Count == 0)
            {
                continue;
            }

            var evidence = BestEvidence(executables);
            result.Add(FinalizeApp(new AppEntry
            {
                Id = PathHelpers.StableId(directory),
                Name = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                InstallLocation = directory,
                PrimaryExecutable = executables.FirstOrDefault()?.Path,
                Source = AppSource.Folder,
                Executables = executables,
                LastRunEvidence = evidence,
                LastRunDays = DaysSince(evidence.TimestampUtc)
            }));
        }

        return result
            .OrderBy(app => app.Recommendation.Level == RecommendationLevel.Review ? 0 : 1)
            .ThenBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private AppEntry FinalizeApp(AppEntry app)
    {
        return app with { Recommendation = _recommendations.Evaluate(app) };
    }

    private static bool IsPotentialUninstallEntryResidue(AppEntry app, IReadOnlyList<ExecutableEntry> executables)
    {
        if (app.Source != AppSource.Registry)
        {
            return false;
        }

        bool hasMissingInstallLocation = !string.IsNullOrWhiteSpace(app.InstallLocation)
            && !SafeDirectoryExists(app.InstallLocation);
        bool hasMissingDisplayIcon = !string.IsNullOrWhiteSpace(app.DisplayIconPath)
            && !File.Exists(app.DisplayIconPath);
        bool hasMissingUninstallTarget = !string.IsNullOrWhiteSpace(app.UninstallCommandPath)
            && !File.Exists(app.UninstallCommandPath);

        return executables.Count == 0
            && (hasMissingInstallLocation || hasMissingDisplayIcon || hasMissingUninstallTarget);
    }

    private IReadOnlyList<ExecutableEntry> InventoryDirectory(
        string? directory,
        DirectoryInventoryCache cache,
        IReadOnlyDictionary<string, DateTimeOffset> prefetch,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return [];
        }

        var fingerprint = DirectoryFingerprint(directory);
        if (!options.Refresh && cache.TryGet(directory, fingerprint, out var cached))
        {
            return cached;
        }

        var executables = new List<ExecutableEntry>();
        try
        {
            foreach (var file in SafeEnumerateFiles(directory, "*.exe", recursive: true).Take(options.ExecutableLimitPerDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    var evidence = EvidenceForExecutable(info, prefetch);
                    executables.Add(new ExecutableEntry(
                        info.Name,
                        info.FullName,
                        info.Length,
                        info.LastWriteTimeUtc,
                        info.LastAccessTimeUtc.Year > 2000 ? info.LastAccessTimeUtc : null,
                        evidence));
                }
                catch
                {
                    continue;
                }
            }
        }
        catch
        {
            return [];
        }

        cache.Put(directory, fingerprint, executables);
        return executables;
    }

    private static EvidenceRecord EvidenceForExecutable(FileInfo info, IReadOnlyDictionary<string, DateTimeOffset> prefetch)
    {
        if (prefetch.TryGetValue(info.Name.ToLowerInvariant(), out var prefetchTime))
        {
            return new EvidenceRecord(prefetchTime, "prefetch", EvidenceConfidence.Medium);
        }

        if (info.LastAccessTimeUtc.Year > 2000)
        {
            return new EvidenceRecord(info.LastAccessTimeUtc, "file-access", EvidenceConfidence.Low);
        }

        return EvidenceRecord.Unknown;
    }

    private static EvidenceRecord BestEvidence(IEnumerable<ExecutableEntry> executables)
    {
        return executables
            .Select(item => item.LastRunEvidence)
            .Where(item => item.TimestampUtc.HasValue)
            .OrderByDescending(item => item.TimestampUtc)
            .FirstOrDefault() ?? EvidenceRecord.Unknown;
    }

    private static int? DaysSince(DateTimeOffset? timestampUtc)
    {
        return timestampUtc.HasValue ? (int)(DateTimeOffset.UtcNow - timestampUtc.Value).TotalDays : null;
    }

    private static string DirectoryFingerprint(string directory)
    {
        var info = new DirectoryInfo(directory);
        return $"{info.LastWriteTimeUtc.Ticks}|{info.CreationTimeUtc.Ticks}";
    }

    private static IReadOnlyDictionary<string, DateTimeOffset> ReadPrefetchEvidence()
    {
        var prefetchRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
        var map = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(prefetchRoot))
        {
            return map;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(prefetchRoot, "*.pf").ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            return map;
        }
        catch (IOException)
        {
            return map;
        }

        foreach (var file in files)
        {
            try
            {
                var exe = PrefetchEvidence.TryGetExecutableName(file);
                if (exe is null)
                {
                    continue;
                }

                var time = File.GetLastWriteTimeUtc(file);
                if (!map.TryGetValue(exe, out var existing) || time > existing)
                {
                    map[exe] = time;
                }
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
        }

        return map;
    }

    private static IReadOnlyList<AppEntry> ReadRegistryApps()
    {
        var roots = new[]
        {
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
            (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
        };

        var apps = new List<AppEntry>();
        foreach (var (hive, path) in roots)
        {
            using var root = OpenRegistryKey(hive, path);
            if (root is null)
            {
                continue;
            }

            foreach (var subkeyName in GetRegistrySubKeyNames(root))
            {
                using var key = root.OpenSubKey(subkeyName);
                if (key is null)
                {
                    continue;
                }

                var displayName = key.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var displayIconPath = PathHelpers.ExtractExecutablePath(key.GetValue("DisplayIcon") as string);
                var uninstallCommand = key.GetValue("UninstallString") as string;
                var uninstallCommandPath = PathHelpers.ExtractExecutablePath(uninstallCommand);
                var registryKeyPath = $"{hive.Name}\\{path}\\{subkeyName}";
                var installLocation = Expand(key.GetValue("InstallLocation") as string)?.Trim('"');
                if (string.IsNullOrWhiteSpace(installLocation))
                {
                    if (!string.IsNullOrWhiteSpace(displayIconPath) && File.Exists(displayIconPath))
                    {
                        installLocation = Path.GetDirectoryName(displayIconPath);
                    }
                }

                apps.Add(new AppEntry
                {
                    Id = PathHelpers.StableId(registryKeyPath),
                    Name = displayName,
                    Publisher = key.GetValue("Publisher") as string ?? "",
                    Version = key.GetValue("DisplayVersion") as string ?? "",
                    InstallDate = key.GetValue("InstallDate") as string ?? "",
                    InstallLocation = installLocation,
                    UninstallCommand = uninstallCommand,
                    RegistryKeyPath = registryKeyPath,
                    DisplayIconPath = displayIconPath,
                    UninstallCommandPath = uninstallCommandPath,
                    Source = AppSource.Registry
                });
            }
        }

        return apps;
    }

    private static IReadOnlyList<ShortcutEntry> ReadStartMenuShortcuts()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        }.Where(path => !string.IsNullOrWhiteSpace(path) && SafeDirectoryExists(path)).Distinct(StringComparer.OrdinalIgnoreCase);

        var shortcuts = new List<ShortcutEntry>();
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return shortcuts;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        foreach (var root in roots)
        {
            foreach (var lnk in SafeEnumerateFiles(root, "*.lnk", recursive: true))
            {
                try
                {
                    dynamic shortcut = shell.CreateShortcut(lnk);
                    string target = shortcut.TargetPath;
                    if (target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(target))
                    {
                        shortcuts.Add(new ShortcutEntry(Path.GetFileNameWithoutExtension(lnk), lnk, target));
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        return shortcuts;
    }

    private static IEnumerable<string> CandidateDirectories(
        IEnumerable<AppEntry> registryApps,
        IEnumerable<ShortcutEntry> shortcuts,
        IEnumerable<string> extraRoots)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in registryApps)
        {
            if (!string.IsNullOrWhiteSpace(app.InstallLocation) && SafeDirectoryExists(app.InstallLocation))
            {
                directories.Add(app.InstallLocation);
            }
        }

        foreach (var shortcut in shortcuts)
        {
            var directory = Path.GetDirectoryName(shortcut.TargetPath);
            if (!string.IsNullOrWhiteSpace(directory) && SafeDirectoryExists(directory))
            {
                directories.Add(directory);
            }
        }

        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")
        }.Concat(extraRoots);

        foreach (var root in roots.Where(path => !string.IsNullOrWhiteSpace(path) && SafeDirectoryExists(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var directory in SafeEnumerateDirectories(root))
            {
                directories.Add(directory);
            }
        }

        return directories;
    }

    private static string? Expand(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Environment.ExpandEnvironmentVariables(value);
    }

    private static RegistryKey? OpenRegistryKey(RegistryKey hive, string path)
    {
        try
        {
            return hive.OpenSubKey(path);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> GetRegistrySubKeyNames(RegistryKey root)
    {
        try
        {
            return root.GetSubKeyNames();
        }
        catch
        {
            return [];
        }
    }

    private static bool SafeDirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path, "*", SafeEnumerationOptions()).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path, string pattern, bool recursive)
    {
        try
        {
            var options = SafeEnumerationOptions();
            options.RecurseSubdirectories = recursive;
            return Directory.EnumerateFiles(path, pattern, options).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static EnumerationOptions SafeEnumerationOptions()
    {
        return new EnumerationOptions
        {
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System | FileAttributes.Temporary,
            ReturnSpecialDirectories = false
        };
    }

    private sealed record ShortcutEntry(string Name, string ShortcutPath, string TargetPath);
}
