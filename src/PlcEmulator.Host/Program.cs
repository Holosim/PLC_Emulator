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

            Console.WriteLine(
                $"plcemu: loaded {controlLogic.Tags.Count} tag(s) from '{controlLogicPath}' and " +
                $"{network.Components.Count} network component(s) from '{networkPath}'.");
        }
        catch (ConfigValidationException ex)
        {
            Console.Error.WriteLine($"plcemu: {ex.Message}");
            return 1;
        }

        // Startup load succeeded — only now do we stand up the network
        // layer. Any failure here is reported the same fail-fast way,
        // even though it is not itself a UI-003 config-validation error.
        try
        {
            var server = new TcpJsonServer(controller);
            server.Start(port);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"plcemu: failed to start TCP listener on port {port}: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"plcemu: listening on TCP port {port}.");

        // Keep the process (and its listener) alive; plcemu is a
        // long-running server, not a one-shot command. The scan loop /
        // connection-lifecycle wiring that actually keeps work flowing
        // while blocked here belongs to TcpJsonServer itself (OUT-400
        // and friends), not to this composition root.
        Thread.Sleep(Timeout.Infinite);
        return 0;
    }

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
