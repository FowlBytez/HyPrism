using System.Text.Json.Serialization;

namespace HyPrism.UI.Models;

public class NewsItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    
    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = "";
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
    
    [JsonPropertyName("date")]
    public string Date { get; set; } = "";
    
    [JsonPropertyName("author")]
    public string Author { get; set; } = "";
    
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = "";
    
    public string Source { get; set; } = "hytale"; // "hytale" or "hyprism"
}
