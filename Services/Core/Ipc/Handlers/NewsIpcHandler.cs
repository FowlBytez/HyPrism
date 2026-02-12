using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles news IPC channels.
/// 
/// @ipc invoke hyprism:news:get -> NewsItem[]
/// </summary>
public class NewsIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public NewsIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var newsService = _services.GetRequiredService<INewsService>();

        Electron.IpcMain.On("hyprism:news:get", async (_) =>
        {
            try
            {
                var news = await newsService.GetNewsAsync();
                Reply("hyprism:news:get:reply", news);
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"News fetch failed: {ex.Message}");
                Reply("hyprism:news:get:reply", new { error = ex.Message });
            }
        });
    }
}
