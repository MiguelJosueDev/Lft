namespace Lft.Discovery;

/// <summary>
/// Analyzes a project structure and generates a manifest.
/// </summary>
public interface IProjectAnalyzer
{
    /// <summary>
    /// Analyzes the project at the given profile root and returns a manifest.
    /// </summary>
    /// <param name="profileRoot">Root path of the profile (where lft.config.json is located).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ProjectManifest containing all discovered information.</returns>
    Task<ProjectManifest> AnalyzeAsync(string profileRoot, CancellationToken ct = default);
}
