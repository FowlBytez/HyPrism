using HyPrism.Models;
using Profile = HyPrism.Models.Profile;

namespace HyPrism.Services.User.Profiles;

/// <summary>
/// Provides comprehensive profile management including CRUD operations,
/// nickname/UUID handling, avatar management, and profile data persistence.
/// </summary>
public interface IProfileManagementService
{
    // ─── Identity ──────────────────────────────────────────────

    /// <summary>Gets the current user's nickname from the active profile.</summary>
    string GetNick();

    /// <summary>Sets the current user's nickname (max 16 characters).</summary>
    bool SetNick(string nick);

    /// <summary>Gets the current user's UUID.</summary>
    string GetUUID();

    /// <summary>Sets the current user's UUID.</summary>
    bool SetUUID(string uuid);

    /// <summary>Gets the UUID for the current user, generating a new one if none exists.</summary>
    string GetCurrentUuid();

    // ─── Avatar ────────────────────────────────────────────────

    /// <summary>Gets a base64 data URI for the current user's avatar.</summary>
    string? GetAvatarPreview();

    /// <summary>Gets a base64 data URI for a specific UUID's avatar.</summary>
    string? GetAvatarPreviewForUUID(string uuid);

    /// <summary>Clears all cached avatar images.</summary>
    bool ClearAvatarCache();

    /// <summary>Gets the directory path for the current user's avatar images.</summary>
    string GetAvatarDirectory();

    /// <summary>Opens the avatar directory in the system file explorer.</summary>
    bool OpenAvatarDirectory();

    // ─── Profile CRUD ──────────────────────────────────────────

    /// <summary>Gets all valid user profiles.</summary>
    List<Profile> GetProfiles();

    /// <summary>Gets the zero-based index of the currently active profile.</summary>
    int GetActiveProfileIndex();

    /// <summary>Creates a new profile with the specified name and UUID.</summary>
    Profile? CreateProfile(string name, string uuid);

    /// <summary>Deletes a profile by its unique identifier.</summary>
    bool DeleteProfile(string profileId);

    /// <summary>Switches to a profile at the specified index.</summary>
    bool SwitchProfile(int index);

    /// <summary>Updates an existing profile's name and/or UUID.</summary>
    bool UpdateProfile(string profileId, string? newName, string? newUuid);

    /// <summary>Saves the current UUID and nickname as a new profile.</summary>
    Profile? SaveCurrentAsProfile();

    // ─── Profile Operations ────────────────────────────────────

    /// <summary>Duplicates a profile including all user data.</summary>
    Profile? DuplicateProfile(string profileId);

    /// <summary>Duplicates a profile without copying user data.</summary>
    Profile? DuplicateProfileWithoutData(string profileId);

    /// <summary>Opens the current profile's folder in the file explorer.</summary>
    bool OpenCurrentProfileFolder();

    /// <summary>Initializes the symbolic link for the current profile's mods folder.</summary>
    void InitializeProfileModsSymlink();

    /// <summary>Gets the absolute path to the profiles root folder.</summary>
    string GetProfilesFolder();

    /// <summary>Gets the file system path for a specific profile.</summary>
    string GetProfilePath(Profile profile);
}
