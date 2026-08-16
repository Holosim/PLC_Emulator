using PlcEmulator.Core.Instructions;

namespace PlcEmulator.Core;

/// <summary>
/// One ladder rung: an ordered sequence of instructions evaluated
/// left-to-right, once per scan (CORE-200, DATA-IN-101).
/// </summary>
public sealed class Rung
{
    public required IReadOnlyList<IInstruction> Instructions { get; init; }
}
