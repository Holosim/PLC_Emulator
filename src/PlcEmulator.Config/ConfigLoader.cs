namespace PlcEmulator.Config;

/// <summary>
/// Parses CONTROL_LOGIC and NETWORK JSON into immutable definition
/// objects, including cross-file validation (DATA-IN-103). Produces
/// either a fully valid definition pair or a descriptive error —
/// never a partial result (see docs/SDD.md, Architecture).
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// Loads and parses a CONTROL_LOGIC JSON file from <paramref name="path"/>.
    /// </summary>
    /// <exception cref="ConfigValidationException">
    /// Thrown on malformed JSON or a schema violation (UI-003).
    /// </exception>
    public static ControlLogicDef LoadControlLogic(string path)
    {
        // TODO: System.Text.Json parse + schema validation (DATA-IN-100/101).
        throw new NotImplementedException("ConfigLoader.LoadControlLogic is scaffolding only.");
    }

    /// <summary>
    /// Loads and parses a NETWORK JSON file from <paramref name="path"/>.
    /// </summary>
    /// <exception cref="ConfigValidationException">
    /// Thrown on malformed JSON or a schema violation (UI-003).
    /// </exception>
    public static NetworkDef LoadNetwork(string path)
    {
        // TODO: System.Text.Json parse + schema validation (DATA-IN-102).
        throw new NotImplementedException("ConfigLoader.LoadNetwork is scaffolding only.");
    }

    /// <summary>
    /// Cross-file validation: every NETWORK component's tag binding
    /// must reference a tag that exists in CONTROL_LOGIC (DATA-IN-103).
    /// </summary>
    /// <exception cref="ConfigValidationException">
    /// Thrown with a descriptive error identifying the offending
    /// component and tag reference.
    /// </exception>
    public static void Validate(ControlLogicDef controlLogic, NetworkDef network)
    {
        // TODO: cross-reference validation (DATA-IN-103).
        throw new NotImplementedException("ConfigLoader.Validate is scaffolding only.");
    }
}
