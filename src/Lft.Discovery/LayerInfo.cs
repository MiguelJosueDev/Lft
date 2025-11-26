namespace Lft.Discovery;

/// <summary>
/// Information about a discovered layer (Api, Services, Repositories, etc.)
/// </summary>
public sealed record LayerInfo
{
    /// <summary>
    /// Full path to the layer's project folder.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Detected namespace for this layer.
    /// Example: "LiveFree.Accounts.Api"
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Name of the .csproj file (without extension).
    /// Example: "LiveFree.Accounts.Api"
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Path to the Extensions folder within this layer (if exists).
    /// </summary>
    public string? ExtensionsPath { get; init; }

    /// <summary>
    /// Path to the Entities folder within this layer (if exists).
    /// </summary>
    public string? EntitiesPath { get; init; }

    /// <summary>
    /// Path to the Mappers folder within this layer (if exists).
    /// </summary>
    public string? MappersPath { get; init; }

    /// <summary>
    /// Path to the Endpoints folder within this layer (if exists).
    /// </summary>
    public string? EndpointsPath { get; init; }

    /// <summary>
    /// Path to the Routes folder within this layer (if exists).
    /// </summary>
    public string? RoutesPath { get; init; }
}

/// <summary>
/// Type of layer in the architecture.
/// </summary>
public enum LayerType
{
    Api,
    Services,
    Repositories,
    Models,
    Interfaces,
    Functions,
    Host
}
