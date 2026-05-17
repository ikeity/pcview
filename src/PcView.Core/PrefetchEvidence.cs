namespace PcView.Core;

internal static class PrefetchEvidence
{
    public static string? TryGetExecutableName(string prefetchFileName)
    {
        var name = Path.GetFileNameWithoutExtension(prefetchFileName);
        var dash = name.LastIndexOf('-');
        if (dash <= 0)
        {
            return null;
        }

        var executable = name[..dash].ToLowerInvariant();
        return executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? executable
            : executable + ".exe";
    }
}
