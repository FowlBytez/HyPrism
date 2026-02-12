using HyPrism.Models;

namespace HyPrism.Services.Core;

/// <summary>
/// Manages progress notifications for download, installation, and game state changes.
/// Coordinates with Discord Rich Presence to reflect current activity.
/// </summary>
public class ProgressNotificationService : IProgressNotificationService
{
    private readonly IDiscordService _discordService;
    
    /// <inheritdoc/>
    public event Action<ProgressUpdateMessage>? DownloadProgressChanged;
    
    /// <inheritdoc/>
    public event Action<string, int>? GameStateChanged;
    
    /// <inheritdoc/>
    public event Action<string, string, string?>? ErrorOccurred;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressNotificationService"/> class.
    /// </summary>
    /// <param name="discordService">The Discord service for Rich Presence updates.</param>
    public ProgressNotificationService(IDiscordService discordService)
    {
        _discordService = discordService;
    }
    
    /// <inheritdoc/>
    public void ReportDownloadProgress(string stage, int progress, string messageKey, object[]? args = null, long downloaded = 0, long total = 0)
    {
        var msg = new ProgressUpdateMessage 
        { 
            State = stage, 
            Progress = progress, 
            MessageKey = messageKey, 
            Args = args,
            DownloadedBytes = downloaded,
            TotalBytes = total
        };
        
        DownloadProgressChanged?.Invoke(msg);
        
        // Only update Discord presence on completion
        if (stage == "complete")
        {
            _discordService.SetPresence(DiscordService.PresenceState.Idle);
        }
    }

    /// <inheritdoc/>
    public void ReportGameStateChanged(string state, int? exitCode = null)
    {
        switch (state)
        {
            case "starting":
                GameStateChanged?.Invoke(state, 0);
                break;
            case "running":
                GameStateChanged?.Invoke(state, 0);
                _discordService.SetPresence(DiscordService.PresenceState.Playing);
                break;
            case "stopped":
                GameStateChanged?.Invoke(state, exitCode ?? 0);
                _discordService.SetPresence(DiscordService.PresenceState.Idle);
                break;
        }
    }

    /// <inheritdoc/>
    public void ReportError(string type, string message, string? technical = null)
    {
        ErrorOccurred?.Invoke(type, message, technical);
    }
}
