using System.Text.Json.Serialization;

namespace HyPrism.Models;

/// <summary>
/// Request to create a game session on the custom auth server.
/// </summary>
public class GameSessionRequest
{
    [JsonPropertyName("uuid")]
    public string UUID { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Result of a game session token request from the custom auth server.
/// </summary>
public class AuthTokenResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? SessionToken { get; set; }
    public string? UUID { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}
