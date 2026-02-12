using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles configuration IPC channels.
/// 
/// @ipc invoke hyprism:config:get -> AppConfig
/// @ipc invoke hyprism:config:save -> { success: boolean }
/// </summary>
public class ConfigIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public ConfigIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var config = _services.GetRequiredService<IConfigService>();

        Electron.IpcMain.On("hyprism:config:get", (_) =>
        {
            Reply("hyprism:config:get:reply", config.Configuration);
        });

        Electron.IpcMain.On("hyprism:config:save", (_) =>
        {
            try
            {
                config.SaveConfig();
                Reply("hyprism:config:save:reply", new { success = true });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Config save failed: {ex.Message}");
                Reply("hyprism:config:save:reply", new { success = false, error = ex.Message });
            }
        });
    }
}
