using PlcEmulator.Core;

namespace PlcEmulator.Tests;

/// <summary>
/// Confirms the test project itself builds, references every project
/// in the solution, and runs under <c>dotnet test</c>. Originally
/// scaffolding-only (issue #5); <see cref="TagType_EnumHasFiveMembers"/>
/// was updated for DATA-IN-100's structured timer/counter tag types
/// (issue #6) — real test procedures (TP-xxx, per docs/RTVM.md) land
/// alongside each feature issue, e.g. <see cref="ControlLogicSchemaTests"/>.
/// </summary>
[TestClass]
public sealed class ScaffoldingSmokeTests
{
    [TestMethod]
    public void TagType_EnumHasFiveMembers()
    {
        var values = Enum.GetValues<TagType>();

        Assert.AreEqual(5, values.Length);
        CollectionAssert.AreEquivalent(
            new[] { TagType.Bool, TagType.Dint, TagType.Real, TagType.Timer, TagType.Counter },
            values);
    }
}
