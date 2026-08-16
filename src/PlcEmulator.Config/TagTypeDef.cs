namespace PlcEmulator.Config;

/// <summary>
/// The tag type strings recognized in CONTROL_LOGIC JSON's <c>type</c>
/// field (DATA-IN-100): <c>BOOL</c>, <c>DINT</c>, <c>REAL</c> for
/// scalar tags, plus <c>TIMER</c>/<c>COUNTER</c> for tags that carry
/// structured <c>.PRE</c>/<c>.ACC</c>/<c>.DN</c>/<c>.EN</c>
/// sub-elements instead of a scalar value.
/// </summary>
/// <remarks>
/// Deliberately a separate enum from <c>PlcEmulator.Core.TagType</c>
/// rather than one shared type: <c>PlcEmulator.Config</c> is a leaf
/// project (see docs/SDD.md, Coding Standards / Namespaces) and cannot
/// reference <c>PlcEmulator.Core</c>. <c>PlcEmulator.Core.ControlLogicBuilder</c>
/// maps this parse-time enum to the runtime <c>TagType</c> when
/// building a <c>TagTable</c> from a validated <see cref="ControlLogicDef"/>.
/// </remarks>
public enum TagTypeDef
{
    Bool,
    Dint,
    Real,
    Timer,
    Counter,
}
