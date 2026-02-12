namespace HyPrism.Models;

/// <summary>
/// Represents a username->UUID mapping for the frontend.
/// </summary>
public class UuidMapping
{
    public string Username { get; set; } = "";
    public string Uuid { get; set; } = "";
    public bool IsCurrent { get; set; } = false;
}
