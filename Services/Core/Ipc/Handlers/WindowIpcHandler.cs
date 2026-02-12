using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles window control IPC channels.
/// 
/// @ipc send hyprism:window:minimize
/// @ipc send hyprism:window:maximize
/// @ipc send hyprism:window:close
/// @ipc send hyprism:browser:open
/// </summary>
public class WindowIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public WindowIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        Electron.IpcMain.On("hyprism:window:minimize", (_) => GetMainWindow()?.Minimize());

        Electron.IpcMain.On("hyprism:window:maximize", async (_) =>
        {
            var win = GetMainWindow();
            if (win == null) return;
            if (await win.IsMaximizedAsync()) win.Unmaximize();
            else win.Maximize();
        });

        Electron.IpcMain.On("hyprism:window:close", (_) => GetMainWindow()?.Close());

        Electron.IpcMain.On("hyprism:browser:open", (args) =>
        {
            var url = ArgsToString(args);
            if (!string.IsNullOrEmpty(url))
                Electron.Shell.OpenExternalAsync(url);
        });
    }
}
