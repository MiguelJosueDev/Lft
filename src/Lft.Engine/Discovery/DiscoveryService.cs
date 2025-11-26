using Lft.Discovery;
using Lft.Engine.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.Engine.Discovery;

/// <summary>
/// Integrates project discovery with the generation engine.
/// </summary>
public sealed class DiscoveryService : IDiscoveryService
{
    private readonly IProjectAnalyzer _analyzer;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(IProjectAnalyzer? analyzer = null, ILogger<DiscoveryService>? logger = null)
    {
        _analyzer = analyzer ?? new ProjectAnalyzer();
        _logger = logger ?? NullLogger<DiscoveryService>.Instance;
    }

    /// <summary>
    /// Analyzes the project and populates the variable context with discovered values.
    /// </summary>
    public async Task<ProjectManifest> AnalyzeAndPopulateAsync(
        string profileRoot,
        VariableContext ctx,
        CancellationToken ct = default)
    {
        var manifest = await _analyzer.AnalyzeAsync(profileRoot, ct);

        // Populate context with discovered variables
        var variables = manifest.ToVariables();
        foreach (var (key, value) in variables)
        {
            if (value != null)
            {
                ctx.Set(key, value);
            }
        }

        // Also set MainModuleName from discovered app name if not already set
        ctx.SetDefault("MainModuleName", manifest.AppName);

        _logger.LogInformation("Discovered app {App} ({Namespace})", manifest.AppName, manifest.BaseNamespace);
        _logger.LogInformation("Found {Count} injection points", manifest.InjectionPoints.Count);

        return manifest;
    }

    /// <summary>
    /// Gets the injection point for a specific target from the manifest.
    /// </summary>
    public static InjectionPoint? GetInjectionPoint(ProjectManifest manifest, string targetName)
    {
        if (Enum.TryParse<InjectionTarget>(targetName, ignoreCase: true, out var target))
        {
            return manifest.GetInjectionPoint(target);
        }
        return null;
    }

    /// <summary>
    /// Resolves the output path for a file based on layer type and manifest.
    /// </summary>
    public static string ResolveOutputPath(
        ProjectManifest manifest,
        string fileName,
        string? layerHint = null)
    {
        // Determine layer from file name or hint
        var layer = layerHint?.ToLowerInvariant() switch
        {
            "models" => manifest.Models,
            "entities" => manifest.Repositories,
            "repositories" => manifest.Repositories,
            "services" => manifest.Services,
            "interfaces" => manifest.Interfaces,
            "endpoints" => manifest.Api,
            "routes" => manifest.Api,
            "api" => manifest.Api,
            _ => InferLayerFromFileName(manifest, fileName)
        };

        if (layer == null)
        {
            // Fallback to API root
            return Path.Combine(manifest.ApiRoot, fileName);
        }

        // Determine subfolder based on file type
        var subFolder = GetSubFolder(layer, fileName);
        return subFolder != null
            ? Path.Combine(subFolder, Path.GetFileName(fileName))
            : Path.Combine(layer.Path, fileName);
    }

    private static LayerInfo? InferLayerFromFileName(ProjectManifest manifest, string fileName)
    {
        var nameLower = fileName.ToLowerInvariant();

        if (nameLower.Contains("model"))
            return manifest.Models;
        if (nameLower.Contains("entity"))
            return manifest.Repositories;
        if (nameLower.Contains("repository"))
            return manifest.Repositories;
        if (nameLower.Contains("service") && !nameLower.Contains("extension"))
            return manifest.Services;
        if (nameLower.Contains("endpoint"))
            return manifest.Api;
        if (nameLower.Contains("routes"))
            return manifest.Api;

        return null;
    }

    private static string? GetSubFolder(LayerInfo layer, string fileName)
    {
        var nameLower = fileName.ToLowerInvariant();

        if (nameLower.Contains("entity") && layer.EntitiesPath != null)
            return layer.EntitiesPath;
        if (nameLower.Contains("endpoint") && layer.EndpointsPath != null)
            return layer.EndpointsPath;
        if (nameLower.Contains("routes") && layer.RoutesPath != null)
            return layer.RoutesPath;
        if (nameLower.Contains("mapping") && layer.MappersPath != null)
            return layer.MappersPath;
        if (nameLower.Contains("extension") && layer.ExtensionsPath != null)
            return layer.ExtensionsPath;

        return null;
    }
}
