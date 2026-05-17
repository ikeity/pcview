namespace PcView.Core.Tests;

[TestClass]
public sealed class AppEntryMergerTests
{
    [TestMethod]
    public void MergeRegistryDuplicates_CollapsesSameNamePublisherAndLocation()
    {
        var apps = AppEntryMerger.MergeRegistryDuplicates([
            new AppEntry
            {
                Id = "a",
                Name = "Xshell 8",
                Publisher = "NetSarang Computer",
                InstallLocation = "C:\\Program Files\\NetSarang\\Xshell 8",
                Version = "8.0",
                Source = AppSource.Registry
            },
            new AppEntry
            {
                Id = "b",
                Name = "Xshell 8",
                Publisher = "NetSarang Computer",
                InstallLocation = "C:\\Program Files\\NetSarang\\Xshell 8\\",
                UninstallCommand = "uninstall.exe",
                Source = AppSource.Registry
            }
        ]);

        Assert.AreEqual(1, apps.Count);
        Assert.AreEqual("Xshell 8", apps[0].Name);
        Assert.AreEqual("uninstall.exe", apps[0].UninstallCommand);
    }

    [TestMethod]
    public void MergeRegistryDuplicates_DoesNotCollapseDifferentVersionsByNameOnlyWhenLocationMissing()
    {
        var apps = AppEntryMerger.MergeRegistryDuplicates([
            new AppEntry { Id = "a", Name = "Xshell 7", Publisher = "NetSarang Computer", Source = AppSource.Registry },
            new AppEntry { Id = "b", Name = "Xshell 7", Publisher = "NetSarang Computer", Source = AppSource.Registry }
        ]);

        Assert.AreEqual(2, apps.Count);
    }
}
