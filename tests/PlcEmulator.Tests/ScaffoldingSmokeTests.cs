using PlcEmulator.Core;

namespace PlcEmulator.Tests;

/// <summary>
/// Confirms the test project itself builds, references every project
/// in the solution, and runs under <c>dotnet test</c> — this issue is
/// scaffolding only, so there is no functional behavior to verify yet.
/// Real test procedures (TP-xxx, per docs/RTVM.md) land alongside each
/// feature issue.
/// </summary>
[TestClass]
public sealed class ScaffoldingSmokeTests
{
    [TestMethod]
    public void TagType_EnumHasThreeMembers()
    {
        var values = Enum.GetValues<TagType>();

        Assert.AreEqual(3, values.Length);
        CollectionAssert.AreEquivalent(
            new[] { TagType.Bool, TagType.Dint, TagType.Real },
            values);
    }
}
