namespace PcView.Core;

public enum AppSource
{
    Registry,
    Shortcut,
    Folder
}

public enum EvidenceConfidence
{
    Unknown,
    Low,
    Medium,
    High
}

public enum RecommendationLevel
{
    Keep,
    Review
}

public sealed record ExecutableEntry(
    string Name,
    string Path,
    long SizeBytes,
    DateTimeOffset LastWriteUtc,
    DateTimeOffset? LastAccessUtc,
    EvidenceRecord LastRunEvidence);

public sealed record EvidenceRecord(
    DateTimeOffset? TimestampUtc,
    string Source,
    EvidenceConfidence Confidence)
{
    public static EvidenceRecord Unknown { get; } = new(null, "unknown", EvidenceConfidence.Unknown);
}

public sealed record Recommendation(
    RecommendationLevel Level,
    IReadOnlyList<string> Reasons);

public sealed record AppEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Publisher { get; init; } = "";
    public string Version { get; init; } = "";
    public string InstallDate { get; init; } = "";
    public string? InstallLocation { get; init; }
    public string? PrimaryExecutable { get; init; }
    public string? UninstallCommand { get; init; }
    public string? RegistryKeyPath { get; init; }
    public string? DisplayIconPath { get; init; }
    public string? UninstallCommandPath { get; init; }
    public bool IsPotentialUninstallEntryResidue { get; init; }
    public AppSource Source { get; init; }
    public IReadOnlyList<ExecutableEntry> Executables { get; init; } = [];
    public EvidenceRecord LastRunEvidence { get; init; } = EvidenceRecord.Unknown;
    public int? LastRunDays { get; init; }
    public Recommendation Recommendation { get; init; } = new(RecommendationLevel.Keep, ["Recent or clearly registered"]);
}

public sealed record ScanResult(
    DateTimeOffset GeneratedUtc,
    TimeSpan Duration,
    string CachePath,
    IReadOnlyList<AppEntry> Apps)
{
    public int AppCount => Apps.Count;
    public int ReviewCount => Apps.Count(app => app.Recommendation.Level == RecommendationLevel.Review);
}

public sealed record ScanOptions
{
    public bool Refresh { get; init; }
    public int ExecutableLimitPerDirectory { get; init; } = 60;
    public IReadOnlyList<string> ExtraRoots { get; init; } = [];
}
