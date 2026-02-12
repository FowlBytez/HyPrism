using System.Text.Json;
using ElectronNET.API;
using Microsoft.Extensions.DependencyInjection;
using HyPrism.Models;
using HyPrism.Services.User.Profiles;
using static HyPrism.Services.Core.Ipc.IpcHelpers;

namespace HyPrism.Services.Core.Ipc.Handlers;

/// <summary>
/// Handles profile management IPC channels.
/// 
/// @ipc invoke hyprism:profile:get -> ProfileSnapshot
/// @ipc invoke hyprism:profile:list -> Profile[]
/// @ipc invoke hyprism:profile:switch -> { success: boolean }
/// @ipc invoke hyprism:profile:setNick -> { success: boolean }
/// @ipc invoke hyprism:profile:setUuid -> { success: boolean }
/// @ipc invoke hyprism:profile:create -> Profile
/// @ipc invoke hyprism:profile:delete -> { success: boolean }
/// @ipc invoke hyprism:profile:activeIndex -> number
/// @ipc invoke hyprism:profile:save -> { success: boolean }
/// @ipc invoke hyprism:profile:duplicate -> Profile
/// @ipc send hyprism:profile:openFolder
/// @ipc invoke hyprism:profile:avatarForUuid -> string
/// </summary>
public class ProfileIpcHandler : IIpcHandler
{
    private readonly IServiceProvider _services;

    public ProfileIpcHandler(IServiceProvider services)
    {
        _services = services;
    }

    public void Register()
    {
        var profileMgmt = _services.GetRequiredService<IProfileManagementService>();

        Electron.IpcMain.On("hyprism:profile:get", (_) =>
        {
            Reply("hyprism:profile:get:reply", new
            {
                nick = profileMgmt.GetNick(),
                uuid = profileMgmt.GetUUID(),
                avatarPath = profileMgmt.GetAvatarPreview()
            });
        });

        Electron.IpcMain.On("hyprism:profile:list", (_) =>
        {
            Reply("hyprism:profile:list:reply", profileMgmt.GetProfiles());
        });

        Electron.IpcMain.On("hyprism:profile:switch", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var index = doc.RootElement.GetProperty("index").GetInt32();
                Reply("hyprism:profile:switch:reply", new { success = profileMgmt.SwitchProfile(index) });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Profile switch failed: {ex.Message}");
                Reply("hyprism:profile:switch:reply", new { success = false });
            }
        });

        Electron.IpcMain.On("hyprism:profile:setNick", (args) =>
        {
            var nick = ArgsToString(args);
            var success = profileMgmt.SetNick(nick);
            Reply("hyprism:profile:setNick:reply", new { success });
        });

        Electron.IpcMain.On("hyprism:profile:setUuid", (args) =>
        {
            var uuid = ArgsToString(args);
            var success = profileMgmt.SetUUID(uuid);
            Reply("hyprism:profile:setUuid:reply", new { success });
        });

        Electron.IpcMain.On("hyprism:profile:create", (args) =>
        {
            try
            {
                var json = ArgsToJson(args);
                using var doc = JsonDocument.Parse(json);
                var name = doc.RootElement.GetProperty("name").GetString() ?? "";
                var uuid = doc.RootElement.GetProperty("uuid").GetString() ?? "";
                var isOfficial = doc.RootElement.TryGetProperty("isOfficial", out var officialProp) && officialProp.GetBoolean();
                
                var profile = profileMgmt.CreateProfile(name, uuid);
                if (profile != null)
                {
                    profile.IsOfficial = isOfficial;
                    _services.GetRequiredService<IConfigService>().SaveConfig();
                }
                Reply("hyprism:profile:create:reply", profile != null ? (object)profile : new { error = "Failed to create profile" });
            }
            catch (Exception ex)
            {
                Logger.Error("IPC", $"Profile create failed: {ex.Message}");
                Reply("hyprism:profile:create:reply", new { error = ex.Message });
            }
        });

        Electron.IpcMain.On("hyprism:profile:delete", (args) =>
        {
            var id = ArgsToString(args);
            var success = profileMgmt.DeleteProfile(id);
            Reply("hyprism:profile:delete:reply", new { success });
        });

        Electron.IpcMain.On("hyprism:profile:activeIndex", (_) =>
        {
            Reply("hyprism:profile:activeIndex:reply", profileMgmt.GetActiveProfileIndex());
        });

        Electron.IpcMain.On("hyprism:profile:save", (_) =>
        {
            var profile = profileMgmt.SaveCurrentAsProfile();
            Reply("hyprism:profile:save:reply", new { success = profile != null });
        });

        Electron.IpcMain.On("hyprism:profile:duplicate", (args) =>
        {
            var id = ArgsToString(args);
            var profile = profileMgmt.DuplicateProfileWithoutData(id);
            Reply("hyprism:profile:duplicate:reply", profile != null ? (object)profile : new { error = "Failed to duplicate" });
        });

        Electron.IpcMain.On("hyprism:profile:openFolder", (_) =>
        {
            profileMgmt.OpenCurrentProfileFolder();
        });

        Electron.IpcMain.On("hyprism:profile:avatarForUuid", (args) =>
        {
            var uuid = ArgsToString(args);
            var path = profileMgmt.GetAvatarPreviewForUUID(uuid);
            Reply("hyprism:profile:avatarForUuid:reply", path ?? "");
        });
    }
}
