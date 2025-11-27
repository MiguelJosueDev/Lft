using Lft.Ast.CSharp.Features.Injection.Models;

namespace Lft.Ast.CSharp.Features.Injection.Services;

public interface ICSharpInjectionService
{
    Task InjectIntoMethodAsync(CodeInjectionRequest request, CancellationToken cancellationToken = default);
}
