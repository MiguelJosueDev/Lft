namespace Lft.Ast.CSharp;

public interface ICSharpInjectionService
{
    Task InjectIntoMethodAsync(CodeInjectionRequest request, CancellationToken cancellationToken = default);
}
