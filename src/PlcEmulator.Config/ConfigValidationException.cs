namespace PlcEmulator.Config;

/// <summary>
/// Thrown by <see cref="ConfigLoader"/> for any load-time failure: a
/// malformed CONTROL_LOGIC/NETWORK file, a schema violation, or a
/// failed cross-file reference (UI-003, DATA-IN-103). Caught once at
/// the Host boundary and reported as a non-zero exit + stderr message
/// — nothing below the Host swallows this into a partially-started
/// state (see docs/SDD.md, Architecture / Error handling).
/// </summary>
public sealed class ConfigValidationException : Exception
{
    public ConfigValidationException(string message) : base(message)
    {
    }

    public ConfigValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
