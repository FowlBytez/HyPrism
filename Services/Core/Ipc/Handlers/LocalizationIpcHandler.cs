using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles localization IPC channels.
/// 
/// @ipc invoke hyprism:i18n:get -> Record<string, string>
/// @ipc invoke hyprism:i18n:current -> string
/// @ipc invoke hyprism:i18n:set -> { success: boolean, language: string }
/// @ipc invoke hyprism:i18n:languages -> LanguageInfo[]
/// </summary>
public class LocalizationIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public LocalizationIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var localization = _services.GetRequiredService<LocalizationService>();
        var settings = _services.GetRequiredService<ISettingsService>();

        Electron.IpcMain.On("hyprism:i18n:current", (_) =>
        {
            Reply("hyprism:i18n:current:reply", localization.CurrentLanguage);
        });

        Electron.IpcMain.On("hyprism:i18n:set", (args) =>
        {
            var lang = ArgsToString(args);
            if (string.IsNullOrEmpty(lang)) lang = "en-US";
            Logger.Info("IPC", $"Language change requested: {lang}");
            // Use SettingsService.SetLanguage which persists to config file
            var success = settings.SetLanguage(lang);
            Reply("hyprism:i18n:set:reply", new { success, language = success ? lang : localization.CurrentLanguage });
        });

        Electron.IpcMain.On("hyprism:i18n:languages", (_) =>
        {
            Reply("hyprism:i18n:languages:reply", LocalizationService.GetAvailableLanguages());
        });
    }
}
