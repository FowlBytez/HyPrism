using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles settings IPC channels.
/// 
/// @ipc invoke hyprism:settings:get -> SettingsSnapshot
/// @ipc invoke hyprism:settings:update -> { success: boolean }
/// </summary>
public class SettingsIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public SettingsIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var settings = _services.GetRequiredService<ISettingsService>();

        Electron.IpcMain.On("hyprism:settings:get", (_) =>
        {
            var lang = settings.GetLanguage();
            Reply("hyprism:settings:get:reply", new
            {
                language = lang,
                musicEnabled = settings.GetMusicEnabled(),
                launcherBranch = settings.GetLauncherBranch(),
                versionType = settings.GetVersionType(),
                selectedVersion = settings.GetSelectedVersion(),
                closeAfterLaunch = settings.GetCloseAfterLaunch(),
                showDiscordAnnouncements = settings.GetShowDiscordAnnouncements(),
                disableNews = settings.GetDisableNews(),
                backgroundMode = settings.GetBackgroundMode(),
                availableBackgrounds = settings.GetAvailableBackgrounds(),
                accentColor = settings.GetAccentColor(),
                hasCompletedOnboarding = settings.GetHasCompletedOnboarding(),
                onlineMode = settings.GetOnlineMode(),
                authDomain = settings.GetAuthDomain(),
                dataDirectory = settings.GetLauncherDataDirectory(),
                gpuPreference = settings.GetGpuPreference(),
                launcherVersion = UpdateService.GetCurrentVersion()
            });
        });

        Electron.IpcMain.On("hyprism:settings:update", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                var updates = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (updates != null)
                    foreach (var (key, value) in updates)
                        ApplySetting(settings, key, value);

                Reply("hyprism:settings:update:reply", new { success = true });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Settings update failed: {ex.Message}");
                Reply("hyprism:settings:update:reply", new { success = false, error = ex.Message });
            }
        });
    }

    private static void ApplySetting(ISettingsService s, string key, JsonElement val)
    {
        switch (key)
        {
            case "language": s.SetLanguage(val.GetString() ?? "en-US"); break;
            case "musicEnabled": s.SetMusicEnabled(val.GetBoolean()); break;
            case "launcherBranch": s.SetLauncherBranch(val.GetString() ?? "release"); break;
            case "versionType": s.SetVersionType(val.GetString() ?? "release"); break;
            case "selectedVersion": s.SetSelectedVersion(val.ValueKind == JsonValueKind.Number ? val.GetInt32() : 0); break;
            case "closeAfterLaunch": s.SetCloseAfterLaunch(val.GetBoolean()); break;
            case "showDiscordAnnouncements": s.SetShowDiscordAnnouncements(val.GetBoolean()); break;
            case "disableNews": s.SetDisableNews(val.GetBoolean()); break;
            case "backgroundMode": s.SetBackgroundMode(val.GetString() ?? "default"); break;
            case "accentColor": s.SetAccentColor(val.GetString() ?? "#7C5CFC"); break;
            case "onlineMode": s.SetOnlineMode(val.GetBoolean()); break;
            case "authDomain": s.SetAuthDomain(val.GetString() ?? ""); break;
            case "gpuPreference": s.SetGpuPreference(val.GetString() ?? "dedicated"); break;
            default: Logger.Warning("IPC", $"Unknown setting key: {key}"); break;
        }
    }
}
