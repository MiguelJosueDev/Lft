using Lft.Domain.Services;

namespace Lft.Ast.CSharp;

/// <summary>
/// Adapter that implements ICodeInjector using CSharpInjectionService.
/// </summary>
public sealed class CSharpCodeInjector : ICodeInjector
{
    private readonly CSharpInjectionService _service = new();

    public bool CanHandle(string filePath)
    {
        return filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    public string Inject(InjectionContext context)
    {
        var position = context.Position == InjectionPosition.Beginning
            ? CodeInjectionPosition.Beginning
            : CodeInjectionPosition.End;

        var pattern = DetectPattern(context.Snippet);

        return _service.InjectIntoMethodSource(
            context.SourceCode,
            context.ClassSuffix,
            context.MethodName,
            context.Snippet,
            position,
            pattern
        );
    }

    /// <summary>
    /// Auto-detect the appropriate injection pattern based on snippet content.
    /// </summary>
    private static CodeInjectionPattern DetectPattern(string snippet)
    {
        if (snippet.Contains("AddScoped") ||
            snippet.Contains("AddTransient") ||
            snippet.Contains("AddSingleton"))
        {
            return CodeInjectionPattern.AddScopedBlock;
        }

        if (snippet.Contains("CreateMap<"))
        {
            return CodeInjectionPattern.CreateMapBlock;
        }

        // Detect Map*Routes patterns (e.g., app.MapUsersRoutes, MapAccountsRoutes)
        if (snippet.Contains(".Map") && snippet.Contains("Routes"))
        {
            return CodeInjectionPattern.MapRoutesBlock;
        }

        return CodeInjectionPattern.Default;
    }
}
