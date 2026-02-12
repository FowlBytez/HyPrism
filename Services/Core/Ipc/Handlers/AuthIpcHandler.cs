using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using HyPrism.Models;
using HyPrism.Services.User.Auth;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles Hytale authentication IPC channels.
/// 
/// @ipc invoke hyprism:auth:status -> HytaleAuthStatus
/// @ipc invoke hyprism:auth:login -> HytaleAuthStatus
/// @ipc invoke hyprism:auth:logout -> { success: boolean }
/// </summary>
public class AuthIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public AuthIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var authService = _services.GetRequiredService<HytaleAuthService>();

        Electron.IpcMain.On("hyprism:auth:status", (_) =>
        {
            Reply("hyprism:auth:status:reply", authService.GetAuthStatus());
        });

        Electron.IpcMain.On("hyprism:auth:login", async (_) =>
        {
            try
            {
                var session = await authService.LoginAsync();
                Reply("hyprism:auth:login:reply", authService.GetAuthStatus());
            }
            catch (HytaleNoProfileException)
            {
                Logger.Warning("IPC", "Auth login: no Hytale game profile found");
                Reply("hyprism:auth:login:reply", new { loggedIn = false, errorType = "no_profile", error = "No game profiles found in this Hytale account" });
            }
            catch (HytaleAuthException ex)
            {
                Logger.Error("IPC", $"Auth login error ({ex.ErrorType}): {ex.Message}");
                Reply("hyprism:auth:login:reply", new { loggedIn = false, errorType = ex.ErrorType, error = ex.Message });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Auth login failed: {ex.Message}");
                Reply("hyprism:auth:login:reply", new { loggedIn = false, errorType = "unknown", error = ex.Message });
            }
        });

        Electron.IpcMain.On("hyprism:auth:logout", (_) =>
        {
            authService.Logout();
            Reply("hyprism:auth:logout:reply", new { success = true });
        });
    }
}
