namespace Lft.Discovery;

/// <summary>
/// Auto-detected naming conventions for a project.
/// </summary>
public sealed record NamingConventions
{
    /// <summary>
    /// Class name for the API services extension.
    /// Example: "AccountsServicesExtensions" or "CellularServicesExtension"
    /// </summary>
    public required string ServiceExtensionClass { get; init; }

    /// <summary>
    /// Method name for adding API services.
    /// Example: "AddAccountsServices"
    /// </summary>
    public required string ServiceExtensionMethod { get; init; }

    /// <summary>
    /// Class name for the routes extension.
    /// Example: "AccountsRoutesExtensions"
    /// </summary>
    public required string RoutesExtensionClass { get; init; }

    /// <summary>
    /// Method name for adding routes.
    /// Example: "AddAccountsRoutes"
    /// </summary>
    public required string RoutesExtensionMethod { get; init; }

    /// <summary>
    /// Method name for adding repositories.
    /// Example: "AddAccountsRepositories"
    /// </summary>
    public required string RepoExtensionMethod { get; init; }

    /// <summary>
    /// Class name for the AutoMapper profile.
    /// Example: "AccountsMappingProfile" or "TransactionMappingProfile"
    /// </summary>
    public required string MappingProfileClass { get; init; }

    /// <summary>
    /// Constructor name for the mapping profile (usually same as class name).
    /// </summary>
    public string MappingProfileConstructor => MappingProfileClass;

    /// <summary>
    /// Whether the project uses singular "Extension" vs plural "Extensions".
    /// </summary>
    public bool UsesSingularExtension { get; init; }

    /// <summary>
    /// Creates conventions with inferred values based on app name.
    /// </summary>
    public static NamingConventions CreateDefault(string appName)
    {
        return new NamingConventions
        {
            ServiceExtensionClass = $"{appName}ServicesExtensions",
            ServiceExtensionMethod = $"Add{appName}Services",
            RoutesExtensionClass = $"{appName}RoutesExtensions",
            RoutesExtensionMethod = $"Add{appName}Routes",
            RepoExtensionMethod = $"Add{appName}Repositories",
            MappingProfileClass = $"{appName}MappingProfile",
            UsesSingularExtension = false
        };
    }
}
