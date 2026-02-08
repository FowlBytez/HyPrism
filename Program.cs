using ElectronNET;
using ElectronNET.API;
using ElectronNET.API.Entities;
using HyPrism.Services.Core;
using Microsoft.Extensions.DependencyInjection;

using Serilog;
using System.Runtime;
using System.Text;

namespace HyPrism;

class Program
{
    static async Task Main(string[] args)
    {
        // Memory optimization
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GCSettings.LatencyMode = GCLatencyMode.Interactive;

        // Initialize Logger
        var appDir = UtilityService.GetEffectiveAppDir();
        var logsDir = Path.Combine(appDir, "Logs");
        Directory.CreateDirectory(logsDir);

        var logFileName = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.log";
        var logFilePath = Path.Combine(logsDir, logFileName);

        try
        {
            File.WriteAllText(logFilePath, """
 .-..-.      .---.       _                
 : :; :      : .; :     :_;               
 :    :.-..-.:  _.'.--. .-. .--. ,-.,-.,-.
 : :: :: :; :: :   : ..': :`._-.': ,. ,. :
 :_;:_;`._. ;:_;   :_;  :_;`.__.':_;:_;:_;
        .-. :                             
        `._.'                     launcher

""" + Environment.NewLine);
        }
        catch { /* Ignore */ }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 20
            )
            .CreateLogger();

        // Get the runtime controller for Console App mode
        var runtimeController = ElectronNetRuntime.RuntimeController;

        try
        {
            // Intercept Console.Out/Error FIRST to route Electron.NET framework
            // messages through our Logger instead of raw stdout/stderr.
            // This must happen before any Console.Write/Logger calls to avoid
            // "stdout: ..." noise in the Electron terminal.
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            Console.SetOut(new ElectronLogInterceptor(originalOut, isError: false));
            Console.SetError(new ElectronLogInterceptor(originalErr, isError: true));

            Logger.Info("Boot", "Starting HyPrism (Electron.NET)...");
            Logger.Info("Boot", $"App Directory: {appDir}");

            // Initialize DI container
            var services = Bootstrapper.Initialize();

            // Start Electron runtime and wait for socket bridge
            Logger.Info("Boot", "Starting Electron runtime...");
            await runtimeController.Start();
            await runtimeController.WaitReadyTask;
            Logger.Info("Boot", "Electron runtime ready");

            // Create window & register IPC
            await ElectronBootstrap(services);

            // Keep alive until Electron quits
            await runtimeController.WaitStoppedTask;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed unexpectedly");
            Logger.Error("Crash", $"Application crashed: {ex.Message}");
            Console.WriteLine(ex.ToString());

            await runtimeController.Stop().ConfigureAwait(false);
            await runtimeController.WaitStoppedTask
                .WaitAsync(TimeSpan.FromSeconds(2))
                .ConfigureAwait(false);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task ElectronBootstrap(IServiceProvider services)
    {
        var wwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

        var mainWindow = await Electron.WindowManager.CreateWindowAsync(
            new BrowserWindowOptions
            {
                Width = 1280,
                Height = 800,
                MinWidth = 1024,
                MinHeight = 700,
                Frame = false,
                Show = false,
                Center = true,
                WebPreferences = new WebPreferences
                {
                    NodeIntegration = false,
                    ContextIsolation = true,
                    Preload = Path.Combine(wwwroot, "preload.js")
                },
                Title = "HyPrism",
                AutoHideMenuBar = true,
                BackgroundColor = "#0D0D10"
            },
            $"file://{Path.Combine(wwwroot, "index.html")}"
        );

        // Register IPC bridge (DI service)
        var ipcService = services.GetRequiredService<IpcService>();
        ipcService.RegisterAll();

        // Quit when all windows closed
        Electron.App.WindowAllClosed += () => Electron.App.Quit();

        // Show after ready
        mainWindow.OnReadyToShow += () => mainWindow.Show();

        Logger.Success("Boot", "Electron window created, IPC handlers registered");
    }
}

/// <summary>
/// Intercepts Console.Out / Console.Error to capture Electron.NET framework
/// messages (prefixed with <c>||</c>, <c>[StartCore]</c>, <c>[StartInternal]</c>,
/// <c>BridgeConnector</c> etc.) and routes them through <see cref="Logger"/>.
/// </summary>
file sealed class ElectronLogInterceptor : TextWriter
{
    private readonly TextWriter _original;
    private readonly bool _isError;

    // Noise patterns to suppress entirely
    private static readonly string[] SuppressPatterns =
    [
        "GetVSyncParametersIfAvailable()",
        "Passthrough is not supported",
        "viz.mojom.Compositor",
        "gpu_channel_manager",
        "sandboxed_process_launcher",
        "Fontconfig error",
    ];

    // Patterns that indicate debug-level info
    private static readonly string[] DebugPatterns =
    [
        "[StartCore]",
        "[StartInternal]",
        "BridgeConnector",
        "Socket.IO",
        "engine.io",
        "DevTools listening",
    ];

    // Patterns that indicate warnings
    private static readonly string[] WarningPatterns =
    [
        "ERROR:",
        "FATAL:",
        "(electron)",
        "Electron Helper",
        "crash",
    ];

    public ElectronLogInterceptor(TextWriter original, bool isError)
    {
        _original = original;
        _isError = isError;
    }

    public override Encoding Encoding => _original.Encoding;

    public override void WriteLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var line = value.Trim();

        // Strip "|| " prefix that Electron.NET adds
        if (line.StartsWith("|| "))
            line = line[3..];

        if (string.IsNullOrWhiteSpace(line))
            return;

        // Suppress noise
        foreach (var pattern in SuppressPatterns)
        {
            if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return;
        }

        // Route through Logger
        if (_isError || MatchesAny(line, WarningPatterns))
        {
            Logger.Warning("Electron", line, logToConsole: false);
        }
        else if (MatchesAny(line, DebugPatterns))
        {
            Logger.Debug("Electron", line);
        }
        else
        {
            Logger.Info("Electron", line, logToConsole: false);
        }
    }

    public override void Write(string? value)
    {
        // Electron.NET framework uses WriteLine predominantly;
        // buffer partial writes for a complete line
        if (!string.IsNullOrEmpty(value))
            WriteLine(value);
    }

    private static bool MatchesAny(string line, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
