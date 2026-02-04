using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Text.Json.Serialization;

namespace HyPrism.Services.Core;

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";
    
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = "";
    
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private const string RepoOwner = "yyyumeniku";
    private const string RepoName = "HyPrism";

    public GitHubService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HyPrism-Launcher");
    }

    public async Task<List<GitHubUser>> GetContributorsAsync()
    {
        try
        {
            // Per page 100 to get everyone
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contributors?per_page=100";
            return await _httpClient.GetFromJsonAsync<List<GitHubUser>>(url) ?? new List<GitHubUser>();
        }
        catch (Exception ex)
        {
            Logger.Error("GitHub", $"Failed to fetch contributors: {ex}");
            return new List<GitHubUser>();
        }
    }

    public async Task<GitHubUser?> GetUserAsync(string username)
    {
        try
        {
            var url = $"https://api.github.com/users/{username}";
            return await _httpClient.GetFromJsonAsync<GitHubUser>(url);
        }
        catch (Exception ex)
        {
            Logger.Error("GitHub", $"Failed to fetch user {username}: {ex}");
            return null;
        }
    }

    public async Task<Bitmap?> LoadAvatarAsync(string url, int? width = null)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return null;
            
            var data = await _httpClient.GetByteArrayAsync(url);
            using var stream = new MemoryStream(data);
            
            if (width.HasValue)
            {
                return Bitmap.DecodeToWidth(stream, width.Value, BitmapInterpolationMode.HighQuality);
            }
            
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Logger.Error("GitHub", $"Failed to load avatar from {url}: {ex}");
            return null;
        }
    }
}
