using System.Text.Encodings.Web;
using System.Text.Json;
using HyPrism.Models;
using HyPrism.Services.Core.Infrastructure;

namespace HyPrism.Services.Game.Sources;

/// <summary>
/// Loads mirror definitions from JSON meta files in the Mirrors directory.
/// On first launch (directory missing or empty), generates default built-in mirror definitions.
/// Users can add custom mirror files to appDir/Mirrors/ — the launcher reads them on startup.
/// </summary>
public static class MirrorLoaderService
{
    private const string MirrorsDirName = "Mirrors";
    private const string MirrorFileExtension = ".mirror.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Loads all mirror sources from the Mirrors directory.
    /// Generates default mirror files if directory is missing or empty.
    /// </summary>
    /// <param name="appDir">Application data directory.</param>
    /// <param name="httpClient">Shared HTTP client.</param>
    /// <returns>List of IVersionSource instances created from mirror meta files.</returns>
    public static List<IVersionSource> LoadAll(string appDir, HttpClient httpClient)
    {
        var mirrorsDir = Path.Combine(appDir, MirrorsDirName);

        // Generate defaults on first launch (directory missing or empty)
        if (!Directory.Exists(mirrorsDir) || !Directory.EnumerateFiles(mirrorsDir, $"*{MirrorFileExtension}").Any())
        {
            Logger.Info("MirrorLoader", "No mirror definitions found, generating defaults...");
            GenerateDefaults(mirrorsDir);
        }

        var sources = new List<IVersionSource>();
        var files = Directory.GetFiles(mirrorsDir, $"*{MirrorFileExtension}");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var meta = JsonSerializer.Deserialize<MirrorMeta>(json, JsonOptions);

                if (meta == null)
                {
                    Logger.Warning("MirrorLoader", $"Failed to deserialize: {Path.GetFileName(file)}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(meta.Id))
                {
                    Logger.Warning("MirrorLoader", $"Mirror file has no id: {Path.GetFileName(file)}");
                    continue;
                }

                if (!meta.Enabled)
                {
                    Logger.Info("MirrorLoader", $"Mirror '{meta.Id}' is disabled, skipping");
                    continue;
                }

                // Validate sourceType has matching config
                if (meta.SourceType == "pattern" && meta.Pattern == null)
                {
                    Logger.Warning("MirrorLoader", $"Mirror '{meta.Id}' has sourceType 'pattern' but no pattern config");
                    continue;
                }
                if (meta.SourceType == "json-index" && meta.JsonIndex == null)
                {
                    Logger.Warning("MirrorLoader", $"Mirror '{meta.Id}' has sourceType 'json-index' but no jsonIndex config");
                    continue;
                }

                var source = new JsonMirrorSource(meta, httpClient);
                sources.Add(source);

                Logger.Info("MirrorLoader", $"Loaded mirror: {meta.Name} ({meta.Id}) [priority={meta.Priority}, type={meta.SourceType}]");
            }
            catch (JsonException ex)
            {
                Logger.Warning("MirrorLoader", $"Invalid JSON in {Path.GetFileName(file)}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Warning("MirrorLoader", $"Error loading {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // Sort by priority
        sources.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        Logger.Success("MirrorLoader", $"Loaded {sources.Count} mirror source(s) from {mirrorsDir}");
        return sources;
    }

    /// <summary>
    /// Generates default mirror JSON files for the built-in community mirrors.
    /// </summary>
    private static void GenerateDefaults(string mirrorsDir)
    {
        Directory.CreateDirectory(mirrorsDir);

        var defaults = GetDefaultMirrors();
        foreach (var meta in defaults)
        {
            try
            {
                var fileName = $"{meta.Id}{MirrorFileExtension}";
                var filePath = Path.Combine(mirrorsDir, fileName);

                var json = JsonSerializer.Serialize(meta, JsonOptions);
                File.WriteAllText(filePath, json);

                Logger.Info("MirrorLoader", $"Generated default mirror: {fileName}");
            }
            catch (Exception ex)
            {
                Logger.Warning("MirrorLoader", $"Failed to generate default mirror '{meta.Id}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Returns the built-in mirror definitions that were previously hardcoded.
    /// </summary>
    private static List<MirrorMeta> GetDefaultMirrors()
    {
        return new List<MirrorMeta>
        {
            // EstroGen mirror — html-autoindex based
            new() {
                SchemaVersion = 1,
                Id = "estrogen",
                Name = "EstroGen",
                Description = "Community mirror hosted by EstroGen (licdn.estrogen.cat)",
                Priority = 100,
                Enabled = true,
                SourceType = "pattern",
                Pattern = new MirrorPatternConfig
                {
                    FullBuildUrl = "{base}/{os}/{arch}/{branch}/0/{version}.pwr",
                    DiffPatchUrl = "{base}/{os}/{arch}/{branch}/{from}/{to}.pwr",
                    SignatureUrl = "{base}/{os}/{arch}/{branch}/0/{version}.pwr.sig",
                    BaseUrl = "https://licdn.estrogen.cat/hytale/patches",
                    VersionDiscovery = new VersionDiscoveryConfig
                    {
                        Method = "html-autoindex",
                        Url = "{base}/{os}/{arch}/{branch}/0/",
                        HtmlPattern = @"<a\s+href=""(\d+)\.pwr"">\d+\.pwr</a>\s+\S+\s+\S+\s+(\d+)",
                        MinFileSizeBytes = 1_048_576
                    },
                    DiffBasedBranches = new List<string>()
                },
                SpeedTest = new MirrorSpeedTestConfig
                {
                    PingUrl = "https://licdn.estrogen.cat/hytale/patches"
                },
                Cache = new MirrorCacheConfig
                {
                    IndexTtlMinutes = 30,
                    SpeedTestTtlMinutes = 60
                }
            },

            // CobyLobby mirror — JSON API based
            new() {
                SchemaVersion = 1,
                Id = "cobylobby",
                Name = "CobyLobby",
                Description = "Community mirror hosted by CobyLobby (cobylobbyht.store)",
                Priority = 101,
                Enabled = true,
                SourceType = "pattern",
                Pattern = new MirrorPatternConfig
                {
                    FullBuildUrl = "{base}/launcher/patches/{os}/{arch}/{branch}/0/{version}.pwr",
                    DiffPatchUrl = "{base}/launcher/patches/{os}/{arch}/{branch}/{from}/{to}.pwr",
                    BaseUrl = "https://cobylobbyht.store",
                    VersionDiscovery = new VersionDiscoveryConfig
                    {
                        Method = "json-api",
                        Url = "{base}/launcher/patches/{branch}/versions?os_name={os}&arch={arch}",
                        JsonPath = "items[].version"
                    },
                    BranchMapping = new Dictionary<string, string>
                    {
                        ["pre-release"] = "prerelease"
                    },
                    DiffBasedBranches = new List<string>()
                },
                SpeedTest = new MirrorSpeedTestConfig
                {
                    PingUrl = "https://cobylobbyht.store/health"
                },
                Cache = new MirrorCacheConfig
                {
                    IndexTtlMinutes = 30,
                    SpeedTestTtlMinutes = 60
                }
            },

            // ShipOfYarn mirror — JSON index based
            new() {
                SchemaVersion = 1,
                Id = "shipofyarn",
                Name = "ShipOfYarn",
                Description = "Community mirror hosted by ShipOfYarn (thecute.cloud)",
                Priority = 102,
                Enabled = true,
                SourceType = "json-index",
                JsonIndex = new MirrorJsonIndexConfig
                {
                    ApiUrl = "https://thecute.cloud/ShipOfYarn/api.php",
                    RootPath = "hytale",
                    Structure = "grouped",
                    PlatformMapping = new Dictionary<string, string>
                    {
                        ["darwin"] = "mac"
                    },
                    FileNamePattern = new FileNamePatternConfig
                    {
                        Full = "v{version}-{os}-{arch}.pwr",
                        Diff = "v{from}~{to}-{os}-{arch}.pwr"
                    },
                    DiffBasedBranches = new List<string> { "pre-release" }
                },
                SpeedTest = new MirrorSpeedTestConfig
                {
                    PingUrl = "https://thecute.cloud/ShipOfYarn/api.php"
                },
                Cache = new MirrorCacheConfig
                {
                    IndexTtlMinutes = 30,
                    SpeedTestTtlMinutes = 60
                }
            }
        };
    }
}
