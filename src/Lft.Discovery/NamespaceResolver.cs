using System.Text.RegularExpressions;

namespace Lft.Discovery;

/// <summary>
/// Resolves namespaces from C# source files.
/// </summary>
public sealed partial class NamespaceResolver : INamespaceResolver
{
    // Matches: namespace Some.Namespace; or namespace Some.Namespace { } or namespace Some.Namespace (malformed)
    // The pattern matches namespace followed by an identifier, optionally followed by semicolon or brace
    [GeneratedRegex(@"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline)]
    private static partial Regex NamespaceRegex();

    public string? ResolveFromFile(string csFilePath)
    {
        if (!File.Exists(csFilePath))
            return null;

        try
        {
            var content = File.ReadAllText(csFilePath);
            var match = NamespaceRegex().Match(content);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    public string? ResolveFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        // Look for .cs files, prioritizing Extensions folder
        var searchPaths = new[]
        {
            Path.Combine(directory, "Extensions"),
            directory
        };

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
                continue;

            var csFiles = Directory.GetFiles(searchPath, "*.cs", SearchOption.TopDirectoryOnly);

            foreach (var file in csFiles)
            {
                var ns = ResolveFromFile(file);
                if (ns != null)
                {
                    // Remove .Extensions suffix if present to get base namespace
                    if (ns.EndsWith(".Extensions"))
                        return ns[..^".Extensions".Length];
                    return ns;
                }
            }
        }

        return null;
    }

    public string InferFromProjectName(string projectName)
    {
        // Project name is typically the namespace
        // Example: "LiveFree.Accounts.Api" -> "LiveFree.Accounts.Api"
        return projectName;
    }
}
