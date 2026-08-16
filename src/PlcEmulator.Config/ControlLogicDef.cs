namespace PlcEmulator.Config;

/// <summary>
/// Immutable, parsed representation of a CONTROL_LOGIC JSON document
/// (DATA-IN-100, DATA-IN-101): the tag definitions and ladder rungs
/// that make up one PLC program.
/// </summary>
/// <remarks>
/// Populated once by <see cref="ConfigLoader.LoadControlLogic"/> and
/// never mutated afterward — a <see cref="PlcEmulator.Core.PlcController"/>
/// is constructed from this definition, not bound to it live.
/// </remarks>
public sealed class ControlLogicDef
{
    /// <summary>Every tag defined in this CONTROL_LOGIC document, in declaration order (DATA-IN-100).</summary>
    public required IReadOnlyList<TagDef> Tags { get; init; }

    /// <summary>Every ladder rung defined in this CONTROL_LOGIC document, in program order (DATA-IN-101).</summary>
    public required IReadOnlyList<RungDef> Rungs { get; init; }
}
