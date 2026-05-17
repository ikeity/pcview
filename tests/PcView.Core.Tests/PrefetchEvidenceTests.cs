namespace PcView.Core.Tests;

[TestClass]
public sealed class PrefetchEvidenceTests
{
    [TestMethod]
    public void TryGetExecutableName_ParsesPrefetchFileName()
    {
        var exe = PrefetchEvidence.TryGetExecutableName("NOTEPAD.EXE-12345678.pf");

        Assert.AreEqual("notepad.exe", exe);
    }

    [TestMethod]
    public void TryGetExecutableName_ReturnsNullForUnexpectedName()
    {
        var exe = PrefetchEvidence.TryGetExecutableName("unexpected.pf");

        Assert.IsNull(exe);
    }
}
