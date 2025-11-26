namespace Lft.Domain.Models;

public sealed class GenerationResult
{
    public IReadOnlyList<GeneratedFile> Files { get; }

    /// <summary>
    /// Variables resolved from profile configuration (MainModuleName, RoutePattern, etc.)
    /// </summary>
    public IReadOnlyDictionary<string, object?> Variables { get; }

    public GenerationResult(IReadOnlyList<GeneratedFile> files, IReadOnlyDictionary<string, object?>? variables = null)
    {
        Files = files ?? throw new ArgumentNullException(nameof(files));
        Variables = variables ?? new Dictionary<string, object?>();
    }

    public static GenerationResult Empty { get; } = new(Array.Empty<GeneratedFile>());

    /// <summary>
    /// Gets a variable value with optional default.
    /// </summary>
    public string GetVariable(string key, string defaultValue = "")
    {
        return Variables.TryGetValue(key, out var value) && value is string str
            ? str
            : defaultValue;
    }
}
