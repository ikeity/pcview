namespace PcView.Core;

public sealed class RecommendationEngine
{
    public Recommendation Evaluate(AppEntry app)
    {
        var reasons = new List<string>();
        var level = RecommendationLevel.Keep;

        if (app.LastRunDays is >= 180)
        {
            level = RecommendationLevel.Review;
            reasons.Add($"Not run for {app.LastRunDays.Value} days");
        }

        if (app.Source == AppSource.Folder)
        {
            level = RecommendationLevel.Review;
            reasons.Add("Not in uninstall list");
        }

        if (string.IsNullOrWhiteSpace(app.Publisher))
        {
            reasons.Add("Unknown publisher");
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData)
            && app.InstallLocation is { Length: > 0 }
            && app.InstallLocation.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Installed under user profile");
        }

        if (app.LastRunEvidence.Confidence == EvidenceConfidence.Unknown)
        {
            reasons.Add("Last-run evidence unavailable");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Recent or clearly registered");
        }

        return new Recommendation(level, reasons);
    }
}
