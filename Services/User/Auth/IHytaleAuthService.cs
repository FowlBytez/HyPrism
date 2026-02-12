using HyPrism.Models;

namespace HyPrism.Services.User.Auth;

/// <summary>
/// Handles Hytale OAuth 2.0 authentication and session management.
/// </summary>
public interface IHytaleAuthService
{
    /// <summary>
    /// The current auth session, or null if not logged in.
    /// </summary>
    HytaleAuthSession? CurrentSession { get; }

    /// <summary>
    /// Starts the OAuth login flow: opens browser, waits for callback,
    /// exchanges code for tokens, fetches profile data.
    /// </summary>
    /// <returns>The authenticated session, or null on failure/cancellation.</returns>
    Task<HytaleAuthSession?> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out: clears session from memory and disk.
    /// </summary>
    void Logout();

    /// <summary>
    /// Returns the current auth status (logged in, username, uuid).
    /// </summary>
    object GetAuthStatus();

    /// <summary>
    /// Refreshes the access token if expired. Returns a valid session or null.
    /// </summary>
    Task<HytaleAuthSession?> GetValidSessionAsync();

    /// <summary>
    /// Forces a token refresh regardless of expiry time.
    /// </summary>
    Task<bool> ForceRefreshAsync();

    /// <summary>
    /// Ensures a valid session with fresh game session tokens for launching.
    /// </summary>
    Task<HytaleAuthSession?> EnsureFreshSessionForLaunchAsync();

    /// <summary>
    /// Reloads session for the current profile. Call this after switching profiles.
    /// </summary>
    void ReloadSessionForCurrentProfile();
}
