namespace PcView.Core.Tests;

[TestClass]
public sealed class DirectoryInventoryCacheTests
{
    [TestMethod]
    public void SaveAndLoad_RetainsDirectoryInventory()
    {
        var root = Path.Combine(Path.GetTempPath(), "PcView.Tests", Guid.NewGuid().ToString("N"));
        var cachePath = Path.Combine(root, "cache.json");

        try
        {
            var cache = new DirectoryInventoryCache();
            var exe = new ExecutableEntry(
                "demo.exe",
                "C:\\Demo\\demo.exe",
                128,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                new EvidenceRecord(DateTimeOffset.UtcNow, "prefetch", EvidenceConfidence.Medium));

            cache.Put("C:\\Demo", "fingerprint", [exe]);
            cache.Save(cachePath);

            var loaded = DirectoryInventoryCache.Load(cachePath);

            Assert.IsTrue(loaded.TryGet("C:\\Demo", "fingerprint", out var executables));
            Assert.AreEqual(1, executables.Count);
            Assert.AreEqual("demo.exe", executables[0].Name);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
