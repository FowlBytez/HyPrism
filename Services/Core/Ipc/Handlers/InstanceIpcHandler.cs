using System.IO.Compression;
using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using HyPrism.Services.Game.Instance;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles instance management IPC channels (create, delete, saves, folders).
/// 
/// @ipc invoke hyprism:instance:create -> InstanceInfo | null
/// @ipc invoke hyprism:instance:delete -> boolean
/// @ipc send hyprism:instance:openFolder
/// @ipc send hyprism:instance:openModsFolder
/// @ipc invoke hyprism:instance:export -> string
/// @ipc invoke hyprism:instance:import -> boolean
/// @ipc invoke hyprism:instance:saves -> SaveInfo[]
/// @ipc send hyprism:instance:openSaveFolder
/// @ipc invoke hyprism:instance:getIcon -> string | null
/// @ipc invoke hyprism:instance:setIcon -> boolean
/// @ipc invoke hyprism:instance:rename -> boolean
/// @ipc invoke hyprism:instance:select -> boolean
/// @ipc invoke hyprism:instance:getSelected -> InstanceInfo | null
/// @ipc invoke hyprism:instance:list -> InstanceInfo[]
/// </summary>
public class InstanceIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public InstanceIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var instanceService = _services.GetRequiredService<IInstanceService>();
        var fileService = _services.GetRequiredService<IFileService>();

        // Create an instance with generated ID
        Electron.IpcMain.On("hyprism:instance:create", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                var customName = data?.ContainsKey("customName") == true ? data["customName"].GetString() : null;
                var isLatest = data?.ContainsKey("isLatest") == true && data["isLatest"].GetBoolean();

                // Create the instance with generated ID
                var meta = instanceService.CreateInstanceMeta(branch, version, customName, isLatest);
                
                Logger.Success("IPC", $"Created instance {meta.Id} ({meta.Name})");
                Reply("hyprism:instance:create:reply", new {
                    id = meta.Id,
                    name = meta.Name,
                    branch = meta.Branch,
                    version = meta.Version
                });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to create instance: {ex.Message}");
                Reply("hyprism:instance:create:reply", null);
            }
        });

        // Select an instance by ID
        Electron.IpcMain.On("hyprism:instance:select", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var instanceId = data?["id"].GetString() ?? "";
                
                if (string.IsNullOrEmpty(instanceId))
                {
                    Reply("hyprism:instance:select:reply", false);
                    return;
                }
                
                instanceService.SetSelectedInstance(instanceId);
                Logger.Info("IPC", $"Selected instance: {instanceId}");
                Reply("hyprism:instance:select:reply", true);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to select instance: {ex.Message}");
                Reply("hyprism:instance:select:reply", false);
            }
        });

        // Get selected instance
        Electron.IpcMain.On("hyprism:instance:getSelected", (_) =>
        {
            try
            {
                var selected = instanceService.GetSelectedInstance();
                Reply("hyprism:instance:getSelected:reply", selected != null ? new {
                    id = selected.Id,
                    name = selected.Name,
                    branch = selected.Branch,
                    version = selected.Version,
                    path = selected.Path
                } : null);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get selected instance: {ex.Message}");
                Reply("hyprism:instance:getSelected:reply", null);
            }
        });

        // List all instances from config
        Electron.IpcMain.On("hyprism:instance:list", (_) =>
        {
            try
            {
                instanceService.SyncInstancesWithConfig();
                var config = _services.GetRequiredService<IConfigService>().Configuration;
                var instances = config.Instances?.Select(i => (object)new {
                    id = i.Id,
                    name = i.Name,
                    branch = i.Branch,
                    version = i.Version,
                    path = i.Path
                }).ToList() ?? new List<object>();
                
                Reply("hyprism:instance:list:reply", instances);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to list instances: {ex.Message}");
                Reply("hyprism:instance:list:reply", new List<object>());
            }
        });

        // Delete an instance
        Electron.IpcMain.On("hyprism:instance:delete", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                
                var result = instanceService.DeleteGame(branch, version);
                Logger.Info("IPC", $"Deleted instance {branch}/{version}: {result}");
                Reply("hyprism:instance:delete:reply", result);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to delete instance: {ex.Message}");
                Reply("hyprism:instance:delete:reply", false);
            }
        });

        // Open instance folder
        Electron.IpcMain.On("hyprism:instance:openFolder", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                
                var path = instanceService.GetInstancePath(branch, version);
                if (Directory.Exists(path))
                {
                    fileService.OpenFolder(path);
                    Logger.Info("IPC", $"Opened folder: {path}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to open instance folder: {ex.Message}");
            }
        });

        // Open mods folder
        Electron.IpcMain.On("hyprism:instance:openModsFolder", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                
                var instancePath = instanceService.GetInstancePath(branch, version);
                var modsPath = Path.Combine(instancePath, "Client", "mods");
                
                if (!Directory.Exists(modsPath))
                {
                    Directory.CreateDirectory(modsPath);
                }
                
                fileService.OpenFolder(modsPath);
                Logger.Info("IPC", $"Opened mods folder: {modsPath}");
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to open mods folder: {ex.Message}");
            }
        });

        // Export instance as zip
        Electron.IpcMain.On("hyprism:instance:export", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                
                var instancePath = instanceService.GetInstancePath(branch, version);
                if (!Directory.Exists(instancePath))
                {
                    Reply("hyprism:instance:export:reply", "");
                    return;
                }

                // Export to desktop by default
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filename = $"HyPrism-{branch}-v{version}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                var savePath = Path.Combine(desktop, filename);

                // Create zip
                if (File.Exists(savePath)) File.Delete(savePath);
                ZipFile.CreateFromDirectory(instancePath, savePath, CompressionLevel.Optimal, false);
                
                Logger.Success("IPC", $"Exported instance to: {savePath}");
                Reply("hyprism:instance:export:reply", savePath);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to export instance: {ex.Message}");
                Reply("hyprism:instance:export:reply", "");
            }
        });

        // Import instance from zip (using file dialog service)
        Electron.IpcMain.On("hyprism:instance:import", async (_) =>
        {
            try
            {
                // For now, return false - import should be triggered from frontend with file picker
                Logger.Info("IPC", "Import triggered - frontend should use file picker");
                Reply("hyprism:instance:import:reply", false);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to import instance: {ex.Message}");
                Reply("hyprism:instance:import:reply", false);
            }
        });

        // Get saves for an instance
        Electron.IpcMain.On("hyprism:instance:saves", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                
                var instancePath = instanceService.GetInstancePath(branch, version);
                var savesPath = Path.Combine(instancePath, "UserData", "Saves");
                
                var saves = new List<object>();
                
                if (Directory.Exists(savesPath))
                {
                    foreach (var saveDir in Directory.GetDirectories(savesPath))
                    {
                        var dirInfo = new DirectoryInfo(saveDir);
                        var previewPath = Path.Combine(saveDir, "preview.png");
                        
                        // Calculate total size
                        long sizeBytes = 0;
                        try
                        {
                            sizeBytes = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                        }
                        catch { /* ignore */ }

                        saves.Add(new
                        {
                            name = dirInfo.Name,
                            path = saveDir,
                            previewPath = File.Exists(previewPath) ? $"file://{previewPath.Replace("\\", "/")}" : null,
                            lastModified = dirInfo.LastWriteTime.ToString("o"),
                            sizeBytes
                        });
                    }
                }
                
                Logger.Info("IPC", $"Found {saves.Count} saves for {branch}/{version}");
                Reply("hyprism:instance:saves:reply", saves);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get saves: {ex.Message}");
                Reply("hyprism:instance:saves:reply", new List<object>());
            }
        });

        // Open save folder
        Electron.IpcMain.On("hyprism:instance:openSaveFolder", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                var saveName = data?["saveName"].GetString() ?? "";
                
                var instancePath = instanceService.GetInstancePath(branch, version);
                var savePath = Path.Combine(instancePath, "UserData", "Saves", saveName);
                
                if (Directory.Exists(savePath))
                {
                    fileService.OpenFolder(savePath);
                    Logger.Info("IPC", $"Opened save folder: {savePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to open save folder: {ex.Message}");
            }
        });

        // Get instance icon
        Electron.IpcMain.On("hyprism:instance:getIcon", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                
                var instancePath = instanceService.GetInstancePath(branch, version);
                var iconPath = Path.Combine(instancePath, "icon.png");
                
                if (File.Exists(iconPath))
                {
                    Reply("hyprism:instance:getIcon:reply", $"file://{iconPath.Replace("\\", "/")}");
                }
                else
                {
                    Reply("hyprism:instance:getIcon:reply", null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get instance icon: {ex.Message}");
                Reply("hyprism:instance:getIcon:reply", null);
            }
        });

        // Set instance icon
        Electron.IpcMain.On("hyprism:instance:setIcon", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                var iconPath = data?["iconPath"].GetString();
                
                var instancePath = instanceService.GetInstancePath(branch, version);
                var targetIconPath = Path.Combine(instancePath, "icon.png");
                
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    File.Copy(iconPath, targetIconPath, true);
                    Reply("hyprism:instance:setIcon:reply", true);
                    Logger.Info("IPC", $"Set icon for {branch}/{version}");
                }
                else
                {
                    // Icon path not provided or invalid
                    Reply("hyprism:instance:setIcon:reply", false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to set instance icon: {ex.Message}");
                Reply("hyprism:instance:setIcon:reply", false);
            }
        });

        // Rename instance (set custom name)
        Electron.IpcMain.On("hyprism:instance:rename", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                var branch = data?["branch"].GetString() ?? "release";
                var version = data?["version"].GetInt32() ?? 0;
                var customName = data?["customName"].GetString();
                
                instanceService.SetInstanceCustomName(branch, version, customName);
                Reply("hyprism:instance:rename:reply", true);
                Logger.Info("IPC", $"Renamed instance {branch}/{version} to: {customName ?? "(default)"}");
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to rename instance: {ex.Message}");
                Reply("hyprism:instance:rename:reply", false);
            }
        });
    }
}
