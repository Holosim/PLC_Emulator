using System.Diagnostics;

namespace PlcEmulator.Core;

/// <summary>
/// Evaluates all ladder rungs in program order once per scan,
/// updating the owning controller's <see cref="TagTable"/> before the
/// next scan begins (CORE-200). Owned by, not shared across,
/// <see cref="PlcController"/> instances.
/// </summary>
public sealed class ScanEngine
{
    /// <summary>
    /// Measures real (wall-clock) time between successive
    /// <see cref="Evaluate"/> calls, for time-driven instructions
    /// (<c>TON</c>/<c>TOF</c>, CORE-203/204). This is instance state on
    /// <see cref="ScanEngine"/> itself, not on any
    /// <see cref="IInstruction"/> — instructions stay stateless per
    /// docs/SDD.md, and <see cref="ScanEngine"/> is already documented
    /// as owned by (never shared across) a single
    /// <see cref="PlcController"/>, so holding this clock here doesn't
    /// weaken NFR-500.
    /// </summary>
    private readonly Stopwatch _clock = new();

    private bool _hasRunBefore;

    /// <summary>
    /// Evaluates every rung, in program order, against
    /// <paramref name="tags"/> — one full pass, left to right within
    /// each rung (CORE-200).
    /// </summary>
    /// <remarks>
    /// For each rung, power flow ("rung state") starts energized
    /// (<see langword="true"/>, per the left power rail) and is
    /// threaded instruction-to-instruction via
    /// <see cref="IInstruction.Evaluate"/>'s return value — see that
    /// method's documentation for how condition- vs. action-type
    /// instructions use it. This loop never wraps an instruction's
    /// <c>Evaluate</c> call in a try/catch: the Scan Engine never
    /// throws for expected runtime conditions like divide-by-zero
    /// (CORE-208) — those are the responsibility of the instruction
    /// itself to turn into a fault flag on its result rather than an
    /// exception (docs/SDD.md, Coding Standards / Error handling), so
    /// a single bad rung can't crash the scan loop.
    /// <para>
    /// Also measures the real elapsed time since the previous call to
    /// this method (<see cref="TimeSpan.Zero"/> on the first call) and
    /// passes the same value to every instruction evaluated during this
    /// scan — this is what lets <c>TON</c>/<c>TOF</c> (CORE-203/204)
    /// accumulate against actual wall-clock time rather than an
    /// idealized fixed scan period, which v1.0 does not define.
    /// </para>
    /// </remarks>
    public void Evaluate(IReadOnlyList<Rung> rungs, TagTable tags)
    {
        var elapsed = _hasRunBefore ? _clock.Elapsed : TimeSpan.Zero;
        _clock.Restart();
        _hasRunBefore = true;

        foreach (var rung in rungs)
        {
            var rungState = true;

            foreach (var instruction in rung.Instructions)
            {
                rungState = instruction.Evaluate(tags, rungState, elapsed);
            }
        }
    }
}
