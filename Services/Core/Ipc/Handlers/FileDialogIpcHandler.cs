using System.IO.Compression;
using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using HyPrism.Models;
using HyPrism.Services.Game.Instance;
using HyPrism.Services.Game.Mod;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles file dialog and file system IPC channels.
/// 
/// @ipc invoke hyprism:file:browseFolder -> string | null
/// @ipc invoke hyprism:file:browseModFiles -> string[]
/// @ipc invoke hyprism:mods:exportToFolder -> string
/// @ipc invoke hyprism:mods:importList -> number
/// @ipc invoke hyprism:settings:launcherPath -> string
/// @ipc invoke hyprism:settings:defaultInstanceDir -> string
/// @ipc invoke hyprism:settings:setInstanceDir -> { success: boolean, path: string }
/// @ipc invoke hyprism:settings:setLauncherDataDir -> { success: boolean }
/// </summary>
public class FileDialogIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public FileDialogIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var fileDialog = _services.GetRequiredService<IFileDialogService>();
        var appPath = _services.GetRequiredService<AppPathConfiguration>();
        var config = _services.GetRequiredService<IConfigService>();
        var instanceService = _services.GetRequiredService<IInstanceService>();
        var modService = _services.GetRequiredService<IModService>();

        // Browse mod files dialog (jar, zip, json)
        Electron.IpcMain.On("hyprism:file:browseModFiles", async (_) =>
        {
            try
            {
                var files = await fileDialog.BrowseModFilesAsync();
                Reply("hyprism:file:browseModFiles:reply", files ?? Array.Empty<string>());
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to browse mod files: {ex.Message}");
                Reply("hyprism:file:browseModFiles:reply", Array.Empty<string>());
            }
        });

        // Export mods to folder (modlist JSON or zip)
        Electron.IpcMain.On("hyprism:mods:exportToFolder", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var branch = root.GetProperty("branch").GetString() ?? "release";
                var version = root.GetProperty("version").GetInt32();
                var exportPath = root.GetProperty("exportPath").GetString() ?? "";
                var exportType = root.TryGetProperty("exportType", out var et) ? et.GetString() ?? "modlist" : "modlist";

                if (string.IsNullOrEmpty(exportPath))
                {
                    Reply("hyprism:mods:exportToFolder:reply", "");
                    return;
                }

                var instancePath = instanceService.GetInstancePath(branch, version);
                var mods = modService.GetInstanceInstalledMods(instancePath);

                if (mods.Count == 0)
                {
                    Reply("hyprism:mods:exportToFolder:reply", "");
                    return;
                }

                // Save last export path to config
                config.Configuration.LastExportPath = exportPath;
                config.SaveConfig();

                if (exportType == "zip")
                {
                    // Zip the mods folder
                    var modsDir = Path.Combine(instancePath, "Client", "mods");
                    if (!Directory.Exists(modsDir))
                    {
                        Reply("hyprism:mods:exportToFolder:reply", "");
                        return;
                    }

                    var zipName = $"HyPrism-Mods-{branch}-v{version}-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
                    var zipPath = Path.Combine(exportPath, zipName);
                    ZipFile.CreateFromDirectory(modsDir, zipPath);
                    Logger.Success("IPC", $"Exported mods zip to: {zipPath}");
                    Reply("hyprism:mods:exportToFolder:reply", zipPath);
                }
                else
                {
                    // Export as mod list JSON
                    var modList = mods
                        .Where(m => !string.IsNullOrEmpty(m.CurseForgeId))
                        .Select(m => new ModListEntry
                        {
                            CurseForgeId = m.CurseForgeId,
                            FileId = m.FileId,
                            Name = m.Name,
                            Version = m.Version
                        })
                        .ToList();

                    if (modList.Count == 0)
                    {
                        Reply("hyprism:mods:exportToFolder:reply", "");
                        return;
                    }

                    var fileName = $"HyPrism-ModList-{branch}-v{version}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                    var filePath = Path.Combine(exportPath, fileName);
                    var jsonContent = JsonSerializer.Serialize(modList, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, jsonContent);
                    Logger.Success("IPC", $"Exported mod list to: {filePath}");
                    Reply("hyprism:mods:exportToFolder:reply", filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to export mods: {ex.Message}");
                Reply("hyprism:mods:exportToFolder:reply", "");
            }
        });

        // Import mod list from JSON file
        Electron.IpcMain.On("hyprism:mods:importList", async (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var filePath = root.GetProperty("filePath").GetString() ?? "";
                var branch = root.GetProperty("branch").GetString() ?? "release";
                var version = root.GetProperty("version").GetInt32();

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Reply("hyprism:mods:importList:reply", 0);
                    return;
                }

                var instancePath = instanceService.GetInstancePath(branch, version);
                var content = await File.ReadAllTextAsync(filePath);
                var modList = JsonSerializer.Deserialize<List<ModListEntry>>(content) ?? new();
                var successCount = 0;

                foreach (var entry in modList)
                {
                    if (string.IsNullOrEmpty(entry.CurseForgeId)) continue;
                    try
                    {
                        var fileId = entry.FileId ?? "";
                        var success = await modService.InstallModFileToInstanceAsync(entry.CurseForgeId, fileId, instancePath);
                        if (success) successCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("IPC", $"Failed to import mod {entry.Name}: {ex.Message}");
                    }
                }

                Logger.Success("IPC", $"Imported {successCount}/{modList.Count} mods from list");
                Reply("hyprism:mods:importList:reply", successCount);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to import mod list: {ex.Message}");
                Reply("hyprism:mods:importList:reply", 0);
            }
        });

        // Browse folder dialog
        Electron.IpcMain.On("hyprism:file:browseFolder", async (args) =>
        {
            try
            {
                var initialPath = ArgsToString(args);
                var selected = await fileDialog.BrowseFolderAsync(string.IsNullOrEmpty(initialPath) ? null : initialPath);
                Reply("hyprism:file:browseFolder:reply", selected ?? "");
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to browse folder: {ex.Message}");
                Reply("hyprism:file:browseFolder:reply", "");
            }
        });

        // Get launcher folder path (app data path)
        Electron.IpcMain.On("hyprism:settings:launcherPath", (_) =>
        {
            Reply("hyprism:settings:launcherPath:reply", appPath.AppDir);
        });

        // Get default instance directory
        Electron.IpcMain.On("hyprism:settings:defaultInstanceDir", (_) =>
        {
            var defaultDir = Path.Combine(appPath.AppDir, "game");
            Reply("hyprism:settings:defaultInstanceDir:reply", defaultDir);
        });
        
        // Set instance directory
        Electron.IpcMain.On("hyprism:settings:setInstanceDir", async (args) =>
        {
            try
            {
                var path = ArgsToString(args);
                var result = await config.SetInstanceDirectoryAsync(path);
                Reply("hyprism:settings:setInstanceDir:reply", new { success = result != null, path = result ?? "" });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to set instance directory: {ex.Message}");
                Reply("hyprism:settings:setInstanceDir:reply", new { success = false, path = "" });
            }
        });
        
        // Set launcher data directory (in config)
        Electron.IpcMain.On("hyprism:settings:setLauncherDataDir", (args) =>
        {
            try
            {
                var path = ArgsToString(args);
                var cfg = config.Configuration;
                cfg.LauncherDataDirectory = path;
                config.SaveConfig();
                Reply("hyprism:settings:setLauncherDataDir:reply", new { success = true });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to set launcher data directory: {ex.Message}");
                Reply("hyprism:settings:setLauncherDataDir:reply", new { success = false, error = ex.Message });
            }
        });
    }
}
