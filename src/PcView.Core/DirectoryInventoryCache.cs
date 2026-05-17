using System.Text.Json;

namespace PcView.Core;

internal sealed record CachedDirectory(
    string Path,
    string Fingerprint,
    DateTimeOffset IndexedUtc,
    IReadOnlyList<ExecutableEntry> Executables);

internal sealed class DirectoryInventoryCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly Dictionary<string, CachedDirectory> _directories = new(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset? LastScanUtc { get; private set; }

    public static DirectoryInventoryCache Load(string path)
    {
        if (!File.Exists(path))
        {
            return new DirectoryInventoryCache();
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<CacheSnapshot>(File.ReadAllText(path), JsonOptions);
            var cache = new DirectoryInventoryCache { LastScanUtc = snapshot?.LastScanUtc };
            foreach (var directory in snapshot?.Directories ?? [])
            {
                cache._directories[directory.Path] = directory;
            }

            return cache;
        }
        catch
        {
            return new DirectoryInventoryCache();
        }
    }

    public bool TryGet(string path, string fingerprint, out IReadOnlyList<ExecutableEntry> executables)
    {
        if (_directories.TryGetValue(path, out var cached) && cached.Fingerprint == fingerprint)
        {
            executables = cached.Executables;
            return true;
        }

        executables = [];
        return false;
    }

    public void Put(string path, string fingerprint, IReadOnlyList<ExecutableEntry> executables)
    {
        _directories[path] = new CachedDirectory(path, fingerprint, DateTimeOffset.UtcNow, executables);
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        LastScanUtc = DateTimeOffset.UtcNow;
        var snapshot = new CacheSnapshot(1, LastScanUtc, _directories.Values.OrderBy(item => item.Path).ToArray());
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private sealed record CacheSnapshot(
        int Version,
        DateTimeOffset? LastScanUtc,
        IReadOnlyList<CachedDirectory> Directories);
}
