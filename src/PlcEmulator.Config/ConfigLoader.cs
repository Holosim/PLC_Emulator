using System.Text.Json;

namespace PlcEmulator.Config;

/// <summary>
/// Parses CONTROL_LOGIC and NETWORK JSON into immutable definition
/// objects, including cross-file validation (DATA-IN-103). Produces
/// either a fully valid definition pair or a descriptive error —
/// never a partial result (see docs/SDD.md, Architecture).
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
    /// Loads and parses a NETWORK JSON file from <paramref name="path"/>
    /// (DATA-IN-102): <c>{ "components": [ { "name", "driver", "tag" | "tags" }, ... ] }</c>.
    /// Validates only the NETWORK document's own shape — every
    /// component has a non-empty name, driver type, and at least one
    /// tag binding. It does <em>not</em> check that the bound tag(s)
    /// actually exist in a CONTROL_LOGIC document; that cross-file
    /// check is <see cref="Validate"/> (DATA-IN-103).
    /// </summary>
    /// <exception cref="ConfigValidationException">
    /// Thrown on an unreadable file, malformed JSON, or a schema
    /// violation (UI-003) — always with a message identifying the
    /// offending component by name/index.
    /// </exception>
    public static NetworkDef LoadNetwork(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ConfigValidationException($"Could not read NETWORK file '{path}': {ex.Message}", ex);
        }

        NetworkFileDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<NetworkFileDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ConfigValidationException($"NETWORK file '{path}' is not valid JSON: {ex.Message}", ex);
        }

        if (dto is null)
        {
            throw new ConfigValidationException($"NETWORK file '{path}' does not contain a JSON object.");
        }

        if (dto.Components is null || dto.Components.Count == 0)
        {
            throw new ConfigValidationException(
                $"NETWORK file '{path}' defines no components — expected a non-empty \"components\" array.");
        }

        var components = new List<NetworkComponentConfig>(dto.Components.Count);
        for (var i = 0; i < dto.Components.Count; i++)
        {
            components.Add(ParseComponent(dto.Components[i], i, path));
        }

        return new NetworkDef { Components = components };
    }

    private static NetworkComponentConfig ParseComponent(NetworkComponentDto component, int index, string path)
    {
        // Prefer the component's own name in error messages once we
        // have one; fall back to its position for the "no name at all"
        // case.
        string Describe() => string.IsNullOrWhiteSpace(component.Name)
            ? $"NETWORK component #{index} in '{path}'"
            : $"NETWORK component '{component.Name}' in '{path}'";

        if (string.IsNullOrWhiteSpace(component.Name))
        {
            throw new ConfigValidationException($"{Describe()} is missing a \"name\".");
        }

        if (string.IsNullOrWhiteSpace(component.Driver))
        {
            throw new ConfigValidationException($"{Describe()} is missing a \"driver\" type reference.");
        }

        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(component.Tag))
        {
            tags.Add(component.Tag);
        }
        if (component.Tags is not null)
        {
            tags.AddRange(component.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
        }

        if (tags.Count == 0)
        {
            throw new ConfigValidationException(
                $"{Describe()} has no tag binding — expected a \"tag\" string or a non-empty \"tags\" array.");
        }

        return new NetworkComponentConfig
        {
            Name = component.Name,
            DriverType = component.Driver,
            Tags = tags,
        };
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
