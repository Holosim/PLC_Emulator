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
    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Loads and parses a CONTROL_LOGIC JSON file from <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// CONTROL_LOGIC JSON schema (DATA-IN-100/101):
    /// <code>
    /// {
    ///   "tags": [
    ///     { "name": "Start_PB", "type": "BOOL", "initialValue": false },
    ///     { "name": "Preset_Count", "type": "DINT", "initialValue": 0 },
    ///     { "name": "MyTimer", "type": "TIMER", "preset": 3000 }
    ///   ],
    ///   "rungs": [
    ///     { "instructions": [
    ///         { "op": "XIC", "operands": ["Start_PB"] },
    ///         { "op": "OTE", "operands": ["Motor_Run"] }
    ///     ] }
    ///   ]
    /// }
    /// </code>
    /// <c>type</c> is one of <c>BOOL</c>/<c>DINT</c>/<c>REAL</c> (scalar,
    /// requires <c>initialValue</c>) or <c>TIMER</c>/<c>COUNTER</c>
    /// (structured, uses <c>preset</c> instead — <c>.ACC</c>/<c>.DN</c>/
    /// <c>.EN</c> always start at their zero/false defaults). Each
    /// instruction's <c>operands</c> are, in order, JSON strings (tag
    /// references) or JSON numbers (literals); exact arity per
    /// mnemonic is enforced by
    /// <c>PlcEmulator.Core.Instructions.InstructionFactory</c>, not
    /// here — this loader only validates that CONTROL_LOGIC is
    /// well-formed JSON matching this generic shape.
    /// </remarks>
    /// <exception cref="ConfigValidationException">
    /// Thrown on malformed JSON or a schema violation (UI-003).
    /// </exception>
    public static ControlLogicDef LoadControlLogic(string path)
    {
        var json = ReadFile(path, "CONTROL_LOGIC");
        var wire = Deserialize<ControlLogicWire>(json, path, "CONTROL_LOGIC");
        return BuildControlLogicDef(wire, path);
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
            dto = JsonSerializer.Deserialize<NetworkFileDto>(json, WireOptions);
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

    private static string ReadFile(string path, string kind)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ConfigValidationException($"{kind} file could not be read: '{path}'. {ex.Message}", ex);
        }
    }

    private static T Deserialize<T>(string json, string path, string kind)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, WireOptions)
                ?? throw new ConfigValidationException($"{kind} file '{path}' parsed to an empty document.");
        }
        catch (JsonException ex)
        {
            throw new ConfigValidationException($"{kind} file '{path}' is not valid JSON: {ex.Message}", ex);
        }
    }

    private static ControlLogicDef BuildControlLogicDef(ControlLogicWire wire, string path)
    {
        var tagWires = wire.Tags ?? new List<TagWire>();
        var tags = new List<TagDef>(tagWires.Count);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var tagWire in tagWires)
        {
            var tagDef = BuildTagDef(tagWire, path);
            if (!seenNames.Add(tagDef.Name))
            {
                throw new ConfigValidationException(
                    $"CONTROL_LOGIC file '{path}' defines tag '{tagDef.Name}' more than once.");
            }

            tags.Add(tagDef);
        }

        var rungWires = wire.Rungs ?? new List<RungWire>();
        var rungs = new List<RungDef>(rungWires.Count);

        foreach (var rungWire in rungWires)
        {
            var instructionWires = rungWire.Instructions ?? new List<InstructionWire>();
            var instructions = new List<InstructionDef>(instructionWires.Count);

            foreach (var instructionWire in instructionWires)
            {
                instructions.Add(BuildInstructionDef(instructionWire, path));
            }

            rungs.Add(new RungDef { Instructions = instructions });
        }

        return new ControlLogicDef { Tags = tags, Rungs = rungs };
    }

    private static TagDef BuildTagDef(TagWire wire, string path)
    {
        if (string.IsNullOrWhiteSpace(wire.Name))
        {
            throw new ConfigValidationException(
                $"CONTROL_LOGIC file '{path}' has a tag with a missing or empty 'name'.");
        }

        var type = ParseTagType(wire.Type, wire.Name, path);

        return type is TagTypeDef.Timer or TagTypeDef.Counter
            ? new TagDef { Name = wire.Name, Type = type, Preset = wire.Preset ?? 0 }
            : new TagDef { Name = wire.Name, Type = type, InitialValue = ParseInitialValue(type, wire, path) };
    }

    private static TagTypeDef ParseTagType(string? typeText, string tagName, string path)
    {
        return typeText?.ToUpperInvariant() switch
        {
            "BOOL" => TagTypeDef.Bool,
            "DINT" => TagTypeDef.Dint,
            "REAL" => TagTypeDef.Real,
            "TIMER" => TagTypeDef.Timer,
            "COUNTER" => TagTypeDef.Counter,
            _ => throw new ConfigValidationException(
                $"CONTROL_LOGIC file '{path}' tag '{tagName}' has an unrecognized type " +
                $"'{typeText}'. Must be one of BOOL, DINT, REAL, TIMER, COUNTER."),
        };
    }

    private static object ParseInitialValue(TagTypeDef type, TagWire wire, string path)
    {
        if (wire.InitialValue is not { } element)
        {
            throw new ConfigValidationException(
                $"CONTROL_LOGIC file '{path}' tag '{wire.Name}' ({wire.Type}) is missing 'initialValue'.");
        }

        try
        {
            return type switch
            {
                TagTypeDef.Bool => element.GetBoolean(),
                TagTypeDef.Dint => element.GetInt32(),
                TagTypeDef.Real => element.GetDouble(),
                _ => throw new InvalidOperationException("unreachable — only scalar types reach ParseInitialValue"),
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new ConfigValidationException(
                $"CONTROL_LOGIC file '{path}' tag '{wire.Name}' has an 'initialValue' that does not " +
                $"match its declared type '{wire.Type}'.", ex);
        }
    }

    private static InstructionDef BuildInstructionDef(InstructionWire wire, string path)
    {
        if (string.IsNullOrWhiteSpace(wire.Op))
        {
            throw new ConfigValidationException(
                $"CONTROL_LOGIC file '{path}' has an instruction with a missing 'op'.");
        }

        var operandElements = wire.Operands ?? new List<JsonElement>();
        var operands = new List<OperandDef>(operandElements.Count);

        foreach (var element in operandElements)
        {
            operands.Add(element.ValueKind switch
            {
                JsonValueKind.String => OperandDef.Tag(element.GetString()!),
                JsonValueKind.Number => OperandDef.Number(element.GetDouble()),
                _ => throw new ConfigValidationException(
                    $"CONTROL_LOGIC file '{path}' instruction '{wire.Op}' has an operand that is " +
                    "neither a tag-name string nor a numeric literal."),
            });
        }

        return new InstructionDef { Mnemonic = wire.Op.ToUpperInvariant(), Operands = operands };
    }

    // Wire-format types: a direct mirror of CONTROL_LOGIC JSON's shape,
    // deserialized by System.Text.Json and then validated/mapped into
    // the public DTOs above. Kept private — nothing outside this class
    // should depend on the JSON's literal field layout.

    private sealed class ControlLogicWire
    {
        public List<TagWire>? Tags { get; set; }
        public List<RungWire>? Rungs { get; set; }
    }

    private sealed class TagWire
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public JsonElement? InitialValue { get; set; }
        public int? Preset { get; set; }
    }

    private sealed class RungWire
    {
        public List<InstructionWire>? Instructions { get; set; }
    }

    private sealed class InstructionWire
    {
        public string? Op { get; set; }
        public List<JsonElement>? Operands { get; set; }
    }
}
