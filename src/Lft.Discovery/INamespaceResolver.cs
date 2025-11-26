namespace Lft.Discovery;

/// <summary>
/// Resolves namespaces from C# files.
/// </summary>
public interface INamespaceResolver
{
    /// <summary>
    /// Extracts the namespace from a C# file.
    /// </summary>
    /// <param name="csFilePath">Path to the .cs file.</param>
    /// <returns>The namespace or null if not found.</returns>
    string? ResolveFromFile(string csFilePath);

    /// <summary>
    /// Finds the most common namespace in a directory.
    /// </summary>
    /// <param name="directory">Directory to search.</param>
    /// <returns>The most common namespace or null if not found.</returns>
    string? ResolveFromDirectory(string directory);

    /// <summary>
    /// Infers namespace from project structure when no files are available.
    /// </summary>
    /// <param name="projectName">Name of the .csproj file (without extension).</param>
    /// <returns>Inferred namespace.</returns>
    string InferFromProjectName(string projectName);
}
