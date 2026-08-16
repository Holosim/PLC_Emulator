namespace PlcEmulator.Core;

/// <summary>
/// The tag data types supported by the CONTROL_LOGIC data model
/// (DATA-IN-100): <see cref="Bool"/>/<see cref="Dint"/>/<see cref="Real"/>
/// are scalar types whose value lives in <see cref="Tag.Value"/>;
/// <see cref="Timer"/>/<see cref="Counter"/> are structured types whose
/// state lives in <see cref="Tag.Timer"/>/<see cref="Tag.Counter"/>
/// instead (<see cref="Tag.Value"/> is unused for those two).
/// </summary>
public enum TagType
{
    Bool,
    Dint,
    Real,
    Timer,
    Counter,
}
