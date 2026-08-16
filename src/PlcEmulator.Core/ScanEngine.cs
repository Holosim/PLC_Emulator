namespace PlcEmulator.Core;

/// <summary>
/// Evaluates all ladder rungs in program order once per scan,
/// updating the owning controller's <see cref="TagTable"/> before the
/// next scan begins (CORE-200). Owned by, not shared across,
/// <see cref="PlcController"/> instances.
/// </summary>
public sealed class ScanEngine
{
    /// <summary>Evaluates every rung, in order, against <paramref name="tags"/>.</summary>
    public void Evaluate(IReadOnlyList<Rung> rungs, TagTable tags)
    {
        // TODO: single scan pass over rungs (CORE-200). The Scan
        // Engine never throws for expected runtime conditions like
        // divide-by-zero (CORE-208) — those set a fault flag on the
        // offending tag/instruction result instead (docs/SDD.md,
        // Coding Standards / Error handling).
        throw new NotImplementedException("ScanEngine.Evaluate is scaffolding only.");
    }
}
