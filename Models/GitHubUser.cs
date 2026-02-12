using System.Text.Json.Serialization;

namespace HyPrism.Models;

/// <summary>
/// Represents a GitHub user with basic profile information.
/// Used for displaying contributors and user details.
/// </summary>
public class GitHubUser
{
    /// <summary>
    /// Gets or sets the GitHub username (login name).
    /// </summary>
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the URL to the user's avatar image.
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the URL to the user's GitHub profile page.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the account type (e.g., "User", "Bot").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}
