namespace PcView.Core.Tests;

[TestClass]
public sealed class PathHelpersTests
{
    [TestMethod]
    public void ExtractExecutablePath_ReturnsQuotedExecutable()
    {
        var path = PathHelpers.ExtractExecutablePath("\"C:\\Program Files\\Demo\\demo.exe\" --flag");

        Assert.AreEqual("C:\\Program Files\\Demo\\demo.exe", path);
    }

    [TestMethod]
    public void ExtractExecutablePath_ReturnsExecutableBeforeArguments()
    {
        var path = PathHelpers.ExtractExecutablePath("C:\\Tools\\Demo\\demo.exe /uninstall");

        Assert.AreEqual("C:\\Tools\\Demo\\demo.exe", path);
    }

    [TestMethod]
    public void StableId_IsStableForSameInput()
    {
        Assert.AreEqual(PathHelpers.StableId("abc"), PathHelpers.StableId("abc"));
    }
}
