using Lft.Ast.CSharp.Features.Injection.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Strategies;

public class DefaultInjectionStrategy : IInjectionStrategy
{
    private readonly CodeInjectionPosition _position;

    public DefaultInjectionStrategy(CodeInjectionPosition position)
    {
        _position = position;
    }

    public bool CanHandle(CodeInjectionPattern pattern)
    {
        return pattern == CodeInjectionPattern.Default;
    }

    public int FindInsertionIndex(BlockSyntax body)
    {
        return _position == CodeInjectionPosition.Beginning
            ? InjectionStrategyHelpers.GetInsertIndexAfterDeclarations(body.Statements)
            : InjectionStrategyHelpers.GetInsertIndexBeforeReturn(body.Statements);
    }

    public bool Matches(InvocationInfo snippet, InvocationInfo existing)
    {
        // Default: match by method name + type arguments
        return snippet.MethodName == existing.MethodName &&
               snippet.TypeArguments.Count == existing.TypeArguments.Count &&
               snippet.TypeArguments.SequenceEqual(existing.TypeArguments);
    }
}
