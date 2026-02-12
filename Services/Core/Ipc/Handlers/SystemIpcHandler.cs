using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles system information IPC channels.
/// 
/// @ipc invoke hyprism:system:gpuAdapters -> GpuAdapterInfo[]
/// </summary>
public class SystemIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public SystemIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var gpuService = _services.GetRequiredService<IGpuDetectionService>();

        Electron.IpcMain.On("hyprism:system:gpuAdapters", (_) =>
        {
            try
            {
                var adapters = gpuService.GetAdapters();
                Reply("hyprism:system:gpuAdapters:reply", adapters);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Failed to get GPU adapters: {ex.Message}");
                Reply("hyprism:system:gpuAdapters:reply", new List<object>());
            }
        });
    }
}
