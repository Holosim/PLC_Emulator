namespace PlcEmulator.Config;

/// <summary>
/// One parsed ladder rung (DATA-IN-101): an ordered list of
/// instruction definitions, evaluated left-to-right.
/// </summary>
public sealed class RungDef
{
    public required IReadOnlyList<InstructionDef> Instructions { get; init; }
}
