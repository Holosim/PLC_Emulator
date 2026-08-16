using PlcEmulator.Core;
using PlcEmulator.Network;

namespace PlcEmulator.Host;

/// <summary>
/// Composition root / CLI entry point (UI-001). Parses CLI arguments,
/// invokes the Config Loader, constructs one <see cref="PlcController"/>
/// from the resulting definitions, constructs one
/// <see cref="TcpJsonServer"/> bound to that controller, and starts
/// both. Owns startup diagnostics (UI-002) and fail-fast error
/// handling (UI-003) — nothing below the Host swallows a load error
/// into a partially-started state (see docs/SDD.md, Architecture).
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        // TODO: CLI argument parsing (--control-logic, --network,
        // --port), ConfigLoader.LoadControlLogic/LoadNetwork/Validate,
        // PlcController + TcpJsonServer construction and startup, and
        // fail-fast error handling all land here (UI-001/002/003).
        Console.Error.WriteLine("plcemu: scaffolding only, no functional startup path yet.");
        return 1;
    }
}
