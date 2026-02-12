using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronNET.API;

namespace HyPrism.Services.Core.Ipc;

/// <summary>
/// Shared utilities for IPC handlers.
/// </summary>
public static class IpcHelpers
{
    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets the main Electron browser window.
    /// </summary>
    public static BrowserWindow? GetMainWindow()
    {
        return Electron.WindowManager.BrowserWindows.FirstOrDefault();
    }

    /// <summary>
    /// Converts IPC args to a JSON string for deserialization.
    /// The renderer sends JSON.stringify(data), so args is typically a string.
    /// But ElectronNET may also deliver a JsonElement or other deserialized object.
    /// </summary>
    public static string ArgsToJson(object? args)
    {
        if (args is null) return "{}";
        if (args is string s) return s;
        if (args is JsonElement je) return je.GetRawText();
        // Fallback: re-serialize whatever C# object ElectronNET produced
        return JsonSerializer.Serialize(args, JsonOpts);
    }

    /// <summary>
    /// Extracts a plain string from IPC args (for channels that expect a single string value).
    /// The renderer sends JSON.stringify("someValue") which produces '"someValue"',
    /// so we need to unwrap the outer quotes.
    /// </summary>
    public static string ArgsToString(object? args)
    {
        if (args is null) return string.Empty;
        var raw = args.ToString() ?? string.Empty;
        // If the renderer sent JSON.stringify("text"), we get a JSON-quoted string
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            try { return JsonSerializer.Deserialize<string>(raw) ?? raw; }
            catch { /* fall through */ }
        }
        return raw;
    }

    /// <summary>
    /// Sends a reply to the renderer on the specified channel.
    /// </summary>
    public static void Reply(string channel, object? data)
    {
        var win = GetMainWindow();
        if (win == null) return;
        Electron.IpcMain.Send(win, channel, JsonSerializer.Serialize(data, JsonOpts));
    }

    /// <summary>
    /// Sends a raw JSON string reply to the renderer.
    /// </summary>
    public static void ReplyRaw(string channel, string raw)
    {
        var win = GetMainWindow();
        if (win == null) return;
        Electron.IpcMain.Send(win, channel, raw);
    }
}
