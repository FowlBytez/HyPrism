using Microsoft.Extensions.DependencyInjection;
using HyPrism.Services.Core.Ipc.Handlers;

namespace HyPrism.Services.Core.Ipc;

/// <summary>
/// Central IPC router — bridges Electron IPC channels to .NET services.
/// Registered as a singleton via DI in Bootstrapper.cs.
/// Each channel follows the pattern: "hyprism:{domain}:{action}"
///
/// Structured @ipc annotations are parsed by Scripts/generate-ipc.mjs
/// to auto-generate Frontend/src/lib/ipc.ts (the ONLY IPC file).
///
/// These @type blocks define TypeScript interfaces emitted into the
/// generated ipc.ts. The C# code never reads them — they are only
/// consumed by the codegen script.
/// </summary>
/// 
/// @type ProgressUpdate { state: string; progress: number; messageKey: string; args?: unknown[]; downloadedBytes: number; totalBytes: number; }
/// @type GameState { state: 'starting' | 'started' | 'running' | 'stopped'; exitCode: number; }
/// @type GameError { type: string; message: string; technical?: string; }
/// @type NewsItem { title: string; excerpt?: string; url?: string; date?: string; publishedAt?: string; author?: string; imageUrl?: string; source?: string; }
/// @type Profile { id: string; name: string; uuid?: string; isOfficial?: boolean; avatar?: string; folderName?: string; }
/// @type HytaleAuthStatus { loggedIn: boolean; username?: string; uuid?: string; error?: string; errorType?: string; }
/// @type ProfileSnapshot { nick: string; uuid: string; avatarPath?: string; }
/// @type SettingsSnapshot { language: string; musicEnabled: boolean; launcherBranch: string; closeAfterLaunch: boolean; showDiscordAnnouncements: boolean; disableNews: boolean; backgroundMode: string; availableBackgrounds: string[]; accentColor: string; hasCompletedOnboarding: boolean; onlineMode: boolean; authDomain: string; dataDirectory: string; gpuPreference?: string; launchOnStartup?: boolean; minimizeToTray?: boolean; animations?: boolean; transparency?: boolean; resolution?: string; ramMb?: number; sound?: boolean; closeOnLaunch?: boolean; developerMode?: boolean; verboseLogging?: boolean; preRelease?: boolean; [key: string]: unknown; }
/// @type ModScreenshot { id: number; title: string; thumbnailUrl: string; url: string; }
/// @type ModInfo { id: string; name: string; slug: string; summary: string; author: string; downloadCount: number; iconUrl: string; thumbnailUrl: string; categories: string[]; dateUpdated: string; latestFileId: string; screenshots: ModScreenshot[]; }
/// @type ModSearchResult { mods: ModInfo[]; totalCount: number; }
/// @type ModFileInfo { id: string; modId: string; fileName: string; displayName: string; downloadUrl: string; fileLength: number; fileDate: string; releaseType: number; gameVersions: string[]; downloadCount: number; }
/// @type ModFilesResult { files: ModFileInfo[]; totalCount: number; }
/// @type ModCategory { id: number; name: string; slug: string; }
/// @type InstalledMod { id: string; name: string; slug?: string; version?: string; fileId?: string; fileName?: string; enabled: boolean; author?: string; description?: string; iconUrl?: string; curseForgeId?: string; fileDate?: string; releaseType?: number; latestFileId?: string; latestVersion?: string; screenshots?: ModScreenshot[]; }
/// @type SaveInfo { name: string; previewPath?: string; lastModified?: string; sizeBytes?: number; }
/// @type AppConfig { language: string; dataDirectory: string; [key: string]: unknown; }
/// @type InstalledInstance { id: string; branch: string; version: number; path: string; hasUserData: boolean; userDataSize: number; totalSize: number; isValid: boolean; }
/// @type InstanceInfo { id: string; name: string; branch: string; version: number; path: string; }
/// @type LanguageInfo { code: string; name: string; }
/// @type GpuAdapterInfo { name: string; vendor: string; type: string; }
/// @type VersionInfo { version: number; source: 'Official' | 'Mirror'; isLatest: boolean; }
/// @type VersionListResponse { versions: VersionInfo[]; hasOfficialAccount: boolean; officialSourceAvailable: boolean; }
public class IpcRouter
{
    private readonly IServiceProvider _services;
    private readonly List<IIpcHandler> _handlers = new();

    public IpcRouter(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers all IPC handlers.
    /// </summary>
    public void RegisterAll()
    {
        Logger.Info("IPC", "Registering IPC handlers...");

        // Create and register all handlers
        _handlers.Add(new ConfigIpcHandler(_services));
        _handlers.Add(new GameIpcHandler(_services));
        _handlers.Add(new InstanceIpcHandler(_services));
        _handlers.Add(new NewsIpcHandler(_services));
        _handlers.Add(new ProfileIpcHandler(_services));
        _handlers.Add(new AuthIpcHandler(_services));
        _handlers.Add(new SettingsIpcHandler(_services));
        _handlers.Add(new LocalizationIpcHandler(_services));
        _handlers.Add(new WindowIpcHandler(_services));
        _handlers.Add(new ModIpcHandler(_services));
        _handlers.Add(new SystemIpcHandler(_services));
        _handlers.Add(new ConsoleIpcHandler(_services));
        _handlers.Add(new FileDialogIpcHandler(_services));

        // Register all handlers
        foreach (var handler in _handlers)
        {
            handler.Register();
        }

        Logger.Success("IPC", $"All IPC handlers registered ({_handlers.Count} handlers)");
    }
}
