using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Tests;

/// <summary>
/// Verifies CORE-205/206 (docs/RTVM.md TP-205/TP-206): <c>CTU</c>
/// counts up, <c>CTD</c> counts down, both only on a rising edge of
/// their enable input (not merely "while true"), and <c>RES</c>
/// resets a counter's <c>.ACC</c>/<c>.DN</c> unconditionally whenever
/// its own rung is true.
/// </summary>
[TestClass]
public sealed class CounterInstructionTests
{
    /// <summary>Builds a TagTable with a single COUNTER tag at the given preset, via the public DATA-IN-100 path (ControlLogicBuilder).</summary>
    private static TagTable BuildCounterTagTable(string name, int preset)
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = name, Type = TagTypeDef.Counter, Preset = preset } },
            Rungs = Array.Empty<RungDef>(),
        };

        return ControlLogicBuilder.BuildTagTable(controlLogic);
    }

    // ---- CTU (TP-205) ----------------------------------------------

    /// <summary>TP-205: CTU with .PRE=3. After 3 rising edges: .ACC=3, .DN=true. After a 4th edge: .ACC=4, .DN remains true.</summary>
    [TestMethod]
    public void Ctu_ThreeRisingEdges_AccReachesPresetAndDnGoesTrue()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var ctu = new Ctu("Ctr");

        // Edge 1: false -> true.
        ctu.Evaluate(tags, false, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);
        // Edge 2.
        ctu.Evaluate(tags, false, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);
        // Edge 3.
        ctu.Evaluate(tags, false, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);

        var counter = tags.Get("Ctr").Counter!;
        Assert.AreEqual(3, counter.Acc);
        Assert.IsTrue(counter.Dn);

        // 4th edge: keeps counting past preset, DN remains true.
        ctu.Evaluate(tags, false, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);

        Assert.AreEqual(4, counter.Acc);
        Assert.IsTrue(counter.Dn);
    }

    /// <summary>Holding the enable input true across multiple scans counts only once — CTU is edge-triggered, not level-triggered.</summary>
    [TestMethod]
    public void Ctu_EnableHeldTrueAcrossScans_CountsOnlyOnce()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var ctu = new Ctu("Ctr");

        ctu.Evaluate(tags, true, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);

        Assert.AreEqual(1, tags.Get("Ctr").Counter!.Acc);
    }

    /// <summary>Disabling and re-enabling CTU produces a fresh rising edge each time.</summary>
    [TestMethod]
    public void Ctu_DisableThenReEnable_CountsAgain()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var ctu = new Ctu("Ctr");

        ctu.Evaluate(tags, true, TimeSpan.Zero);
        ctu.Evaluate(tags, false, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);

        Assert.AreEqual(2, tags.Get("Ctr").Counter!.Acc);
    }

    /// <summary>CTU returns rungState unchanged (action-type pass-through), so power flow continues correctly to later instructions on the rung.</summary>
    [TestMethod]
    public void Ctu_Evaluate_ReturnsRungStateUnchanged()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var ctu = new Ctu("Ctr");

        Assert.IsTrue(ctu.Evaluate(tags, true, TimeSpan.Zero));
        Assert.IsFalse(ctu.Evaluate(tags, false, TimeSpan.Zero));
    }

    // ---- CTD (TP-206) -----------------------------------------------

    /// <summary>TP-206: CTD with .PRE=3, .ACC starting at 3. After 3 rising edges: .ACC=0, .DN=true. After RES: .ACC=0, .DN=false.</summary>
    [TestMethod]
    public void Ctd_ThreeRisingEdgesThenRes_MatchesTp206()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        tags.Get("Ctr").Counter!.Acc = 3;
        var ctd = new Ctd("Ctr");
        var res = new Res("Ctr");

        ctd.Evaluate(tags, false, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero);
        ctd.Evaluate(tags, false, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero);
        ctd.Evaluate(tags, false, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero);

        var counter = tags.Get("Ctr").Counter!;
        Assert.AreEqual(0, counter.Acc, "after 3 edges, .ACC should be 0");
        Assert.IsTrue(counter.Dn, "after 3 edges, .DN should be true");

        res.Evaluate(tags, true, TimeSpan.Zero);

        Assert.AreEqual(0, counter.Acc, "after RES, .ACC should stay 0");
        Assert.IsFalse(counter.Dn, "after RES, .DN should be false");
    }

    /// <summary>CTD's .DN condition is .ACC &lt;= 0, so it can go true (and stay true) even if the count continues below zero.</summary>
    [TestMethod]
    public void Ctd_CountsBelowZero_DnRemainsTrue()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        tags.Get("Ctr").Counter!.Acc = 1;
        var ctd = new Ctd("Ctr");

        ctd.Evaluate(tags, false, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero); // Acc 1 -> 0, DN true
        ctd.Evaluate(tags, false, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero); // Acc 0 -> -1, DN remains true

        var counter = tags.Get("Ctr").Counter!;
        Assert.AreEqual(-1, counter.Acc);
        Assert.IsTrue(counter.Dn);
    }

    /// <summary>Holding CTD's enable input true across scans counts only once (edge-triggered, same as CTU).</summary>
    [TestMethod]
    public void Ctd_EnableHeldTrueAcrossScans_CountsOnlyOnce()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        tags.Get("Ctr").Counter!.Acc = 3;
        var ctd = new Ctd("Ctr");

        ctd.Evaluate(tags, true, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero);
        ctd.Evaluate(tags, true, TimeSpan.Zero);

        Assert.AreEqual(2, tags.Get("Ctr").Counter!.Acc);
    }

    // ---- RES ----------------------------------------------------------

    /// <summary>RES is not edge-triggered: it resets every scan its own rung is true, not just on a rising edge.</summary>
    [TestMethod]
    public void Res_HeldTrueAcrossScans_KeepsResettingEveryScan()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var ctu = new Ctu("Ctr");
        var res = new Res("Ctr");

        ctu.Evaluate(tags, true, TimeSpan.Zero);
        Assert.AreEqual(1, tags.Get("Ctr").Counter!.Acc);

        res.Evaluate(tags, true, TimeSpan.Zero);
        Assert.AreEqual(0, tags.Get("Ctr").Counter!.Acc);

        // While RES's rung stays true, the counter can't accumulate even if CTU also fires this scan.
        ctu.Evaluate(tags, false, TimeSpan.Zero);
        ctu.Evaluate(tags, true, TimeSpan.Zero);
        res.Evaluate(tags, true, TimeSpan.Zero);
        Assert.AreEqual(0, tags.Get("Ctr").Counter!.Acc);
    }

    /// <summary>RES does nothing when its own rung is false — a disabled RES must not clobber an actively-counting counter.</summary>
    [TestMethod]
    public void Res_RungFalse_DoesNotResetCounter()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var ctu = new Ctu("Ctr");
        var res = new Res("Ctr");

        ctu.Evaluate(tags, true, TimeSpan.Zero);
        res.Evaluate(tags, false, TimeSpan.Zero);

        Assert.AreEqual(1, tags.Get("Ctr").Counter!.Acc);
    }

    /// <summary>RES returns rungState unchanged (action-type pass-through).</summary>
    [TestMethod]
    public void Res_Evaluate_ReturnsRungStateUnchanged()
    {
        var tags = BuildCounterTagTable("Ctr", preset: 3);
        var res = new Res("Ctr");

        Assert.IsTrue(res.Evaluate(tags, true, TimeSpan.Zero));
        Assert.IsFalse(res.Evaluate(tags, false, TimeSpan.Zero));
    }

    /// <summary>A counter instruction targeting a non-COUNTER tag fails clearly rather than silently misbehaving.</summary>
    [TestMethod]
    public void Ctu_TargetingNonCounterTag_ThrowsDescriptiveException()
    {
        var controlLogic = new ControlLogicDef
        {
            Tags = new[] { new TagDef { Name = "NotACounter", Type = TagTypeDef.Bool, InitialValue = false } },
            Rungs = Array.Empty<RungDef>(),
        };
        var tags = ControlLogicBuilder.BuildTagTable(controlLogic);
        var ctu = new Ctu("NotACounter");

        Assert.ThrowsException<InvalidOperationException>(() => ctu.Evaluate(tags, true, TimeSpan.Zero));
    }
}
