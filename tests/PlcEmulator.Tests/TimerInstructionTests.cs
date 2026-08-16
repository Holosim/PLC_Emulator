using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies CORE-203 (<c>TON</c>) and CORE-204 (<c>TOF</c>) — docs/RTVM.md
/// TP-203/TP-204. Drives <see cref="Ton"/>/<see cref="Tof"/> directly with
/// controlled <see cref="TimeSpan"/> values (rather than real
/// <see cref="Thread.Sleep"/> delays) so accumulation math is exact and
/// the suite stays fast/non-flaky; <see cref="ScanEngine"/>'s own
/// wall-clock measurement is covered separately, once, in
/// <see cref="ScanEngine_MeasuresRealElapsedTime_BetweenCalls"/>.
/// </summary>
[TestClass]
public sealed class TimerInstructionTests
{
    /// <summary>Builds a TagTable with a single TIMER tag at the given preset (ms), via the public DATA-IN-100 path.</summary>
    private static TagTable BuildTimerTagTable(string name, int presetMs)
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = name, Type = TagTypeDef.Timer, Preset = presetMs } },
            Rungs = Array.Empty<RungDef>(),
        };

        return ControlLogicBuilder.BuildTagTable(controlLogic);
    }

    // --- TON (CORE-203 / TP-203: PRE=2000ms) ---

    [TestMethod]
    public void Ton_EnabledAtT0_AccZeroAndNotDone()
    {
        var tags = BuildTimerTagTable("T1", 2000);
        var ton = new Ton("T1");

        // First scan enabling the timer: no prior scan to measure elapsed against.
        ton.Evaluate(tags, true, TimeSpan.Zero);

        var timer = tags.Get("T1").Timer!;
        Assert.AreEqual(0, timer.Acc);
        Assert.IsFalse(timer.Dn);
        Assert.IsTrue(timer.En);
    }

    [TestMethod]
    public void Ton_EnabledFor1000ms_AccApprox1000AndNotYetDone()
    {
        var tags = BuildTimerTagTable("T1", 2000);
        var ton = new Ton("T1");

        ton.Evaluate(tags, true, TimeSpan.Zero);
        ton.Evaluate(tags, true, TimeSpan.FromMilliseconds(1000));

        var timer = tags.Get("T1").Timer!;
        Assert.AreEqual(1000, timer.Acc);
        Assert.IsFalse(timer.Dn, ".DN must stay false while .ACC < .PRE");
    }

    [TestMethod]
    public void Ton_EnabledPastPreset_DoneBecomesTrue()
    {
        var tags = BuildTimerTagTable("T1", 2000);
        var ton = new Ton("T1");

        ton.Evaluate(tags, true, TimeSpan.Zero);
        ton.Evaluate(tags, true, TimeSpan.FromMilliseconds(1000));
        ton.Evaluate(tags, true, TimeSpan.FromMilliseconds(1100)); // total elapsed 2100ms >= 2000ms preset

        var timer = tags.Get("T1").Timer!;
        Assert.AreEqual(2100, timer.Acc);
        Assert.IsTrue(timer.Dn, ".DN must become true once .ACC >= .PRE");
    }

    [TestMethod]
    public void Ton_Disabled_ResetsAccAndDone()
    {
        var tags = BuildTimerTagTable("T1", 2000);
        var ton = new Ton("T1");

        ton.Evaluate(tags, true, TimeSpan.Zero);
        ton.Evaluate(tags, true, TimeSpan.FromMilliseconds(2100));
        ton.Evaluate(tags, false, TimeSpan.FromMilliseconds(50)); // disable

        var timer = tags.Get("T1").Timer!;
        Assert.AreEqual(0, timer.Acc, "disabling TON must reset .ACC to 0");
        Assert.IsFalse(timer.Dn, "disabling TON must reset .DN to false");
        Assert.IsFalse(timer.En);
    }

    [TestMethod]
    public void Ton_ReturnsRungStateUnchanged_ForPowerFlowContinuation()
    {
        var tags = BuildTimerTagTable("T1", 2000);
        var ton = new Ton("T1");

        Assert.IsTrue(ton.Evaluate(tags, true, TimeSpan.Zero));
        Assert.IsFalse(ton.Evaluate(tags, false, TimeSpan.Zero));
    }

    // --- TOF (CORE-204 / TP-204: PRE=1000ms) ---

    [TestMethod]
    public void Tof_Enabled_DoneImmediatelyTrue()
    {
        var tags = BuildTimerTagTable("T2", 1000);
        var tof = new Tof("T2");

        tof.Evaluate(tags, true, TimeSpan.Zero);

        var timer = tags.Get("T2").Timer!;
        Assert.IsTrue(timer.Dn, ".DN must be true immediately while TOF is enabled");
        Assert.AreEqual(0, timer.Acc);
        Assert.IsTrue(timer.En);
    }

    [TestMethod]
    public void Tof_DisabledFor500ms_DoneStillTrue()
    {
        var tags = BuildTimerTagTable("T2", 1000);
        var tof = new Tof("T2");

        tof.Evaluate(tags, true, TimeSpan.Zero); // enable, observe .DN=true
        tof.Evaluate(tags, false, TimeSpan.Zero); // disable at t=0
        tof.Evaluate(tags, false, TimeSpan.FromMilliseconds(500)); // sample at t=500ms after disable

        var timer = tags.Get("T2").Timer!;
        Assert.AreEqual(500, timer.Acc);
        Assert.IsTrue(timer.Dn, ".DN must remain true until .PRE has elapsed since disable");
        Assert.IsFalse(timer.En);
    }

    [TestMethod]
    public void Tof_DisabledPastPreset_DoneBecomesFalse()
    {
        var tags = BuildTimerTagTable("T2", 1000);
        var tof = new Tof("T2");

        tof.Evaluate(tags, true, TimeSpan.Zero);
        tof.Evaluate(tags, false, TimeSpan.Zero);
        tof.Evaluate(tags, false, TimeSpan.FromMilliseconds(500));
        tof.Evaluate(tags, false, TimeSpan.FromMilliseconds(600)); // total elapsed since disable: 1100ms >= 1000ms preset

        var timer = tags.Get("T2").Timer!;
        Assert.AreEqual(1100, timer.Acc);
        Assert.IsFalse(timer.Dn, ".DN must go false once .PRE has elapsed since disable");
    }

    [TestMethod]
    public void Tof_ReEnabledAfterDisable_ResetsAccAndDoneImmediatelyTrue()
    {
        var tags = BuildTimerTagTable("T2", 1000);
        var tof = new Tof("T2");

        tof.Evaluate(tags, true, TimeSpan.Zero);
        tof.Evaluate(tags, false, TimeSpan.Zero);
        tof.Evaluate(tags, false, TimeSpan.FromMilliseconds(1100)); // .DN now false
        tof.Evaluate(tags, true, TimeSpan.FromMilliseconds(50)); // re-enable

        var timer = tags.Get("T2").Timer!;
        Assert.AreEqual(0, timer.Acc);
        Assert.IsTrue(timer.Dn, "re-enabling TOF must make .DN true immediately again");
    }

    [TestMethod]
    public void Tof_ReturnsRungStateUnchanged_ForPowerFlowContinuation()
    {
        var tags = BuildTimerTagTable("T2", 1000);
        var tof = new Tof("T2");

        Assert.IsTrue(tof.Evaluate(tags, true, TimeSpan.Zero));
        Assert.IsFalse(tof.Evaluate(tags, false, TimeSpan.Zero));
    }

    // --- Misconfiguration guard (shared RequireTimer helper) ---

    [TestMethod]
    public void Ton_AgainstNonTimerTag_ThrowsDescriptiveError()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = "NotATimer", Type = TagTypeDef.Bool, InitialValue = false } },
            Rungs = Array.Empty<RungDef>(),
        };
        var tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        var ton = new Ton("NotATimer");

        var ex = Assert.ThrowsException<InvalidOperationException>(() => ton.Evaluate(tags, true, TimeSpan.Zero));
        StringAssert.Contains(ex.Message, "NotATimer");
    }

    // --- ScanEngine wall-clock plumbing (CORE-200 <-> CORE-203/204 integration) ---

    /// <summary>
    /// Confirms <see cref="ScanEngine"/> itself measures real elapsed
    /// time between calls (rather than always passing
    /// <see cref="TimeSpan.Zero"/>) and threads it to every instruction
    /// in program order — the plumbing TON/TOF rely on. Uses a small
    /// real sleep with a loose lower-bound assertion to avoid flakiness
    /// from scheduler jitter.
    /// </summary>
    [TestMethod]
    public void ScanEngine_MeasuresRealElapsedTime_BetweenCalls()
    {
        var tags = BuildTimerTagTable("T3", 50);
        var ton = new Ton("T3");
        var rungs = new List<Rung> { new() { Instructions = new IInstruction[] { ton } } };
        var engine = new ScanEngine();

        engine.Evaluate(rungs, tags); // first scan: elapsed must be zero, no prior scan
        Assert.AreEqual(0, tags.Get("T3").Timer!.Acc);

        Thread.Sleep(60);
        engine.Evaluate(rungs, tags); // second scan: elapsed must reflect the real sleep above

        Assert.IsTrue(tags.Get("T3").Timer!.Acc >= 50, $"expected .ACC to reflect >=50ms real elapsed time, was {tags.Get("T3").Timer!.Acc}");
    }
}
