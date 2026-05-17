namespace PcView.Core.Tests;

[TestClass]
public sealed class RecommendationEngineTests
{
    private readonly RecommendationEngine _engine = new();

    [TestMethod]
    public void Evaluate_MarksFolderDiscoveryForReview()
    {
        var recommendation = _engine.Evaluate(new AppEntry
        {
            Id = "folder",
            Name = "Portable Tool",
            Source = AppSource.Folder,
            InstallLocation = "C:\\Tools\\PortableTool"
        });

        Assert.AreEqual(RecommendationLevel.Review, recommendation.Level);
        CollectionAssert.Contains(recommendation.Reasons.ToList(), "Not in uninstall list");
    }

    [TestMethod]
    public void Evaluate_MarksStaleAppForReview()
    {
        var recommendation = _engine.Evaluate(new AppEntry
        {
            Id = "stale",
            Name = "Old Tool",
            Publisher = "Vendor",
            Source = AppSource.Registry,
            LastRunDays = 220,
            LastRunEvidence = new EvidenceRecord(DateTimeOffset.UtcNow.AddDays(-220), "prefetch", EvidenceConfidence.Medium)
        });

        Assert.AreEqual(RecommendationLevel.Review, recommendation.Level);
        Assert.IsTrue(recommendation.Reasons.Any(reason => reason.StartsWith("Not run for 220 days", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Evaluate_KeepsRecentRegisteredApp()
    {
        var recommendation = _engine.Evaluate(new AppEntry
        {
            Id = "known",
            Name = "Known Tool",
            Publisher = "Known Publisher",
            Source = AppSource.Registry,
            LastRunDays = 2,
            LastRunEvidence = new EvidenceRecord(DateTimeOffset.UtcNow.AddDays(-2), "prefetch", EvidenceConfidence.Medium)
        });

        Assert.AreEqual(RecommendationLevel.Keep, recommendation.Level);
        CollectionAssert.Contains(recommendation.Reasons.ToList(), "Recent or clearly registered");
    }
}
