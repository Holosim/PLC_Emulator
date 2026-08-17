using PlcEmulator.Config;
using PlcEmulator.Core;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies NFR-503 (docs/RTVM.md TP-503): the server keeps no runtime
/// tag/controller state outside a single <see cref="PlcController"/>
/// instance's own memory, so a process restart — modeled here as
/// discarding one <see cref="PlcController"/> and constructing a new one
/// from the same CONTROL_LOGIC/NETWORK definitions, exactly what
/// <c>PlcEmulator.Host.Program.Main</c> does on every launch — always
/// comes back up with tags at their CONTROL_LOGIC-defined initial
/// values, never a prior run's mutated values.
/// </summary>
/// <remarks>
/// This is a verification pass, not new functional code (see issue #26):
/// <c>Program.Main</c> already calls <c>ConfigLoader.Load*</c> and
/// constructs a fresh <see cref="PlcController"/> on every invocation
/// (no cached/static controller, no file/database write anywhere in
/// <c>src/</c> — see docs/SDD.md, Data Architecture / "Storage: none, by
/// design"), and <see cref="Core.ControlLogicBuilder.BuildTagTable"/>
/// already builds brand-new <see cref="Tag"/> instances from
/// <see cref="TagDef.InitialValue"/> on every call, never a reference
/// shared back into a previous run's <see cref="TagTable"/>. This test
/// class is the concrete unit-test artifact TP-503 calls for, proving
/// that conclusion by construction: it reuses one
/// <see cref="ControlLogicDef"/> instance across two separately
/// constructed controllers (the strongest form of the check — any
/// accidental caching keyed off the shared config object would leak the
/// mutated value across the "restart" boundary here even though two
/// independently-parsed config objects would not expose it).
/// </remarks>
[TestClass]
public sealed class RestartStatePersistenceTests
{
    /// <summary>
    /// TP-503: <c>Start_PB</c> is set true via a queued write (OUT-401,
    /// the TP-401 mechanism the requirement text references) and scanned
    /// on a first controller instance, simulating a live run. A second
    /// controller built afterward from the same CONTROL_LOGIC definition
    /// — simulating a restart with the same files, since neither
    /// <c>Program.Main</c> nor anything it calls persists state between
    /// runs — must come up with <c>Start_PB</c> back at its
    /// CONTROL_LOGIC-declared initial value (<see langword="false"/>),
    /// not the prior instance's mutated value (<see langword="true"/>).
    /// </summary>
    [TestMethod]
    public void SecondController_FromSameControlLogic_StartsAtInitialValue_NotPriorRunsMutatedValue()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = "Start_PB", Type = TagTypeDef.Bool, InitialValue = false } },
            Rungs = Array.Empty<RungDef>(),
        };
        var network = new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() };

        // "Run 1": construct a controller, queue and apply a write that
        // sets Start_PB true (the TP-401 tag_write path), and confirm it
        // actually took effect on this instance.
        var firstRun = new PlcController(controlLogic, network,
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        firstRun.QueueWrite("Start_PB", true);
        firstRun.RunScan();

        Assert.AreEqual(true, firstRun.GetSnapshot().Values["Start_PB"], "Sanity check: the queued write must have applied to the first run's own tag table.");

        // "Restart with the same CONTROL_LOGIC/NETWORK files": build a
        // brand-new controller from the very same ControlLogicDef/NetworkDef
        // objects, exactly as Program.Main does on every launch. Nothing
        // about firstRun is passed to this constructor.
        var secondRun = new PlcController(controlLogic, network,
            static driverType => throw new InvalidOperationException($"no NETWORK components in this test; unexpected driver type '{driverType}'"));

        Assert.AreEqual(
            false,
            secondRun.GetSnapshot().Values["Start_PB"],
            "Start_PB must reset to CONTROL_LOGIC's declared initial value on restart, not carry over the prior run's mutated value.");
    }
}
