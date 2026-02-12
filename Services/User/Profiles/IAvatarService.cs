namespace HyPrism.Services.User.Profiles;

/// <summary>
/// Manages user avatar cache and preview images for game instances.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Event raised when the user's avatar image has been updated.
    /// </summary>
    event Action<string>? AvatarUpdated;

    /// <summary>
    /// Gets the path to the persistent avatar backup file for the specified UUID.
    /// </summary>
    string GetAvatarBackupPath(string uuid);

    /// <summary>
    /// Copies the latest avatar from the game's CachedAvatarPreviews to persistent backup.
    /// </summary>
    bool BackupAvatar(string uuid);

    /// <summary>
    /// Clears the avatar cache for the specified UUID.
    /// </summary>
    bool ClearAvatarCache(string uuid);
}
