namespace Lft.Discovery;

/// <summary>
/// Locates injection points in a project.
/// </summary>
public interface IInjectionPointLocator
{
    /// <summary>
    /// Locates all injection points of a specific type in the search root.
    /// </summary>
    /// <param name="searchRoot">Root directory to search.</param>
    /// <param name="target">Type of injection target to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of discovered injection points.</returns>
    Task<IReadOnlyList<InjectionPoint>> LocateAsync(
        string searchRoot,
        InjectionTarget target,
        CancellationToken ct = default);

    /// <summary>
    /// Locates all injection points in the search root.
    /// </summary>
    /// <param name="searchRoot">Root directory to search.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all discovered injection points.</returns>
    Task<IReadOnlyList<InjectionPoint>> LocateAllAsync(
        string searchRoot,
        CancellationToken ct = default);
}
