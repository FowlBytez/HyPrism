using ElectronNET.API;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles console logging IPC channels (Electron renderer → .NET Logger).
/// 
/// @ipc send hyprism:console:log
/// @ipc send hyprism:console:warn
/// @ipc send hyprism:console:error
/// </summary>
public class ConsoleIpcHandler : IIpcHandler
{
    public ConsoleIpcHandler(IServiceProvider services)
    {
        // No dependencies needed
    }

    public void Register()
    {
        Electron.IpcMain.On("hyprism:console:log", (args) =>
            Logger.Info("Renderer", ArgsToString(args)));

        Electron.IpcMain.On("hyprism:console:warn", (args) =>
            Logger.Warning("Renderer", ArgsToString(args)));

        Electron.IpcMain.On("hyprism:console:error", (args) =>
            Logger.Error("Renderer", ArgsToString(args)));
    }
}
