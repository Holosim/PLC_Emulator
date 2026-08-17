namespace PlcEmulator.Core.Instructions;

/// <summary>
/// Shared contract for every ladder-logic instruction mnemonic
/// (<c>XIC</c>, <c>OTE</c>, <c>TON</c>, <c>CTU</c>, etc.). Stateless —
/// operates only on the <see cref="TagTable"/> it is given, so the
/// same instruction logic is reused correctly across isolated
/// <see cref="PlcController"/> instances (NFR-500; see docs/SDD.md,
/// Coding Standards / Instruction classes).
/// </summary>
public interface IInstruction
{
    /// <summary>
    /// The instruction's mnemonic (<c>XIC</c>, <c>OTE</c>, <c>TON</c>,
    /// etc.), preserved verbatim from CONTROL_LOGIC JSON (DATA-IN-101;
    /// see docs/SDD.md, Coding Standards / Naming). Used for
    /// diagnostics/logging (UI-002) and for inspecting a parsed rung's
    /// instruction sequence (e.g. TP-101).
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Evaluates this instruction against the given tag table.
    /// </summary>
    /// <param name="tags">The owning controller's tag table (the only mutable state an instruction ever touches).</param>
    /// <param name="rungState">
    /// The rung's accumulated power-flow state immediately before this
    /// instruction — "rung-condition-in", standard ladder-logic
    /// terminology. <see cref="ScanEngine"/> seeds this to
    /// <see langword="true"/> (energized from the left power rail) at
    /// the start of every rung and threads each instruction's return
    /// value into the next instruction's <paramref name="rungState"/>,
    /// left to right (CORE-200). Condition-type instructions (contacts
    /// <c>XIC</c>/<c>XIO</c>, compares) AND their own tag-based
    /// condition into <paramref name="rungState"/> and return the
    /// result ("rung-condition-out"). Action-type instructions (coils,
    /// timers, counters, math) use <paramref name="rungState"/> to
    /// decide whether to apply their side effect (e.g. a coil only
    /// writes true when energized) and normally return it unchanged,
    /// so power flow continues correctly past them to any further
    /// instructions on the same rung.
    /// </param>
    /// <param name="elapsed">
    /// Real (wall-clock) time elapsed since the previous scan's call to
    /// <see cref="ScanEngine.Evaluate"/> — <see cref="TimeSpan.Zero"/>
    /// on a controller's very first scan, since there is no previous
    /// scan to measure from (CORE-203/204). <see cref="ScanEngine"/>
    /// measures this once per scan (not per rung/instruction) and
    /// passes the same value to every instruction evaluated during that
    /// scan. Only time-driven instructions (<c>TON</c>, <c>TOF</c>) use
    /// this; every other instruction ignores it. Measuring wall-clock
    /// time here (rather than assuming a fixed scan period) matches how
    /// a real PLC's timer accumulates against the actual time between
    /// scans, not an idealized one — see docs/SDD.md, Coding Standards.
    /// </param>
    /// <returns>This instruction's rung-condition-out, fed to the next instruction in the rung as its <paramref name="rungState"/>.</returns>
    bool Evaluate(TagTable tags, bool rungState, TimeSpan elapsed);
}
