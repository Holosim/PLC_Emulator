using PlcEmulator.Config;
using PlcEmulator.Core;
using PlcEmulator.Drivers;
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
    /// <summary>
    /// Default TCP listen port when <c>--port</c> is not supplied.
    /// OUT-400 leaves the port operator-configurable (no specific port
    /// was mandated by the client, per docs/RTVM.md's "Assumptions made
    /// while breaking down scope"), but TP-001's command line
    /// (`plcemu --control-logic ... --network ...`) omits `--port`
    /// entirely, so v1.0 needs a usable default for that common case.
    /// </summary>
    private const int DefaultPort = 5000;

    public static int Main(string[] args)
    {
        Dictionary<string, string> options;
        try
        {
            options = ParseArgs(args);
        }
        catch (CliArgumentException ex)
        {
            Console.Error.WriteLine($"plcemu: {ex.Message}");
            return 1;
        }

        if (!options.TryGetValue("control-logic", out var controlLogicPath))
        {
            Console.Error.WriteLine("Missing required argument: --control-logic");
            return 1;
        }

        if (!options.TryGetValue("network", out var networkPath))
        {
            Console.Error.WriteLine("Missing required argument: --network");
            return 1;
        }

        var port = DefaultPort;
        if (options.TryGetValue("port", out var portText))
        {
            if (!int.TryParse(portText, out port) || port <= 0)
            {
                Console.Error.WriteLine(
                    $"plcemu: invalid value for --port: '{portText}' (must be a positive integer).");
                return 1;
            }
        }

        // Load + cross-validate + build the controller before touching
        // the network layer at all — a failure anywhere in this block
        // must exit non-zero without ever starting the TCP listener
        // (UI-003).
        PlcController controller;
        try
        {
            var controlLogic = ConfigLoader.LoadControlLogic(controlLogicPath);
            var network = ConfigLoader.LoadNetwork(networkPath);
            ConfigLoader.Validate(controlLogic, network);
            controller = new PlcController(controlLogic, network, DriverFactory.Create);

            PrintStartupDiagnostics(controlLogic, network, controlLogicPath, networkPath);
        }
        catch (ConfigValidationException ex)
        {
            Console.Error.WriteLine($"plcemu: {ex.Message}");
            return 1;
        }

        // Startup load succeeded — only now do we stand up the network
        // layer. Any failure here is reported the same fail-fast way,
        // even though it is not itself a UI-003 config-validation error.
        var server = new TcpJsonServer(controller);
        try
        {
            server.Start(port);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"plcemu: failed to start TCP listener on port {port}: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"plcemu: listening on TCP port {port}.");

        // Keep the process alive by driving the free-running scan loop
        // (OUT-403) on the main thread itself — this both keeps plcemu
        // running as a long-lived server and is the thing that
        // actually keeps work flowing. This loop belongs to the Host,
        // not TcpJsonServer: the server's job is the client
        // protocol/connection lifecycle, not deciding when a scan runs
        // (see docs/SDD.md, Architecture).
        RunScanLoop(controller, server);
        return 0;
    }

    /// <summary>
    /// Repeatedly runs one scan cycle (<see cref="PlcController.RunScan"/>)
    /// and broadcasts the resulting snapshot (<see cref="TcpJsonServer.Broadcast"/>,
    /// DATA-OUT-301) to whoever is connected — or no one, if no client
    /// is connected right now (OUT-403). Runs back-to-back with no
    /// artificial delay between scans: v1.0 does not define a fixed
    /// scan period (see CORE-203/204's own elapsed-time design), so
    /// this free-runs as fast as possible, matching how a real PLC
    /// scans. A failure inside one scan is logged and the loop
    /// continues with the next cycle rather than taking the whole
    /// long-running process down, the same "one bad cycle doesn't kill
    /// the server" posture already used for individual client messages
    /// (see <see cref="TcpJsonServer"/>'s read loop).
    /// </summary>
    private static void RunScanLoop(PlcController controller, TcpJsonServer server)
    {
        while (true)
        {
            try
            {
                controller.RunScan();
                server.Broadcast(controller.GetSnapshot());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"plcemu: error during scan cycle: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Prints structured startup diagnostics (UI-002) once CONTROL_LOGIC
    /// and NETWORK have both loaded and cross-validated successfully:
    /// the loaded-count summary line for each file, followed by a
    /// per-tag (name/type) and per-component (name/driver) listing —
    /// this is the "loaded model visible" teaching-tool view called out
    /// in SN-2 / docs/SDD.md's Host responsibilities. Only ever called
    /// from the success path in <see cref="Main"/>; a load/validation
    /// failure reports its own error instead (UI-003) and never reaches
    /// here.
    /// </summary>
    /// <remarks>
    /// TP-003 checks for the literal substrings <c>"3 tags loaded"</c>
    /// and <c>"2 components loaded"</c>, so the count lines always use
    /// the plural noun regardless of count (i.e. `"1 tags loaded"`, not
    /// `"1 tag loaded"`) — matching the requirement text verbatim
    /// rather than adding singular/plural grammar the RTVM doesn't ask
    /// for.
    /// </remarks>
    private static void PrintStartupDiagnostics(
        ControlLogicDef controlLogic, NetworkDef network, string controlLogicPath, string networkPath)
    {
        Console.WriteLine($"plcemu: {controlLogic.Tags.Count} tags loaded from '{controlLogicPath}':");
        foreach (var tag in controlLogic.Tags)
        {
            Console.WriteLine($"plcemu:   {tag.Name} ({FormatTagType(tag.Type)})");
        }

        Console.WriteLine($"plcemu: {network.Components.Count} components loaded from '{networkPath}':");
        foreach (var component in network.Components)
        {
            Console.WriteLine($"plcemu:   {component.Name} ({component.DriverType})");
        }
    }

    /// <summary>
    /// Renders a <see cref="TagTypeDef"/> the same way it's written in
    /// CONTROL_LOGIC JSON (e.g. <c>BOOL</c>, <c>DINT</c>) rather than
    /// its .NET enum-member casing, so the diagnostics line matches the
    /// source file an engineer-in-training is reading alongside it.
    /// </summary>
    private static string FormatTagType(TagTypeDef type) => type.ToString().ToUpperInvariant();

    /// <summary>
    /// Parses <c>--key value</c> pairs from the raw command line. Every
    /// argument must be a <c>--name</c> flag immediately followed by its
    /// value; an unrecognized shape is reported as a
    /// <see cref="CliArgumentException"/> (UI-003) rather than silently
    /// ignored.
    /// </summary>
    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal) || arg.Length <= 2)
            {
                throw new CliArgumentException($"Unrecognized argument: '{arg}'.");
            }

            var key = arg[2..];
            var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            if (!hasValue)
            {
                throw new CliArgumentException($"Argument --{key} requires a value.");
            }

            options[key] = args[++i];
        }

        return options;
    }

    private sealed class CliArgumentException : Exception
    {
        public CliArgumentException(string message) : base(message)
        {
        }
    }
}
