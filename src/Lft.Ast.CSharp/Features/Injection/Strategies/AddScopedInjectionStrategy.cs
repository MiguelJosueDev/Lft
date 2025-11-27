using Lft.Ast.CSharp.Features.Injection.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Strategies;

public class AddScopedInjectionStrategy : IInjectionStrategy
{
    public bool CanHandle(CodeInjectionPattern pattern)
    {
        return pattern == CodeInjectionPattern.AddScopedBlock;
    }

    public int FindInsertionIndex(BlockSyntax body)
    {
        // Find the LAST contiguous block of AddScoped/AddTransient/AddSingleton calls
        int lastServiceRegIndex = -1;

        for (int i = 0; i < body.Statements.Count; i++)
        {
            if (IsServiceRegistrationInvocation(body.Statements[i]))
            {
                lastServiceRegIndex = i;
            }
        }

        // If we found service registrations, insert after the last one
        if (lastServiceRegIndex >= 0)
        {
            return lastServiceRegIndex + 1;
        }

        // No service registration found - insert at beginning (after local declarations)
        return InjectionStrategyHelpers.GetInsertIndexAfterDeclarations(body.Statements);
    }

    public bool Matches(InvocationInfo snippet, InvocationInfo existing)
    {
        // Match by method name + type arguments
        return snippet.MethodName == existing.MethodName &&
               snippet.TypeArguments.Count == existing.TypeArguments.Count &&
               snippet.TypeArguments.SequenceEqual(existing.TypeArguments);
    }

    private static bool IsServiceRegistrationInvocation(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return false;
        if (expr.Expression is not InvocationExpressionSyntax inv) return false;

        var methodName = InjectionStrategyHelpers.GetMethodName(inv);
        if (methodName == null) return false;

        return methodName.StartsWith("AddScoped") ||
               methodName.StartsWith("AddTransient") ||
               methodName.StartsWith("AddSingleton");
    }
}
