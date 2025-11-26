using System.Text.RegularExpressions;
using Lft.Domain.Services;

namespace Lft.App.Services;

// Keep the old interface for backward compatibility with existing code
public record ResolutionResult(string Directory, string? Namespace);

public interface ISmartPathResolver
{
    ResolutionResult? Resolve(string fileSuffix, string rootDirectory, string? pathHint = null);
}

public class SuffixBasedPathResolver : ISmartPathResolver, IPathResolver
{
    private static readonly string[] ExcludedDirs =
    {
        "node_modules", "bin", "obj", ".git", "dist", "coverage", ".vs", "packages", ".idea", "TestResults"
    };

    // Map suffix patterns to conventional folder names
    private static readonly Dictionary<string, string[]> SuffixToConventionalFolders = new()
    {
        { "Model.cs", new[] { "Models", "Domain", "Dtos" } },
        { "Entity.cs", new[] { "Entities", "Domain", "Data" } },
        { "Repository.cs", new[] { "Repositories", "Data", "Infrastructure" } },
        { "Service.cs", new[] { "Services", "Application" } },
        { "Interface.cs", new[] { "Interfaces", "Abstractions", "Contracts" } },
        { "Endpoint.cs", new[] { "Endpoints", "Api", "Controllers" } },
        { "Controller.cs", new[] { "Controllers", "Api", "Endpoints" } },
        { "Routes.cs", new[] { "Routes", "Extensions", "Api" } },
        { ".jsx", new[] { "components", "views", "controllers", "features" } },
        { ".js", new[] { "services", "utils", "helpers", "core" } },
    };

    // ISmartPathResolver implementation (for existing code)
    public ResolutionResult? Resolve(string fileSuffix, string rootDirectory, string? pathHint = null)
    {
        var result = ResolveInternal(fileSuffix, rootDirectory);
        return result != null ? new ResolutionResult(result.Directory, result.Namespace) : null;
    }

    // IPathResolver implementation (for StepExecutor)
    PathResolutionResult? IPathResolver.Resolve(string fileSuffix, string rootDirectory)
    {
        return ResolveInternal(fileSuffix, rootDirectory);
    }

    private PathResolutionResult? ResolveInternal(string fileSuffix, string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return null;
        }

        // Strategy 1: Find by existing files with same suffix
        var bySuffix = FindBySuffix(fileSuffix, rootDirectory);
        if (bySuffix != null)
        {
            return bySuffix;
        }

        // Strategy 2: Find by conventional folder names
        var byConvention = FindByConvention(fileSuffix, rootDirectory);
        if (byConvention != null)
        {
            return byConvention;
        }

        // Strategy 3: Return null - caller should handle fallback (ask user or use template default)
        return null;
    }

    private PathResolutionResult? FindBySuffix(string fileSuffix, string rootDirectory)
    {
        try
        {
            var matchingFiles = Directory.EnumerateFiles(rootDirectory, $"*{fileSuffix}", SearchOption.AllDirectories)
                .Where(f => !IsExcludedPath(f))
                .ToList();

            if (!matchingFiles.Any())
            {
                return null;
            }

            // Find the best match (directory with most files of this type)
            var bestMatchFile = matchingFiles
                .GroupBy(Path.GetDirectoryName)
                .OrderByDescending(g => g.Count())
                .First()
                .First();

            var directory = Path.GetDirectoryName(bestMatchFile)!;
            var namespaceName = ExtractNamespace(bestMatchFile);

            return new PathResolutionResult(directory, namespaceName);
        }
        catch
        {
            return null;
        }
    }

    private PathResolutionResult? FindByConvention(string fileSuffix, string rootDirectory)
    {
        // Find which conventional folders to look for based on suffix
        var conventionalFolders = SuffixToConventionalFolders
            .Where(kvp => fileSuffix.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            .SelectMany(kvp => kvp.Value)
            .Distinct()
            .ToArray();

        if (!conventionalFolders.Any())
        {
            return null;
        }

        try
        {
            // Search for directories matching conventional names
            var allDirs = Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                .Where(d => !IsExcludedPath(d))
                .ToList();

            foreach (var conventionalName in conventionalFolders)
            {
                var matchingDir = allDirs
                    .FirstOrDefault(d => Path.GetFileName(d).Equals(conventionalName, StringComparison.OrdinalIgnoreCase));

                if (matchingDir != null)
                {
                    // Try to extract namespace from any .cs file in that directory
                    var anyFile = Directory.EnumerateFiles(matchingDir, "*.cs").FirstOrDefault();
                    var namespaceName = anyFile != null ? ExtractNamespace(anyFile) : null;

                    return new PathResolutionResult(matchingDir, namespaceName);
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    private bool IsExcludedPath(string path)
    {
        return ExcludedDirs.Any(excluded =>
            path.Contains($"{Path.DirectorySeparatorChar}{excluded}{Path.DirectorySeparatorChar}") ||
            path.Contains($"{Path.AltDirectorySeparatorChar}{excluded}{Path.AltDirectorySeparatorChar}"));
    }

    private string? ExtractNamespace(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            // Matches: namespace My.Namespace; or namespace My.Namespace {
            var match = Regex.Match(content, @"^\s*namespace\s+([\w\.]+)", RegexOptions.Multiline);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch
        {
            // Ignore errors reading file
        }

        return null;
    }
}
