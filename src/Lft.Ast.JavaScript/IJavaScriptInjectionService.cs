namespace Lft.Ast.JavaScript;

public interface IJavaScriptInjectionService
{
    /// <summary>
    /// Injects an import statement into a JavaScript/TypeScript file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="importStatement">The import statement to inject.</param>
    Task InjectImportAsync(string filePath, string importStatement);

    /// <summary>
    /// Injects a code snippet into an array in a JavaScript/TypeScript file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="arrayName">The name of the array variable or export.</param>
    /// <param name="snippet">The code snippet to inject into the array.</param>
    Task InjectIntoArrayAsync(string filePath, string arrayName, string snippet);
}
