using Lft.Ast.CSharp.Features.Injection.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Strategies;

public class CreateMapInjectionStrategy : IInjectionStrategy
{
    public bool CanHandle(CodeInjectionPattern pattern)
    {
        return pattern == CodeInjectionPattern.CreateMapBlock;
    }

    public int FindInsertionIndex(BlockSyntax body)
    {
        // Find last CreateMap<> call
        int lastCreateMapIndex = -1;
        for (int i = 0; i < body.Statements.Count; i++)
        {
            if (IsCreateMapInvocation(body.Statements[i]))
            {
                lastCreateMapIndex = i;
            }
        }

        // Insert after last CreateMap or at end (before return if exists)
        if (lastCreateMapIndex >= 0)
        {
            return lastCreateMapIndex + 1;
        }

        return InjectionStrategyHelpers.GetInsertIndexBeforeReturn(body.Statements);
    }

    public bool Matches(InvocationInfo snippet, InvocationInfo existing)
    {
        // Match by type arguments only (ignore method name which is always CreateMap)
        // Actually, snippet.MethodName should be CreateMap too, but strict check:
        return snippet.TypeArguments.Count == existing.TypeArguments.Count &&
               snippet.TypeArguments.SequenceEqual(existing.TypeArguments);
    }

    private static bool IsCreateMapInvocation(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return false;

        // CreateMap calls may be chained: CreateMap<A, B>().ReverseMap();
        var invocation = InjectionStrategyHelpers.GetRootInvocation(expr.Expression);
        if (invocation == null) return false;

        var identifier = invocation.Expression switch
        {
            GenericNameSyntax gen => gen.Identifier.Text,
            MemberAccessExpressionSyntax ma when ma.Name is GenericNameSyntax gen => gen.Identifier.Text,
            _ => null
        };

        return identifier == "CreateMap";
    }
}
