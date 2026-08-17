using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Core.Drivers;

namespace PlcEmulator.Drivers;

/// <summary>
/// Base for every built-in driver whose NETWORK component binds to
/// exactly one BOOL tag (CORE-209) — discrete on/off devices like
/// <see cref="DiscreteSensorDriver"/> and <see cref="RelayDriver"/>.
/// </summary>
/// <remarks>
/// v1.0 has no external device simulation (that lands with OUT-400/401)
/// to give a sensor's input driver and a relay's output driver actually
/// different runtime behavior, so both are — today — a validated
/// binding to one BOOL tag and nothing more. <see cref="OnScanComplete"/>
/// is the extension point a future component-specific behavior (e.g. a
/// derived/debounced reading) would override once such a requirement is
/// stated; sharing this base keeps that eventual divergence a one-class
/// change, not a rewrite of the binding/validation logic every driver
/// needs regardless.
/// </remarks>
public abstract class SingleTagDriverBase : IDriver
{
    private Tag? _tag;

    /// <summary>Descriptive driver type name for error messages (e.g. <c>"DiscreteSensor"</c>) — matches <see cref="NetworkComponentConfig.DriverType"/>.</summary>
    protected abstract string DriverTypeName { get; }

    /// <summary>The single BOOL tag this driver instance is bound to. Valid only after <see cref="Bind"/> has run.</summary>
    protected Tag BoundTag =>
        _tag ?? throw new InvalidOperationException($"{DriverTypeName} driver used before Bind() was called.");

    /// <inheritdoc/>
    public void Bind(TagTable tags, NetworkComponentConfig config)
    {
        if (config.Tags.Count != 1)
        {
            throw new ConfigValidationException(
                $"NETWORK component '{config.Name}' ({DriverTypeName}) must bind to exactly one tag; got {config.Tags.Count}.");
        }

        var tagName = config.Tags[0];

        if (!tags.TryGet(tagName, out var tag) || tag is null)
        {
            throw new ConfigValidationException(
                $"NETWORK component '{config.Name}' ({DriverTypeName}) is bound to undefined tag '{tagName}'.");
        }

        if (tag.Type != TagType.Bool)
        {
            throw new ConfigValidationException(
                $"NETWORK component '{config.Name}' ({DriverTypeName}) must bind to a BOOL tag; '{tagName}' is {tag.Type}.");
        }

        _tag = tag;
    }

    /// <inheritdoc/>
    public virtual void OnScanComplete()
    {
        // No derived-state recomputation is required by any v1.0
        // requirement (see class remarks) — this touches BoundTag only
        // to enforce that Bind() ran before the scan loop ever calls
        // this, matching IDriver's documented lifecycle.
        _ = BoundTag;
    }
}
