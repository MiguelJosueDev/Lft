using System.Text.RegularExpressions;

namespace Lft.Discovery;

/// <summary>
/// Analyzes a project structure and builds a ProjectManifest.
/// </summary>
public sealed partial class ProjectAnalyzer : IProjectAnalyzer
{
    private readonly INamespaceResolver _namespaceResolver;
    private readonly IInjectionPointLocator _injectionPointLocator;

    // Pattern to extract app name from project name: LiveFree.[App].Layer or LiveFree.Artemis.[App].Layer
    // Also matches without trailing dot for minimal structures like LiveFree.AppName
    [GeneratedRegex(@"LiveFree\.(?:Artemis\.)?(\w+)(?:\.|$)")]
    private static partial Regex AppNameRegex();

    // Pattern to detect layer from project name
    [GeneratedRegex(@"\.(\w+)(?:\.SqlServer|\.Providers)?$")]
    private static partial Regex LayerSuffixRegex();

    public ProjectAnalyzer(
        INamespaceResolver? namespaceResolver = null,
        IInjectionPointLocator? injectionPointLocator = null)
    {
        _namespaceResolver = namespaceResolver ?? new NamespaceResolver();
        _injectionPointLocator = injectionPointLocator ?? new InjectionPointLocator(_namespaceResolver);
    }

    public async Task<ProjectManifest> AnalyzeAsync(string profileRoot, CancellationToken ct = default)
    {
        // 1. Detect API and App roots
        var apiRoot = FindSubdirectory(profileRoot, "api") ?? profileRoot;
        var appRoot = FindSubdirectory(profileRoot, "app");

        // 2. Discover all .NET projects
        var projects = DiscoverProjects(apiRoot);

        // 3. Categorize projects by layer
        var layers = CategorizeProjects(projects);

        // 4. Determine app name and base namespace
        var (appName, baseNamespace) = DetectAppIdentity(projects);

        // 5. Detect naming conventions
        var conventions = await DetectConventionsAsync(layers, appName, ct);

        // 6. Locate injection points
        var injectionPoints = await _injectionPointLocator.LocateAllAsync(apiRoot, ct);

        // 7. Discover frontend router and route files
        var frontendRouters = DiscoverFrontendRouters(appRoot);
        var frontendRoutes = DiscoverFrontendRoutes(appRoot);

        return new ProjectManifest
        {
            AppName = appName,
            BaseNamespace = baseNamespace,
            ProfileRoot = profileRoot,
            ApiRoot = apiRoot,
            AppRoot = appRoot,
            Api = layers.GetValueOrDefault(LayerType.Api),
            Services = layers.GetValueOrDefault(LayerType.Services),
            Repositories = layers.GetValueOrDefault(LayerType.Repositories),
            Models = layers.GetValueOrDefault(LayerType.Models),
            Interfaces = layers.GetValueOrDefault(LayerType.Interfaces),
            Functions = layers.GetValueOrDefault(LayerType.Functions),
            Host = layers.GetValueOrDefault(LayerType.Host),
            Conventions = conventions,
            InjectionPoints = injectionPoints.ToList(),
            FrontendRouterFiles = frontendRouters,
            FrontendRoutesFiles = frontendRoutes
        };
    }

    private static string? FindSubdirectory(string root, string name)
    {
        var path = Path.Combine(root, name);
        return Directory.Exists(path) ? path : null;
    }

    private static List<ProjectInfo> DiscoverProjects(string searchRoot)
    {
        var projects = new List<ProjectInfo>();

        try
        {
            var csprojFiles = Directory.GetFiles(searchRoot, "*.csproj", SearchOption.AllDirectories);

            foreach (var csproj in csprojFiles)
            {
                var projectDir = Path.GetDirectoryName(csproj)!;
                var projectName = Path.GetFileNameWithoutExtension(csproj);

                projects.Add(new ProjectInfo
                {
                    Path = projectDir,
                    ProjectName = projectName,
                    CsprojPath = csproj
                });
            }
        }
        catch
        {
            // Ignore errors during discovery
        }

        return projects;
    }

    private static IReadOnlyList<string> DiscoverFrontendRouters(string? appRoot)
    {
        var routers = new List<string>();

        if (string.IsNullOrEmpty(appRoot) || !Directory.Exists(appRoot))
            return routers;

        try
        {
            var files = Directory.GetFiles(appRoot, "*Router.*", SearchOption.AllDirectories)
                .Where(IsJsTsFile)
                .OrderByDescending(f => f.Contains(Path.Combine("routing", "routers"), StringComparison.OrdinalIgnoreCase))
                .ThenBy(f => f)
                .ToList();

            routers.AddRange(files);
        }
        catch
        {
            // ignore
        }

        return routers;
    }

    private static IReadOnlyList<string> DiscoverFrontendRoutes(string? appRoot)
    {
        var routes = new List<string>();

        if (string.IsNullOrEmpty(appRoot) || !Directory.Exists(appRoot))
            return routes;

        try
        {
            var files = Directory.GetFiles(appRoot, "*routes.*", SearchOption.AllDirectories)
                .Where(IsJsTsFile)
                .OrderByDescending(f => f.Contains(Path.Combine("routing", "routers"), StringComparison.OrdinalIgnoreCase))
                .ThenBy(f => f)
                .ToList();

            routes.AddRange(files);
        }
        catch
        {
            // ignore
        }

        return routes;
    }

    private static bool IsJsTsFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".js" or ".jsx" or ".ts" or ".tsx";
    }

    private Dictionary<LayerType, LayerInfo> CategorizeProjects(List<ProjectInfo> projects)
    {
        var layers = new Dictionary<LayerType, LayerInfo>();

        foreach (var project in projects)
        {
            var layerType = DetectLayerType(project.ProjectName);
            if (layerType == null)
                continue;

            // Skip if we already have this layer (take first match)
            if (layers.ContainsKey(layerType.Value))
                continue;

            var ns = _namespaceResolver.ResolveFromDirectory(project.Path)
                     ?? _namespaceResolver.InferFromProjectName(project.ProjectName);

            layers[layerType.Value] = new LayerInfo
            {
                Path = project.Path,
                Namespace = ns,
                ProjectName = project.ProjectName,
                ExtensionsPath = FindSubdirectory(project.Path, "Extensions"),
                EntitiesPath = FindSubdirectory(project.Path, "Entities"),
                MappersPath = FindSubdirectory(project.Path, "Mappers"),
                EndpointsPath = FindSubdirectory(project.Path, "Endpoints"),
                RoutesPath = FindSubdirectory(project.Path, "Routes")
            };
        }

        return layers;
    }

    private static LayerType? DetectLayerType(string projectName)
    {
        var nameLower = projectName.ToLowerInvariant();

        // Order matters - more specific patterns first
        if (nameLower.EndsWith(".host"))
            return LayerType.Host;
        if (nameLower.EndsWith(".functions"))
            return LayerType.Functions;
        if (nameLower.EndsWith(".api"))
            return LayerType.Api;
        if (nameLower.EndsWith(".services"))
            return LayerType.Services;
        if (nameLower.Contains(".repositories"))
            return LayerType.Repositories;
        if (nameLower.EndsWith(".models"))
            return LayerType.Models;
        if (nameLower.EndsWith(".interfaces"))
            return LayerType.Interfaces;

        return null;
    }

    private (string AppName, string BaseNamespace) DetectAppIdentity(List<ProjectInfo> projects)
    {
        // Try to extract app name from any project
        foreach (var project in projects)
        {
            var match = AppNameRegex().Match(project.ProjectName);
            if (match.Success)
            {
                var appName = match.Groups[1].Value;

                // Determine base namespace pattern
                // Check if project uses "LiveFree.Artemis.X" or "LiveFree.X" pattern
                var baseNs = project.ProjectName.Contains(".Artemis.")
                    ? $"LiveFree.Artemis.{appName}"
                    : $"LiveFree.{appName}";

                return (appName, baseNs);
            }
        }

        // Fallback: use folder name
        var folderName = projects.FirstOrDefault()?.Path;
        if (folderName != null)
        {
            var dirName = new DirectoryInfo(folderName).Parent?.Name ?? "Unknown";
            // Clean up app name (remove -app suffix if present)
            var appName = dirName.Replace("-app", "", StringComparison.OrdinalIgnoreCase);
            appName = ToPascalCase(appName);
            return (appName, $"LiveFree.{appName}");
        }

        return ("Unknown", "LiveFree.Unknown");
    }

    private async Task<NamingConventions> DetectConventionsAsync(
        Dictionary<LayerType, LayerInfo> layers,
        string appName,
        CancellationToken ct)
    {
        // Start with defaults
        var conventions = NamingConventions.CreateDefault(appName);

        // Try to detect actual naming from existing files
        if (layers.TryGetValue(LayerType.Api, out var apiLayer) && apiLayer.ExtensionsPath != null)
        {
            conventions = await DetectApiConventionsAsync(apiLayer.ExtensionsPath, conventions, ct);
        }

        if (layers.TryGetValue(LayerType.Repositories, out var repoLayer) && repoLayer.MappersPath != null)
        {
            conventions = await DetectMappingConventionsAsync(repoLayer.MappersPath, conventions, ct);
        }

        return conventions;
    }

    private async Task<NamingConventions> DetectApiConventionsAsync(
        string extensionsPath,
        NamingConventions current,
        CancellationToken ct)
    {
        try
        {
            // Find *ServicesExtension*.cs
            var servicesFiles = Directory.GetFiles(extensionsPath, "*ServicesExtension*.cs");
            if (servicesFiles.Length > 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(servicesFiles[0]);
                var content = await File.ReadAllTextAsync(servicesFiles[0], ct);

                // Extract method name
                var methodMatch = Regex.Match(content, @"public\s+static\s+\w+\s+(Add\w+Services)\s*\(");
                var methodName = methodMatch.Success ? methodMatch.Groups[1].Value : current.ServiceExtensionMethod;

                var usesSingular = !fileName.EndsWith("Extensions", StringComparison.Ordinal);

                current = current with
                {
                    ServiceExtensionClass = fileName,
                    ServiceExtensionMethod = methodName,
                    UsesSingularExtension = usesSingular
                };
            }

            // Find *RoutesExtension*.cs
            var routesFiles = Directory.GetFiles(extensionsPath, "*RoutesExtension*.cs");
            if (routesFiles.Length > 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(routesFiles[0]);
                var content = await File.ReadAllTextAsync(routesFiles[0], ct);

                // Extract method name
                var methodMatch = Regex.Match(content, @"public\s+static\s+\w+\s+(Add\w+Routes)\s*\(");
                var methodName = methodMatch.Success ? methodMatch.Groups[1].Value : current.RoutesExtensionMethod;

                current = current with
                {
                    RoutesExtensionClass = fileName,
                    RoutesExtensionMethod = methodName
                };
            }
        }
        catch
        {
            // Use defaults on error
        }

        return current;
    }

    private async Task<NamingConventions> DetectMappingConventionsAsync(
        string mappersPath,
        NamingConventions current,
        CancellationToken ct)
    {
        try
        {
            var mappingFiles = Directory.GetFiles(mappersPath, "*MappingProfile.cs");
            if (mappingFiles.Length > 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(mappingFiles[0]);
                current = current with { MappingProfileClass = fileName };
            }
        }
        catch
        {
            // Use defaults on error
        }

        return current;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Handle kebab-case and snake_case
        var words = input.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w =>
            char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private sealed class ProjectInfo
    {
        public required string Path { get; init; }
        public required string ProjectName { get; init; }
        public required string CsprojPath { get; init; }
    }
}
