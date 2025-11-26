using System.Text.Json;
using System.Text.Json.Serialization;
using Lft.Domain.Models;

namespace Lft.Engine.Variables;

/// <summary>
/// Loads variables from lft.config.json files found in the output directory hierarchy.
/// Supports multiple profiles and definition paths.
/// </summary>
public sealed class ProjectConfigVariableProvider : IVariableProvider
{
    private const string ConfigFileName = "lft.config.json";

    private readonly string? _profileName;

    public ProjectConfigVariableProvider(string? profileName = null)
    {
        _profileName = profileName;
    }

    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        var configPath = FindConfigFile(request.OutputDirectory);
        if (configPath == null)
        {
            // No config file found - that's OK, ConventionsVariableProvider has defaults
            return;
        }

        var profiles = LoadProfiles(configPath);
        if (profiles == null || profiles.Count == 0)
        {
            return;
        }

        var profile = SelectProfile(profiles);
        if (profile == null)
        {
            return;
        }

        // Set the config root (directory containing lft.config.json)
        var configRoot = Path.GetDirectoryName(configPath)!;
        ctx.Set("_ConfigRoot", configRoot);

        // Populate params as variables
        if (profile.Params != null)
        {
            foreach (var (key, value) in profile.Params)
            {
                ctx.Set(key, value);
            }
        }

        // Populate definition paths (store raw paths - they contain Liquid templates like {{BaseNamespaceName}})
        // The StepExecutor will render these paths when resolving output locations
        if (profile.Defs != null)
        {
            foreach (var def in profile.Defs)
            {
                // Store the raw path (with Liquid variables) - will be rendered by StepExecutor
                ctx.Set($"_defPath_{def.Name}", def.Path);
                ctx.Set($"_ConfigRoot", configRoot);
            }
        }
    }

    private static string? FindConfigFile(string? startDirectory)
    {
        if (string.IsNullOrEmpty(startDirectory))
        {
            startDirectory = Directory.GetCurrentDirectory();
        }

        var dir = new DirectoryInfo(startDirectory);

        // Walk up the directory tree looking for lft.config.json
        while (dir != null)
        {
            var configPath = Path.Combine(dir.FullName, ConfigFileName);
            if (File.Exists(configPath))
            {
                return configPath;
            }
            dir = dir.Parent;
        }

        return null;
    }

    private static List<LftProfile>? LoadProfiles(string configPath)
    {
        try
        {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<List<LftProfile>>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LFT] Warning: Failed to parse {configPath}: {ex.Message}");
            return null;
        }
    }

    private LftProfile? SelectProfile(List<LftProfile> profiles)
    {
        // If a profile name was specified, use it
        if (!string.IsNullOrEmpty(_profileName))
        {
            var profile = profiles.FirstOrDefault(p =>
                string.Equals(p.Profile, _profileName, StringComparison.OrdinalIgnoreCase));

            if (profile == null)
            {
                Console.WriteLine($"[LFT] Warning: Profile '{_profileName}' not found in config. Available: {string.Join(", ", profiles.Select(p => p.Profile))}");
            }
            return profile;
        }

        // Otherwise use the default profile
        var defaultProfile = profiles.FirstOrDefault(p => p.Default);

        // If no default, use the first profile
        return defaultProfile ?? profiles.FirstOrDefault();
    }

    private sealed class LftProfile
    {
        [JsonPropertyName("profile")]
        public string? Profile { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, object>? Params { get; set; }

        [JsonPropertyName("defs")]
        public List<LftDefinition>? Defs { get; set; }
    }

    private sealed class LftDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }
}
