using System.Text.Json.Serialization;

namespace HyPrism.Models;

/// <summary>
/// Persisted auth session for Hytale account.
/// </summary>
public class HytaleAuthSession
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";
    
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";
    
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
    
    [JsonPropertyName("session_token")]
    public string SessionToken { get; set; } = "";
    
    [JsonPropertyName("identity_token")]
    public string IdentityToken { get; set; } = "";
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
    
    [JsonPropertyName("uuid")]
    public string UUID { get; set; } = "";
    
    [JsonPropertyName("account_owner_id")]
    public string AccountOwnerId { get; set; } = "";
}

/// <summary>
/// Thrown when no game profiles are found in the Hytale account.
/// This is a non-critical warning — the user simply has no game profile yet.
/// </summary>
public class HytaleNoProfileException : Exception
{
    public HytaleNoProfileException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a general auth error occurs (network, HTTP status, etc.).
/// </summary>
public class HytaleAuthException : Exception
{
    public string ErrorType { get; }
    public HytaleAuthException(string errorType, string message) : base(message)
    {
        ErrorType = errorType;
    }
}
