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

        return _service.InjectIntoMethodSource(
            context.SourceCode,
            context.ClassSuffix,
            context.MethodName,
            context.Snippet,
            position
        );
    }
}
