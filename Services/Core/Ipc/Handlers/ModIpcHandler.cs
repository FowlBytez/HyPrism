using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using HyPrism.Models;
using HyPrism.Services.Game.Instance;
using HyPrism.Services.Game.Mod;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles mod management IPC channels (search, install, toggle, export/import).
/// 
/// @ipc invoke hyprism:mods:list -> InstalledMod[]
/// @ipc invoke hyprism:mods:search -> ModSearchResult 15000
/// @ipc invoke hyprism:mods:installed -> InstalledMod[]
/// @ipc invoke hyprism:mods:uninstall -> boolean
/// @ipc invoke hyprism:mods:checkUpdates -> InstalledMod[] 30000
/// @ipc invoke hyprism:mods:install -> boolean 30000
/// @ipc invoke hyprism:mods:files -> ModFilesResult
/// @ipc invoke hyprism:mods:categories -> ModCategory[]
/// @ipc invoke hyprism:mods:installLocal -> boolean
/// @ipc invoke hyprism:mods:installBase64 -> boolean
/// @ipc send hyprism:mods:openFolder
/// @ipc invoke hyprism:mods:toggle -> boolean
/// </summary>
public class ModIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public ModIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var modService = _services.GetRequiredService<IModService>();
        var instanceService = _services.GetRequiredService<IInstanceService>();
        var config = _services.GetRequiredService<IConfigService>();

        Electron.IpcMain.On("hyprism:mods:list", (_) =>
        {
            try
            {
                var branch = config.Configuration.LauncherBranch ?? "release";
                Reply("hyprism:mods:list:reply", modService.GetInstanceInstalledMods(
                    instanceService.GetLatestInstancePath(branch)));
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods list failed: {ex.Message}");
            }
        });

        Electron.IpcMain.On("hyprism:mods:search", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var query = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
                var page = root.TryGetProperty("page", out var p) ? p.GetInt32() : 0;
                var pageSize = root.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : 20;
                var sortField = root.TryGetProperty("sortField", out var sf) ? sf.GetInt32() : 1;
                var sortOrder = root.TryGetProperty("sortOrder", out var so) ? so.GetInt32() : 1;
                
                var categories = Array.Empty<string>();
                if (root.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
                {
                    categories = cats.EnumerateArray()
                        .Select(c => c.GetString() ?? "")
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToArray();
                }
                
                var result = await modService.SearchModsAsync(query, page, pageSize, categories, sortField, sortOrder);
                Reply("hyprism:mods:search:reply", result);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods search failed: {ex.Message}");
                Reply("hyprism:mods:search:reply", new { mods = new List<object>(), totalCount = 0 });
            }
        });

        // Get installed mods for a specific instance (by branch and version)
        Electron.IpcMain.On("hyprism:mods:installed", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var branch = doc.RootElement.GetProperty("branch").GetString() ?? "release";
                var version = doc.RootElement.GetProperty("version").GetInt32();
                var instancePath = instanceService.GetInstancePath(branch, version);
                
                var mods = modService.GetInstanceInstalledMods(instancePath);
                Reply("hyprism:mods:installed:reply", mods);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods installed failed: {ex.Message}");
                Reply("hyprism:mods:installed:reply", new List<object>());
            }
        });

        // Uninstall a mod from an instance
        Electron.IpcMain.On("hyprism:mods:uninstall", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var modId = doc.RootElement.GetProperty("modId").GetString() ?? "";
                var branch = doc.RootElement.GetProperty("branch").GetString() ?? "release";
                var version = doc.RootElement.GetProperty("version").GetInt32();
                var instancePath = instanceService.GetInstancePath(branch, version);
                
                // Get current mods, remove the one with matching ID, save back
                var mods = modService.GetInstanceInstalledMods(instancePath);
                var modToRemove = mods.FirstOrDefault(m => m.Id == modId || m.Name == modId);
                if (modToRemove != null)
                {
                    mods.Remove(modToRemove);
                    
                    // Delete the actual mod file if it exists
                    if (!string.IsNullOrEmpty(modToRemove.FileName))
                    {
                        var modFilePath = Path.Combine(instancePath, "Client", "mods", modToRemove.FileName);
                        if (File.Exists(modFilePath))
                        {
                            try { File.Delete(modFilePath); }
                            catch (Exception ex) { Logger.Warning("IPC", $"Failed to delete mod file: {ex.Message}"); }
                        }
                    }
                    
                    await modService.SaveInstanceModsAsync(instancePath, mods);
                    Reply("hyprism:mods:uninstall:reply", true);
                }
                else
                {
                    Reply("hyprism:mods:uninstall:reply", false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods uninstall failed: {ex.Message}");
                Reply("hyprism:mods:uninstall:reply", false);
            }
        });

        // Check for mod updates (returns mods that have updates available)
        Electron.IpcMain.On("hyprism:mods:checkUpdates", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var branch = doc.RootElement.GetProperty("branch").GetString() ?? "release";
                var version = doc.RootElement.GetProperty("version").GetInt32();
                var instancePath = instanceService.GetInstancePath(branch, version);
                
                var updates = await modService.CheckInstanceModUpdatesAsync(instancePath);
                Reply("hyprism:mods:checkUpdates:reply", updates);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods check updates failed: {ex.Message}");
                Reply("hyprism:mods:checkUpdates:reply", new List<object>());
            }
        });
        
        // Install a mod from CurseForge by modId and fileId
        Electron.IpcMain.On("hyprism:mods:install", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var modId = root.GetProperty("modId").GetString() ?? "";
                var fileId = root.GetProperty("fileId").GetString() ?? "";
                var branch = root.TryGetProperty("branch", out var b) ? b.GetString() ?? "release" : "release";
                var version = root.TryGetProperty("version", out var v) ? v.GetInt32() : 0;
                
                string instancePath;
                if (version > 0)
                    instancePath = instanceService.GetInstancePath(branch, version);
                else
                    instancePath = instanceService.GetLatestInstancePath(branch);
                
                var success = await modService.InstallModFileToInstanceAsync(modId, fileId, instancePath);
                Reply("hyprism:mods:install:reply", success);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods install failed: {ex.Message}");
                Reply("hyprism:mods:install:reply", false);
            }
        });
        
        // Get available files for a mod
        Electron.IpcMain.On("hyprism:mods:files", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var modId = root.GetProperty("modId").GetString() ?? "";
                var page = root.TryGetProperty("page", out var p) ? p.GetInt32() : 0;
                var pageSize = root.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : 20;
                
                var result = await modService.GetModFilesAsync(modId, page, pageSize);
                Reply("hyprism:mods:files:reply", result);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods files failed: {ex.Message}");
                Reply("hyprism:mods:files:reply", new { files = new List<object>(), totalCount = 0 });
            }
        });
        
        // Get mod categories
        Electron.IpcMain.On("hyprism:mods:categories", async (_) =>
        {
            try
            {
                var categories = await modService.GetModCategoriesAsync();
                Reply("hyprism:mods:categories:reply", categories);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods categories failed: {ex.Message}");
                Reply("hyprism:mods:categories:reply", new List<object>());
            }
        });
        
        // Install mod from local file path
        Electron.IpcMain.On("hyprism:mods:installLocal", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var sourcePath = root.GetProperty("sourcePath").GetString() ?? "";
                var branch = root.TryGetProperty("branch", out var b) ? b.GetString() ?? "release" : "release";
                var version = root.TryGetProperty("version", out var v) ? v.GetInt32() : 0;
                
                string instancePath;
                if (version > 0)
                    instancePath = instanceService.GetInstancePath(branch, version);
                else
                    instancePath = instanceService.GetLatestInstancePath(branch);
                
                var success = await modService.InstallLocalModFile(sourcePath, instancePath);
                Reply("hyprism:mods:installLocal:reply", success);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods install local failed: {ex.Message}");
                Reply("hyprism:mods:installLocal:reply", false);
            }
        });
        
        // Install mod from base64-encoded content
        Electron.IpcMain.On("hyprism:mods:installBase64", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var fileName = root.GetProperty("fileName").GetString() ?? "";
                var base64Content = root.GetProperty("base64Content").GetString() ?? "";
                var branch = root.TryGetProperty("branch", out var b) ? b.GetString() ?? "release" : "release";
                var version = root.TryGetProperty("version", out var v) ? v.GetInt32() : 0;
                
                string instancePath;
                if (version > 0)
                    instancePath = instanceService.GetInstancePath(branch, version);
                else
                    instancePath = instanceService.GetLatestInstancePath(branch);
                
                var success = await modService.InstallModFromBase64(fileName, base64Content, instancePath);
                Reply("hyprism:mods:installBase64:reply", success);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods install base64 failed: {ex.Message}");
                Reply("hyprism:mods:installBase64:reply", false);
            }
        });
        
        // Open the mods folder for an instance
        Electron.IpcMain.On("hyprism:mods:openFolder", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var branch = root.TryGetProperty("branch", out var b) ? b.GetString() ?? "release" : "release";
                var version = root.TryGetProperty("version", out var v) ? v.GetInt32() : 0;
                
                string instancePath;
                if (version > 0)
                    instancePath = instanceService.GetInstancePath(branch, version);
                else
                    instancePath = instanceService.GetLatestInstancePath(branch);
                
                var modsPath = Path.Combine(instancePath, "Client", "mods");
                Directory.CreateDirectory(modsPath);
                Electron.Shell.OpenPathAsync(modsPath);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Open mods folder failed: {ex.Message}");
            }
        });
        
        // Toggle mod enabled/disabled (renames .jar <-> .jar.disabled)
        Electron.IpcMain.On("hyprism:mods:toggle", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var modId = root.GetProperty("modId").GetString() ?? "";
                var branch = root.GetProperty("branch").GetString() ?? "release";
                var version = root.GetProperty("version").GetInt32();
                var instancePath = instanceService.GetInstancePath(branch, version);
                
                var mods = modService.GetInstanceInstalledMods(instancePath);
                var mod = mods.FirstOrDefault(m => m.Id == modId || m.Name == modId);
                if (mod == null || string.IsNullOrEmpty(mod.FileName))
                {
                    Reply("hyprism:mods:toggle:reply", false);
                    return;
                }
                
                var modsDir = Path.Combine(instancePath, "Client", "mods");
                var currentPath = Path.Combine(modsDir, mod.FileName);
                
                if (mod.Enabled)
                {
                    // Disable: rename file.jar -> file.jar.disabled
                    var disabledPath = currentPath + ".disabled";
                    if (File.Exists(currentPath))
                    {
                        File.Move(currentPath, disabledPath);
                        mod.FileName = mod.FileName + ".disabled";
                        mod.Enabled = false;
                        Logger.Info("IPC", $"Disabled mod: {mod.Name}");
                    }
                }
                else
                {
                    // Enable: rename file.jar.disabled -> file.jar
                    if (mod.FileName.EndsWith(".disabled") && File.Exists(currentPath))
                    {
                        var enabledPath = currentPath[..^".disabled".Length];
                        File.Move(currentPath, enabledPath);
                        mod.FileName = mod.FileName[..^".disabled".Length];
                        mod.Enabled = true;
                        Logger.Info("IPC", $"Enabled mod: {mod.Name}");
                    }
                }
                
                await modService.SaveInstanceModsAsync(instancePath, mods);
                Reply("hyprism:mods:toggle:reply", true);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Mods toggle failed: {ex.Message}");
                Reply("hyprism:mods:toggle:reply", false);
            }
        });
    }
}
