using IHateDPI.Engine.Abstractions;
using IHateDPI.Engine.Models;
using IHateDPI.Engine.Networking;
using IHateDPI.Engine.Processors;
using IHateDPI.Engine.Services;
using System.Text.Json;
using System.Threading.Channels;

namespace IHateDPI.Engine;

internal class Program
{
    private const string ConfigFileName = "engineConfig.json";

    static void Main()
    {
        Console.Title = "IHateDPI Engine";
        Console.WriteLine("Initializing IHateDPI...");

        // 1. Load Configuration
        var config = LoadConfiguration();
        PrintConfiguration(config);

        try
        {
            // 2. Lifecycle Management (Composition Root)
            // Since we aren't using a heavy DI container, we manually instantiate and link dependencies here.
            // 'using' statements ensure all resources are disposed correctly upon exit.

            // Shared Dependencies
            using var ttlTracker = new TtlTracker();
            using var dnsResolver = new DohClient(config.DohProviderUrl);

            // Build and Start the Engine
            using var engine = BuildEngine(config, ttlTracker, dnsResolver);

            // Hook up logging to Console
            engine.OnLog += (msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

            engine.Start();

            Console.WriteLine("\n--> Engine is active. Press ENTER to shutdown.\n");
            Console.ReadLine();

            Console.WriteLine("Stopping engine...");
            engine.Stop();
        }
        catch (DllNotFoundException)
        {
            PrintCriticalError("WinDivert driver files are missing!",
                "Please ensure 'WinDivert.dll' and 'WinDivert64.sys' are present in the application directory.");
        }
        catch (Exception ex)
        {
            PrintCriticalError($"FATAL ERROR: {ex.Message}",
                ex.InnerException != null ? $"Caused by: {ex.InnerException.Message}" : null);
        }

        // Resources (ttlTracker, dnsResolver, engine) are automatically disposed here.
    }

    /// <summary>
    /// Loads the configuration from disk or creates a default one if it doesn't exist.
    /// </summary>
    private static EngineConfig LoadConfiguration()
    {
        try
        {
            if (File.Exists(ConfigFileName))
            {
                var json = File.ReadAllText(ConfigFileName);
                // Use Source Generator context for AOT safety
                var loadedConfig = JsonSerializer.Deserialize(json, AppConfigContext.Default.EngineConfig);
                return loadedConfig ?? new EngineConfig();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warning] Failed to load configuration: {ex.Message}. Using defaults.");
            Console.ResetColor();
        }

        // Create and save default config
        var defaults = new EngineConfig();
        var defaultJson = JsonSerializer.Serialize(defaults, AppConfigContext.Default.EngineConfig);
        File.WriteAllText(ConfigFileName, defaultJson);

        return defaults;
    }

    /// <summary>
    /// Constructs the DpiEngine and wires up all processors and channels.
    /// </summary>
    private static DpiEngine BuildEngine(EngineConfig config, ITtlTracker ttlTracker, IDnsResolver dnsResolver)
    {
        // Setup High-Performance Channel
        // Bounded capacity prevents memory exhaustion (Backpressure) if the consumer falls behind.
        var channelOptions = new BoundedChannelOptions(1000)
        {
            SingleReader = true,  // Consumer: DnsOverHttpsService
            SingleWriter = true,  // Producer: UdpPacketProcessor
            FullMode = BoundedChannelFullMode.DropOldest
        };

        var dnsChannel = Channel.CreateBounded<DnsRequestSnapshot>(channelOptions);

        // Instantiate Processors
        var udpProcessor = new UdpPacketProcessor(config, dnsChannel.Writer);
        var tcpProcessor = new TcpPacketProcessor(config, ttlTracker);

        IPacketProcessor[] processors = [tcpProcessor, udpProcessor];

        return new DpiEngine(processors, dnsResolver, dnsChannel, config, ttlTracker);
    }

    /// <summary>
    /// Prints the active configuration to the console in a human-readable, aligned table format.
    /// </summary>
    private static void PrintConfiguration(EngineConfig config)
    {
        Console.WriteLine("\n--- Active Configuration ---");

        var properties = typeof(EngineConfig).GetProperties();

        int maxNameLength = 0;
        foreach (var prop in properties)
        {
            if (prop.Name.Length > maxNameLength)
                maxNameLength = prop.Name.Length;
        }

        foreach (var prop in properties)
        {
            var value = prop.GetValue(config);

            Console.Write($" {prop.Name.PadRight(maxNameLength)} : ");

            if (value is bool boolVal)
                Console.ForegroundColor = boolVal ? ConsoleColor.Green : ConsoleColor.DarkGray;
            else
                Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine(value ?? "null");
            Console.ResetColor();
        }

        Console.WriteLine("----------------------------\n");
    }

    private static void PrintCriticalError(string message, string? detail = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        if (!string.IsNullOrEmpty(detail))
        {
            Console.WriteLine(detail);
        }
        Console.ResetColor();
    }
}