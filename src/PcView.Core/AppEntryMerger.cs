namespace PcView.Core;

internal static class AppEntryMerger
{
    public static IReadOnlyList<AppEntry> MergeRegistryDuplicates(IEnumerable<AppEntry> apps)
    {
        return apps
            .GroupBy(RegistryDuplicateKey, StringComparer.OrdinalIgnoreCase)
            .Select(MergeGroup)
            .ToArray();
    }

    private static string RegistryDuplicateKey(AppEntry app)
    {
        if (app.Source != AppSource.Registry)
        {
            return app.Id;
        }

        var name = Normalize(app.Name);
        var publisher = Normalize(app.Publisher);
        var location = NormalizePath(app.InstallLocation);

        if (string.IsNullOrWhiteSpace(location))
        {
            return $"registry:{name}|{publisher}|{app.Id}";
        }

        return $"registry:{name}|{publisher}|{location}";
    }

    private static AppEntry MergeGroup(IGrouping<string, AppEntry> group)
    {
        var entries = group.ToArray();
        if (entries.Length == 1)
        {
            return entries[0];
        }

        var best = entries
            .OrderByDescending(Score)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .First();

        var uninstallCommand = entries.Select(entry => entry.UninstallCommand).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return best with
        {
            Id = PathHelpers.StableId(group.Key),
            UninstallCommand = uninstallCommand,
            Version = FirstNonEmpty(entries.Select(entry => entry.Version)),
            InstallDate = FirstNonEmpty(entries.Select(entry => entry.InstallDate)),
            Publisher = FirstNonEmpty(entries.Select(entry => entry.Publisher)),
            InstallLocation = FirstNonEmpty(entries.Select(entry => entry.InstallLocation)),
            RegistryKeyPath = FirstNonEmpty(entries.Select(entry => entry.RegistryKeyPath)),
            DisplayIconPath = FirstNonEmpty(entries.Select(entry => entry.DisplayIconPath)),
            UninstallCommandPath = FirstNonEmpty(entries.Select(entry => entry.UninstallCommandPath)),
            IsPotentialUninstallEntryResidue = entries.Any(entry => entry.IsPotentialUninstallEntryResidue)
        };
    }

    private static int Score(AppEntry entry)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(entry.Publisher)) score += 4;
        if (!string.IsNullOrWhiteSpace(entry.Version)) score += 2;
        if (!string.IsNullOrWhiteSpace(entry.InstallLocation)) score += 3;
        if (!string.IsNullOrWhiteSpace(entry.UninstallCommand)) score += 2;
        if (!string.IsNullOrWhiteSpace(entry.InstallDate)) score += 1;
        return score;
    }

    private static string FirstNonEmpty(IEnumerable<string?> values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string Normalize(string? value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    private static string NormalizePath(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
    }
}
