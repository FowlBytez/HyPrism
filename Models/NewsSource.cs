namespace HyPrism.Models;

/// <summary>
/// Defines the source of news items.
/// </summary>
public enum NewsSource
{
    /// <summary>Fetch news from all sources.</summary>
    All,
    /// <summary>Fetch news from Hytale official blog only.</summary>
    Hytale,
    /// <summary>Fetch news from HyPrism GitHub releases only.</summary>
    HyPrism
}
