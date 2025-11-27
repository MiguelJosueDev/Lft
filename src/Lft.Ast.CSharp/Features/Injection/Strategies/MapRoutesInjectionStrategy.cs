using Lft.Ast.CSharp.Features.Injection.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Strategies;

public class MapRoutesInjectionStrategy : IInjectionStrategy
{
    public bool CanHandle(CodeInjectionPattern pattern)
    {
        return pattern == CodeInjectionPattern.MapRoutesBlock;
    }

    public int FindInsertionIndex(BlockSyntax body)
    {
        // Find last Map*Routes call
        int lastMapRoutesIndex = -1;
        for (int i = 0; i < body.Statements.Count; i++)
        {
            if (IsMapRoutesInvocation(body.Statements[i]))
            {
                lastMapRoutesIndex = i;
            }
        }

        // Insert after last MapRoutes or at end (before return if exists)
        if (lastMapRoutesIndex >= 0)
        {
            return lastMapRoutesIndex + 1;
        }

        return InjectionStrategyHelpers.GetInsertIndexBeforeReturn(body.Statements);
    }

    public bool Matches(InvocationInfo snippet, InvocationInfo existing)
    {
        // Match by method name + type arguments
        return snippet.MethodName == existing.MethodName &&
               snippet.TypeArguments.Count == existing.TypeArguments.Count &&
               snippet.TypeArguments.SequenceEqual(existing.TypeArguments);
    }

    private static bool IsMapRoutesInvocation(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return false;
        if (expr.Expression is not InvocationExpressionSyntax inv) return false;

        var methodName = InjectionStrategyHelpers.GetMethodName(inv);
        if (methodName == null) return false;

        // Match Map*Routes patterns (MapAccountsRoutes, MapUsersRoutes, etc.)
        return methodName.StartsWith("Map") && methodName.EndsWith("Routes");
    }
}
