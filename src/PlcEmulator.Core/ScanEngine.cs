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
    /// </remarks>
    public void Evaluate(IReadOnlyList<Rung> rungs, TagTable tags)
    {
        foreach (var rung in rungs)
        {
            var rungState = true;

            foreach (var instruction in rung.Instructions)
            {
                rungState = instruction.Evaluate(tags, rungState);
            }
        }
    }
}
