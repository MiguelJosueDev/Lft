using Lft.Ast.CSharp.Features.Injection.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Strategies;

public interface IInjectionStrategy
{
    bool CanHandle(CodeInjectionPattern pattern);
    int FindInsertionIndex(BlockSyntax body);
    bool Matches(InvocationInfo snippet, InvocationInfo existing);
}
