namespace PlcEmulator.Config;

/// <summary>
/// Wire-format shape of a NETWORK JSON document, deserialized directly
/// by <see cref="System.Text.Json"/> before <see cref="ConfigLoader.LoadNetwork"/>
/// validates it and maps it onto the immutable <see cref="NetworkDef"/>
/// model. Kept separate from <see cref="NetworkDef"/> so the wire
/// format (nullable, unvalidated) never leaks past the loader — nothing
/// outside <c>PlcEmulator.Config</c> should ever see this type.
/// </summary>
/// <remarks>
/// Top-level shape: <c>{ "components": [ { ... }, ... ] }</c>. Property
/// names match the NETWORK JSON examples in docs/RTVM.md (TP-005,
/// TP-102) verbatim (lowercase); deserialization is configured
/// case-insensitively in <see cref="ConfigLoader"/> so this still holds
/// if a future NETWORK document capitalizes them.
/// </remarks>
internal sealed class NetworkFileDto
{
    public List<NetworkComponentDto>? Components { get; set; }
}

/// <summary>
/// Wire-format shape of one NETWORK component entry. <see cref="Tag"/>
/// (singular) and <see cref="Tags"/> (plural) are both accepted —
/// DATA-IN-102 allows a binding to "one or more" tags; TP-102's example
/// uses the singular <c>"tag"</c> form. <see cref="ConfigLoader.LoadNetwork"/>
/// merges whichever of the two is present into one ordered list.
/// </summary>
internal sealed class NetworkComponentDto
{
    public string? Name { get; set; }
    public string? Driver { get; set; }
    public string? Tag { get; set; }
    public List<string>? Tags { get; set; }
}
