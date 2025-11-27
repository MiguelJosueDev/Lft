namespace Lft.Discovery;

/// <summary>
/// Complete manifest of a discovered project structure.
/// Contains all information needed for code generation and injection.
/// </summary>
public sealed record ProjectManifest
{
    /// <summary>
    /// The application name (e.g., "Accounts", "Transactions", "Cellular").
    /// </summary>
    public required string AppName { get; init; }

    /// <summary>
    /// Base namespace prefix for the application.
    /// Example: "LiveFree.Accounts", "LiveFree.Artemis.Ticketing"
    /// </summary>
    public required string BaseNamespace { get; init; }

    /// <summary>
    /// Root path of the profile (where lft.config.json is located).
    /// </summary>
    public required string ProfileRoot { get; init; }

    /// <summary>
    /// Path to the API folder (usually profileRoot/api).
    /// </summary>
    public required string ApiRoot { get; init; }

    /// <summary>
    /// Path to the frontend app folder (usually profileRoot/app).
    /// </summary>
    public string? AppRoot { get; init; }

    // Layer Information
    public LayerInfo? Api { get; init; }
    public LayerInfo? Services { get; init; }
    public LayerInfo? Repositories { get; init; }
    public LayerInfo? Models { get; init; }
    public LayerInfo? Interfaces { get; init; }
    public LayerInfo? Functions { get; init; }
    public LayerInfo? Host { get; init; }

    /// <summary>
    /// Auto-detected naming conventions.
    /// </summary>
    public required NamingConventions Conventions { get; init; }

    /// <summary>
    /// Discovered injection points for code generation.
    /// </summary>
    public IReadOnlyList<InjectionPoint> InjectionPoints { get; init; } = [];

    /// <summary>
    /// Discovered frontend router entry points (JS/TS files).
    /// </summary>
    public IReadOnlyList<string> FrontendRouterFiles { get; init; } = [];

    /// <summary>
    /// Discovered frontend route array files (JS/TS files).
    /// </summary>
    public IReadOnlyList<string> FrontendRoutesFiles { get; init; } = [];

    /// <summary>
    /// Gets the layer info by type.
    /// </summary>
    public LayerInfo? GetLayer(LayerType type) => type switch
    {
        LayerType.Api => Api,
        LayerType.Services => Services,
        LayerType.Repositories => Repositories,
        LayerType.Models => Models,
        LayerType.Interfaces => Interfaces,
        LayerType.Functions => Functions,
        LayerType.Host => Host,
        _ => null
    };

    /// <summary>
    /// Gets the injection point for a specific target.
    /// </summary>
    public InjectionPoint? GetInjectionPoint(InjectionTarget target)
        => InjectionPoints.FirstOrDefault(p => p.Target == target);

    /// <summary>
    /// Converts the manifest to a dictionary for template variables.
    /// </summary>
    public Dictionary<string, object?> ToVariables()
    {
        var vars = new Dictionary<string, object?>
        {
            // Identity
            ["_AppName"] = AppName,
            ["_BaseNamespace"] = BaseNamespace,
            ["_ProfileRoot"] = ProfileRoot,
            ["_ApiRoot"] = ApiRoot,
            ["_AppRoot"] = AppRoot,

            // Naming conventions
            ["_ServiceExtensionClass"] = Conventions.ServiceExtensionClass,
            ["_ServiceExtensionMethod"] = Conventions.ServiceExtensionMethod,
            ["_RoutesExtensionClass"] = Conventions.RoutesExtensionClass,
            ["_RoutesExtensionMethod"] = Conventions.RoutesExtensionMethod,
            ["_RepoExtensionMethod"] = Conventions.RepoExtensionMethod,
            ["_MappingProfileClass"] = Conventions.MappingProfileClass,
            ["_UsesSingularExtension"] = Conventions.UsesSingularExtension,

            // Architecture patterns
            ["_UsesInterfacesLayer"] = Interfaces != null,
            ["_ServiceInterfacesLayer"] = Interfaces != null ? Interfaces : Services,
            ["_ExtensionSuffix"] = Conventions.UsesSingularExtension ? "Extension" : "Extensions",
        };

        // Add layer paths and namespaces
        AddLayerVariables(vars, "Api", Api);
        AddLayerVariables(vars, "Services", Services);
        AddLayerVariables(vars, "Repositories", Repositories);
        AddLayerVariables(vars, "Models", Models);
        AddLayerVariables(vars, "Interfaces", Interfaces);
        AddLayerVariables(vars, "Functions", Functions);
        AddLayerVariables(vars, "Host", Host);

        // Add injection point paths
        foreach (var point in InjectionPoints)
        {
            var targetName = point.Target.ToString();
            vars[$"_Inject{targetName}Path"] = point.FilePath;
            vars[$"_Inject{targetName}Class"] = point.ClassName;
            vars[$"_Inject{targetName}Method"] = point.MethodName;
        }

        if (FrontendRouterFiles.Count > 0)
        {
            vars["_FrontendRouterFile"] = FrontendRouterFiles[0];
            vars["_FrontendRouterFiles"] = FrontendRouterFiles;
        }

        if (FrontendRoutesFiles.Count > 0)
        {
            vars["_FrontendRoutesFile"] = FrontendRoutesFiles[0];
            vars["_FrontendRoutesFiles"] = FrontendRoutesFiles;
        }

        return vars;
    }

    private static void AddLayerVariables(Dictionary<string, object?> vars, string name, LayerInfo? layer)
    {
        if (layer == null) return;

        vars[$"_{name}Path"] = layer.Path;
        vars[$"_{name}Namespace"] = layer.Namespace;
        vars[$"_{name}ProjectName"] = layer.ProjectName;

        if (layer.ExtensionsPath != null)
            vars[$"_{name}ExtensionsPath"] = layer.ExtensionsPath;
        if (layer.EntitiesPath != null)
            vars[$"_{name}EntitiesPath"] = layer.EntitiesPath;
        if (layer.MappersPath != null)
            vars[$"_{name}MappersPath"] = layer.MappersPath;
        if (layer.EndpointsPath != null)
            vars[$"_{name}EndpointsPath"] = layer.EndpointsPath;
        if (layer.RoutesPath != null)
            vars[$"_{name}RoutesPath"] = layer.RoutesPath;
    }
}
