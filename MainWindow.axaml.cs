using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using HyPrism.Backend;

namespace HyPrism;

public partial class MainWindow : Window
{
    private static HttpListener? _server;
    private static int _port = 49152;
    private static string _wwwroot = "";
    private readonly AppService _app;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize backend services
        _app = new AppService();
        
        // Get the wwwroot directory
        _wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(_wwwroot))
        {
            // macOS app bundle fallback: Contents/Resources/wwwroot
            var resourcesWwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "wwwroot"));
            if (Directory.Exists(resourcesWwwroot))
            {
                _wwwroot = resourcesWwwroot;
            }
        }
        
        // Start local HTTP server for serving static files
        StartLocalServer();
        
        // Set main window reference for game state events
        _app.SetMainWindow(this);
        
        Logger.Success("HyPrism", "Launcher started");
        
        // Check for updates after window loads (async, non-blocking)
        Task.Run(async () =>
        {
            await Task.Delay(2000); // Wait for UI to load
            await _app.CheckForLauncherUpdatesAsync();
        });
        
        // Open browser to the local server
        Task.Run(async () =>
        {
            await Task.Delay(500);
            OpenBrowser($"http://localhost:{_port}/index.html");
        });
    }
    
    private void OpenBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Browser", $"Failed to open: {ex.Message}");
        }
    }
    
    public void SendWebMessage(string message)
    {
        // For now, just log - we'll implement WebSocket or similar later
        Logger.Info("WebMessage", $"Would send: {message.Substring(0, Math.Min(100, message.Length))}...");
    }
    
    static void StartLocalServer()
    {
        // Find an available port
        for (int i = 0; i < 100; i++)
        {
            try
            {
                _server = new HttpListener();
                _server.Prefixes.Add($"http://localhost:{_port}/");
                _server.Start();
                Logger.Info("Server", $"Started on port {_port}");
                
                // Handle requests in background
                Task.Run(() => HandleRequests());
                return;
            }
            catch
            {
                _port++;
            }
        }
        throw new Exception("Could not start local server");
    }
    
    static void HandleRequests()
    {
        while (_server?.IsListening == true)
        {
            try
            {
                var context = _server.GetContext();
                Task.Run(() => ProcessRequest(context));
            }
            catch
            {
                // Server stopped
                break;
            }
        }
    }
    
    static void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.LocalPath ?? "/";
            if (path == "/") path = "/index.html";
            
            var filePath = Path.Combine(_wwwroot, path.TrimStart('/'));
            
            if (File.Exists(filePath))
            {
                var extension = Path.GetExtension(filePath).ToLower();
                context.Response.ContentType = extension switch
                {
                    ".html" => "text/html",
                    ".css" => "text/css",
                    ".js" => "application/javascript",
                    ".json" => "application/json",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".svg" => "image/svg+xml",
                    ".ogg" => "audio/ogg",
                    ".mp3" => "audio/mpeg",
                    ".woff" => "font/woff",
                    ".woff2" => "font/woff2",
                    _ => "application/octet-stream"
                };
                
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                context.Response.StatusCode = 200;
                
                var bytes = File.ReadAllBytes(filePath);
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Server", $"Request error: {ex.Message}");
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }
    
    private void HandleMessage(string message)
    {
        // Temporarily disabled - will implement WebSocket bridge
        Logger.Info("RPC", $"Received message: {message.Substring(0, Math.Min(100, message.Length))}...");
    }
    
    private async Task<object?> ExecuteRpcMethod(RpcRequest request)
    {
        return request.Method switch
        {
            // User/Config
            "QueryConfig" => _app.QueryConfig(),
            "GetNick" => _app.GetNick(),
            "SetNick" => _app.SetNick(GetArg<string>(request.Args, 0)),
            "GetLauncherVersion" => _app.GetLauncherVersion(),
            "GetUUID" => _app.GetUUID(),
            "SetUUID" => _app.SetUUID(GetArg<string>(request.Args, 0)),
            "GetAvatarPreview" => _app.GetAvatarPreview(),
            "GetAvatarPreviewForUUID" => _app.GetAvatarPreviewForUUID(GetArg<string>(request.Args, 0)),
            "ClearAvatarCache" => _app.ClearAvatarCache(),
            
            // Profile Management
            "GetProfiles" => _app.GetProfiles(),
            "GetActiveProfileIndex" => _app.GetActiveProfileIndex(),
            "CreateProfile" => _app.CreateProfile(GetArg<string>(request.Args, 0), GetArg<string>(request.Args, 1)),
            "DeleteProfile" => _app.DeleteProfile(GetArg<string>(request.Args, 0)),
            "SwitchProfile" => _app.SwitchProfile(GetArg<int>(request.Args, 0)),
            "UpdateProfile" => _app.UpdateProfile(GetArg<string>(request.Args, 0), GetArg<string>(request.Args, 1), GetArg<string>(request.Args, 2)),
            "SaveCurrentAsProfile" => _app.SaveCurrentAsProfile(),
            
            "GetCustomInstanceDir" => _app.GetCustomInstanceDir(),
            "SetInstanceDirectory" => await _app.SetInstanceDirectoryAsync(GetArg<string>(request.Args, 0)),
            
            // Version Management
            "GetVersionType" => _app.GetVersionType(),
            "SetVersionType" => _app.SetVersionType(GetArg<string>(request.Args, 0)),
            "GetVersionList" => await _app.GetVersionListAsync(GetArg<string>(request.Args, 0)),
            "SetSelectedVersion" => _app.SetSelectedVersion(GetArg<int>(request.Args, 0)),
            "IsVersionInstalled" => _app.IsVersionInstalled(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            "GetInstalledVersionsForBranch" => _app.GetInstalledVersionsForBranch(GetArg<string>(request.Args, 0)),
            "CheckLatestNeedsUpdate" => await _app.CheckLatestNeedsUpdateAsync(GetArg<string>(request.Args, 0)),
            "GetPendingUpdateInfo" => await _app.GetPendingUpdateInfoAsync(GetArg<string>(request.Args, 0)),
            "CopyUserData" => await _app.CopyUserDataAsync(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1), GetArg<int>(request.Args, 2)),
            
            // Assets
            "HasAssetsZip" => _app.HasAssetsZip(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            "GetAssetsZipPath" => _app.GetAssetsZipPath(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            
            // Game
            "DownloadAndLaunch" => await _app.DownloadAndLaunchAsync(this),
            "CancelDownload" => _app.CancelDownload(),
            "IsGameRunning" => _app.IsGameRunning(),
            "GetRecentLogs" => _app.GetRecentLogs(GetArg<int>(request.Args, 0)),
            "ExitGame" => _app.ExitGame(),
            "DeleteGame" => _app.DeleteGame(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            
            // Multiplayer & News
            "GetNews" => await _app.GetNewsAsync(GetArg<int>(request.Args, 0)),
            
            // Launcher Settings
            "GetMusicEnabled" => _app.GetMusicEnabled(),
            "SetMusicEnabled" => _app.SetMusicEnabled(GetArg<bool>(request.Args, 0)),
            "GetShowDiscordAnnouncements" => _app.GetShowDiscordAnnouncements(),
            "SetShowDiscordAnnouncements" => _app.SetShowDiscordAnnouncements(GetArg<bool>(request.Args, 0)),
            
            // Utility
            "OpenFolder" => _app.OpenFolder(),
            "SelectInstanceDirectory" => await _app.SelectInstanceDirectoryAsync(),
            "BrowseFolder" => await _app.BrowseFolderAsync(GetArg<string?>(request.Args, 0)),
            "BrowseModFiles" => await _app.BrowseModFilesAsync(),
            "BrowserOpenURL" => _app.BrowserOpenURL(GetArg<string>(request.Args, 0)),
            
            // Window controls
            "SetMinimized" => SetMinimized(GetArg<bool>(request.Args, 0)),
            "ToggleMaximize" => ToggleMaximize(),
            "WindowClose" => CloseWindow(),
            
            _ => throw new NotImplementedException($"Method '{request.Method}' not implemented")
        };
    }
    
    private static T GetArg<T>(object?[]? args, int index)
    {
        if (args == null || args.Length <= index || args[index] == null)
            return default!;
        
        if (args[index] is JsonElement element)
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText())!;
        }
        
        return (T)args[index]!;
    }
    
    bool SetMinimized(bool value)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowState = value ? WindowState.Minimized : WindowState.Normal;
        });
        return true;
    }
    
    bool ToggleMaximize()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        });
        return true;
    }
    
    bool CloseWindow()
    {
        Dispatcher.UIThread.InvokeAsync(() => Close());
        return true;
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _server?.Stop();
        _app?.Dispose();
        base.OnClosed(e);
    }
}

public class RpcRequest
{
    public string? Id { get; set; }
    public string Method { get; set; } = "";
    public object?[]? Args { get; set; }
}

public class RpcResponse
{
    public string? Id { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}
