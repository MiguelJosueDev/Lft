namespace Lft.Domain.Services;

/// <summary>
/// Result of resolving a path for a generated file.
/// </summary>
/// <param name="Directory">The resolved directory path.</param>
/// <param name="Namespace">The namespace detected from existing files, if any.</param>
public record PathResolutionResult(string Directory, string? Namespace);

/// <summary>
/// Resolves paths for generated files based on the existing project structure.
/// </summary>
public interface IPathResolver
{
    /// <summary>
    /// Resolves the best directory for a file based on its suffix and the existing project structure.
    /// </summary>
    /// <param name="fileSuffix">The suffix to look for (e.g., "Model.cs", "Repository.cs").</param>
    /// <param name="rootDirectory">The root directory to scan.</param>
    /// <returns>The resolution result containing directory and namespace, or null if no match found.</returns>
    PathResolutionResult? Resolve(string fileSuffix, string rootDirectory);
}
