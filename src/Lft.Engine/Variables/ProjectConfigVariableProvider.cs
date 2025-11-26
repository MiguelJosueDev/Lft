using System.Text.Json;
using System.Text.Json.Serialization;
using Lft.Domain.Models;

namespace Lft.Engine.Variables;

/// <summary>
/// Loads variables from lft.config.json files found in the output directory hierarchy.
/// Uses dynamic discovery to determine where files should be placed.
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

        var configRoot = Path.GetDirectoryName(configPath)!;
        ctx.Set("_ConfigRoot", configRoot);

        if (profile.Params != null)
        {
            foreach (var (key, value) in profile.Params)
            {
                ctx.Set(key, value);
            }
        }

        var profileRoot = DetermineProfileRoot(profile, configRoot);

        if (!string.IsNullOrEmpty(profileRoot))
        {
            ctx.Set("_ProfileRoot", profileRoot);
            Console.WriteLine($"[LFT] Profile root: {profileRoot}");
        }
    }

    private static string? DetermineProfileRoot(LftProfile profile, string configRoot)
    {
        // 1. Explicit root property
        if (!string.IsNullOrEmpty(profile.Root))
        {
            return Path.GetFullPath(Path.Combine(configRoot, profile.Root));
        }

        // 2. Infer from profile name (e.g., "transactions-app" -> "apps/transactions-app/")
        if (!string.IsNullOrEmpty(profile.Profile))
        {
            var possibleRoot = Path.Combine(configRoot, "apps", profile.Profile);
            if (Directory.Exists(possibleRoot))
            {
                return possibleRoot;
            }
        }

        return null;
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

        [JsonPropertyName("root")]
        public string? Root { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, object>? Params { get; set; }
    }
}
