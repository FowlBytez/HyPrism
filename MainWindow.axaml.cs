using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaWebView;
using HyPrism.Backend;

namespace HyPrism;

public partial class MainWindow : Window
{
    private static HttpListener? _server;
    private static int _port = 49152;
    private static string _wwwroot = "";
    private readonly AppService _app;
    private IWebView? _webView;
    
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
        
        // Initialize WebView
        InitializeWebView();
        
        // Set main window reference for game state events
        _app.SetMainWindow(this);
        
        Logger.Success("HyPrism", "Launcher started");
        
        // Check for updates after window loads (async, non-blocking)
        Task.Run(async () =>
        {
            await Task.Delay(2000); // Wait for UI to load
            await _app.CheckForLauncherUpdatesAsync();
        });
    }
    
    private void InitializeWebView()
    {
        try
        {
            // Create WebView using AvaloniaWebView
            _webView = WebViewBuilder.Initialize(opt =>
            {
                opt.IsDevToolEnabled = true;
                opt.DefaultContextMenuEnabled = false;
            }).AsControl();
            
            if (_webView is Control webViewControl)
            {
                // Add WebView to the grid
                var grid = this.FindControl<Grid>("RootGrid");
                if (grid != null)
                {
                    grid.Children.Add(webViewControl);
                }
                
                // Subscribe to WebMessage events
                _webView.WebMessageReceived += OnWebMessageReceived;
                
                // Load the page
                _webView.Navigate(new Uri($"http://localhost:{_port}/index.html"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error("WebView", $"Failed to initialize: {ex.Message}");
        }
    }
    
    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessage.Data;
            if (string.IsNullOrEmpty(message)) return;
            
            HandleMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Error("WebView", $"Message handling error: {ex.Message}");
        }
    }
    
    public void SendWebMessage(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                _webView?.PostWebMessage(message);
            }
            catch (Exception ex)
            {
                Logger.Error("WebView", $"Failed to send message: {ex.Message}");
            }
        });
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
        try
        {
            var request = JsonSerializer.Deserialize<RpcRequest>(message);
            if (request == null) return;
            
            Task.Run(async () =>
            {
                object? result = null;
                string? error = null;
                
                try
                {
                    result = await ExecuteRpcMethod(request);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    Logger.Error("RPC", $"{request.Method}: {ex.Message}");
                }
                
                var response = new RpcResponse
                {
                    Id = request.Id,
                    Result = result,
                    Error = error
                };
                
                var json = JsonSerializer.Serialize(response, JsonOptions);
                SendWebMessage(json);
            });
        }
        catch (Exception ex)
        {
            Logger.Error("RPC", $"Message parse error: {ex.Message}");
        }
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
            "DownloadAndInstallAssetsZip" => await _app.DownloadAndInstallAssetsZipAsync(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            
            // Avatar & Skin
            "SaveAvatar" => _app.SaveAvatar(GetArg<string>(request.Args, 0), GetArg<bool>(request.Args, 1)),
            "GetAvatar" => _app.GetAvatar(),
            "SetSkin" => _app.SetSkin(GetArg<string>(request.Args, 0)),
            "GetSkin" => _app.GetSkin(),
            
            // Game
            "DownloadAndLaunch" => await _app.DownloadAndLaunchAsync(this),
            "CancelDownload" => _app.CancelDownload(),
            "KillGameProcess" => _app.KillGameProcess(),
            "IsGameRunning" => _app.IsGameRunning(),
            "GetRecentLogs" => _app.GetRecentLogs(GetArg<int>(request.Args, 0)),
            "ExitGame" => _app.ExitGame(),
            "DeleteGame" => _app.DeleteGame(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            
            // Multiplayer & News
            "GetNews" => await _app.GetNewsAsync(GetArg<int>(request.Args, 0)),
            "GetServerList" => await _app.GetServerListAsync(GetArg<bool>(request.Args, 0)),
            "AddServerToFavorites" => _app.AddServerToFavorites(GetArg<string>(request.Args, 0)),
            "RemoveServerFromFavorites" => _app.RemoveServerFromFavorites(GetArg<string>(request.Args, 0)),
            "ToggleServerFavorite" => _app.ToggleServerFavorite(GetArg<string>(request.Args, 0)),
            "GetFavoriteServers" => _app.GetFavoriteServers(),
            
            // Mods
            "GetInstalledMods" => _app.GetInstalledMods(),
            "SetModEnabled" => _app.SetModEnabled(GetArg<string>(request.Args, 0), GetArg<bool>(request.Args, 1)),
            "UninstallMod" => _app.UninstallMod(GetArg<string>(request.Args, 0)),
            "GetAvailableMods" => await _app.GetAvailableModsAsync(),
            "InstallMod" => await _app.InstallModAsync(GetArg<string>(request.Args, 0)),
            "UpdateMod" => await _app.UpdateModAsync(GetArg<string>(request.Args, 0)),
            "GetModInfo" => await _app.GetModInfoAsync(GetArg<string>(request.Args, 0)),
            "OpenModsFolder" => _app.OpenModsFolder(),
            "SearchMods" => await _app.SearchModsAsync(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            "GetModFiles" => await _app.GetModFilesAsync(GetArg<string>(request.Args, 0)),
            "GetModCategories" => await _app.GetModCategoriesAsync(),
            "GetInstanceInstalledMods" => _app.GetInstanceInstalledMods(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1)),
            "InstallModFileToInstance" => await _app.InstallModFileToInstanceAsync(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1), GetArg<int>(request.Args, 2)),
            "UninstallInstanceMod" => _app.UninstallInstanceMod(GetArg<string>(request.Args, 0), GetArg<int>(request.Args, 1), GetArg<string>(request.Args, 2)),
            
            // JVM Settings
            "GetJavaMemory" => _app.GetJavaMemory(),
            "SetJavaMemory" => _app.SetJavaMemory(GetArg<int>(request.Args, 0)),
            "GetJavaPath" => _app.GetJavaPath(),
            "SetJavaPath" => _app.SetJavaPath(GetArg<string>(request.Args, 0)),
            "GetJavaArgs" => _app.GetJavaArgs(),
            "SetJavaArgs" => _app.SetJavaArgs(GetArg<string>(request.Args, 0)),
            "DetectJavaPath" => _app.DetectJavaPath(),
            
            // Game Settings
            "GetGameWidth" => _app.GetGameWidth(),
            "SetGameWidth" => _app.SetGameWidth(GetArg<int>(request.Args, 0)),
            "GetGameHeight" => _app.GetGameHeight(),
            "SetGameHeight" => _app.SetGameHeight(GetArg<int>(request.Args, 0)),
            "GetFullscreen" => _app.GetFullscreen(),
            "SetFullscreen" => _app.SetFullscreen(GetArg<bool>(request.Args, 0)),
            "GetGameArgs" => _app.GetGameArgs(),
            "SetGameArgs" => _app.SetGameArgs(GetArg<string>(request.Args, 0)),
            
            // Launcher Settings
            "GetShowSnapshots" => _app.GetShowSnapshots(),
            "SetShowSnapshots" => _app.SetShowSnapshots(GetArg<bool>(request.Args, 0)),
            "GetKeepLauncherOpen" => _app.GetKeepLauncherOpen(),
            "SetKeepLauncherOpen" => _app.SetKeepLauncherOpen(GetArg<bool>(request.Args, 0)),
            "GetAutoUpdate" => _app.GetAutoUpdate(),
            "SetAutoUpdate" => _app.SetAutoUpdate(GetArg<bool>(request.Args, 0)),
            "GetDiscordRichPresence" => _app.GetDiscordRichPresence(),
            "SetDiscordRichPresence" => _app.SetDiscordRichPresence(GetArg<bool>(request.Args, 0)),
            "GetSkinProtection" => _app.GetSkinProtection(),
            "SetSkinProtection" => _app.SetSkinProtection(GetArg<bool>(request.Args, 0)),
            "GetMusicEnabled" => _app.GetMusicEnabled(),
            "SetMusicEnabled" => _app.SetMusicEnabled(GetArg<bool>(request.Args, 0)),
            
            // Utility
            "OpenFolder" => _app.OpenFolder(),
            "SelectInstanceDirectory" => await _app.SelectInstanceDirectoryAsync(),
            "BrowseFolder" => await _app.BrowseFolderAsync(GetArg<string?>(request.Args, 0)),
            "BrowseModFiles" => await _app.BrowseModFilesAsync(),
            "BrowserOpenURL" => _app.BrowserOpenURL(GetArg<string>(request.Args, 0)),
            "GetSystemInfo" => _app.GetSystemInfo(),
            "ClearCache" => _app.ClearCache(),
            "ExportLogs" => _app.ExportLogs(),
            "Update" => await _app.UpdateAsync(request.Args),
            
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
