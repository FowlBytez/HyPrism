using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using HyPrism.Services.Game;
using HyPrism.Services.Game.Instance;
using HyPrism.Services.Game.Launch;
using HyPrism.Services.Game.Version;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles game session IPC channels (launch, cancel, versions, running state).
/// 
/// @ipc send hyprism:game:launch
/// @ipc send hyprism:game:cancel
/// @ipc invoke hyprism:game:instances -> InstalledInstance[]
/// @ipc invoke hyprism:game:isRunning -> boolean
/// @ipc invoke hyprism:game:versions -> number[]
/// @ipc invoke hyprism:game:versionsWithSources -> VersionListResponse
/// @ipc event hyprism:game:progress -> ProgressUpdate
/// @ipc event hyprism:game:state -> GameState
/// @ipc event hyprism:game:error -> GameError
/// </summary>
public class GameIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public GameIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var gameSession = _services.GetRequiredService<IGameSessionService>();
        var progressService = _services.GetRequiredService<ProgressNotificationService>();
        var instanceService = _services.GetRequiredService<IInstanceService>();
        var gameProcessService = _services.GetRequiredService<IGameProcessService>();
        var versionService = _services.GetRequiredService<IVersionService>();
        var configService = _services.GetRequiredService<IConfigService>();

        // Push events from .NET → React
        progressService.DownloadProgressChanged += (msg) =>
        {
            try { Reply("hyprism:game:progress", msg); } catch { /* swallow */ }
        };

        progressService.GameStateChanged += (state, exitCode) =>
        {
            Logger.Info("IPC", $"Sending game-state event: state={state}, exitCode={exitCode}");
            try { Reply("hyprism:game:state", new { state, exitCode }); } catch { /* swallow */ }
        };

        progressService.ErrorOccurred += (type, message, technical) =>
        {
            try { Reply("hyprism:game:error", new { type, message, technical }); } catch { /* swallow */ }
        };

        Electron.IpcMain.On("hyprism:game:launch", async (args) =>
        {
            // First check if game is already running
            if (gameProcessService.IsGameRunning())
            {
                Logger.Warning("IPC", "Game launch request ignored - game already running");
                return;
            }
            
            // Optionally accept branch and version to launch a specific instance
            if (args != null)
            {
                try
                {
                    var json = ArgsToJson(args);
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOpts);
                    if (data != null)
                    {
                        if (data.TryGetValue("branch", out var branchEl))
                        {
                            var branchValue = branchEl.GetString() ?? "release";
                            #pragma warning disable CS0618 // Backward compatibility: VersionType kept for migration
                            configService.Configuration.VersionType = branchValue;
                            #pragma warning restore CS0618
                            configService.Configuration.LauncherBranch = branchValue;
                        }
                        if (data.TryGetValue("version", out var versionEl))
                        {
                            #pragma warning disable CS0618 // Backward compatibility: SelectedVersion kept for migration
                            configService.Configuration.SelectedVersion = versionEl.GetInt32();
                            #pragma warning restore CS0618
                        }
                    }
                }
                catch { /* ignore parsing errors, use current config */ }
            }
            
            Logger.Info("IPC", "Game launch requested");
            try { await gameSession.DownloadAndLaunchAsync(); }
            catch (Exception ex) { Logger.Error("IPC", $"Game launch failed: {ex.Message}"); }
        });

        Electron.IpcMain.On("hyprism:game:cancel", (_) =>
        {
            Logger.Info("IPC", "Game download cancel requested");
            gameSession.CancelDownload();
        });

        Electron.IpcMain.On("hyprism:game:instances", (_) =>
        {
            try
            {
                var instances = instanceService.GetInstalledInstances();
                Logger.Debug("IPC", $"Returning {instances.Count} installed instances");
                Reply("hyprism:game:instances:reply", instances);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get instances: {ex.Message}");
                Reply("hyprism:game:instances:reply", new List<object>());
            }
        });

        Electron.IpcMain.On("hyprism:game:isRunning", (_) =>
        {
            try
            {
                var isRunning = gameProcessService.CheckForRunningGame();
                Reply("hyprism:game:isRunning:reply", isRunning);
            }
            catch
            {
                Reply("hyprism:game:isRunning:reply", false);
            }
        });

        Electron.IpcMain.On("hyprism:game:versions", async (args) =>
        {
            try
            {
                #pragma warning disable CS0618 // Backward compatibility: VersionType kept for migration
                string branch = configService.Configuration.VersionType ?? "release";
                #pragma warning restore CS0618
                if (args != null)
                {
                    var json = ArgsToJson(args);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpts);
                    if (data != null && data.TryGetValue("branch", out var b) && !string.IsNullOrEmpty(b))
                    {
                        branch = b;
                    }
                }
                
                var versions = await versionService.GetVersionListAsync(branch);
                Logger.Info("IPC", $"Returning {versions.Count} available versions for branch {branch}");
                Reply("hyprism:game:versions:reply", versions);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get versions: {ex.Message}");
                Reply("hyprism:game:versions:reply", new List<int>());
            }
        });

        // Get versions with source information (official vs mirror)
        Electron.IpcMain.On("hyprism:game:versionsWithSources", async (args) =>
        {
            try
            {
                #pragma warning disable CS0618 // Backward compatibility: VersionType kept for migration
                string branch = configService.Configuration.VersionType ?? "release";
                #pragma warning restore CS0618
                if (args != null)
                {
                    var json = ArgsToJson(args);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpts);
                    if (data != null && data.TryGetValue("branch", out var b) && !string.IsNullOrEmpty(b))
                    {
                        branch = b;
                    }
                }
                
                var response = await versionService.GetVersionListWithSourcesAsync(branch);
                Logger.Info("IPC", $"Returning {response.Versions.Count} versions with sources for branch {branch} (official={response.OfficialSourceAvailable})");
                Reply("hyprism:game:versionsWithSources:reply", response);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get versions with sources: {ex.Message}");
                Reply("hyprism:game:versionsWithSources:reply", new { versions = new List<object>(), hasOfficialAccount = false, officialSourceAvailable = false });
            }
        });
    }
}
