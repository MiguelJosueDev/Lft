namespace Lft.Domain.Services;

/// <summary>
/// Abstraction for injecting code snippets into existing source files.
/// Implementations handle specific languages (C#, JavaScript, etc.)
/// </summary>
public interface ICodeInjector
{
    /// <summary>
    /// Checks if this injector can handle the given file.
    /// </summary>
    bool CanHandle(string filePath);

    /// <summary>
    /// Injects a code snippet into the target file.
    /// Returns the modified source code (does not write to disk).
    /// </summary>
    string Inject(InjectionContext context);
}

public sealed record InjectionContext(
    string SourceCode,
    string ClassSuffix,
    string MethodName,
    string Snippet,
    InjectionPosition Position = InjectionPosition.End
);

public enum InjectionPosition
{
    Beginning,
    End
}
