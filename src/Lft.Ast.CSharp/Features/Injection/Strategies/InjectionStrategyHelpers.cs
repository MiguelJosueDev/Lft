using Lft.Ast.CSharp.Features.Injection.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Strategies;

public static class InjectionStrategyHelpers
{
    public static int GetInsertIndexAfterDeclarations(SyntaxList<StatementSyntax> statements)
    {
        int insertIndex = 0;
        foreach (var stmt in statements)
        {
            if (stmt is LocalDeclarationStatementSyntax)
            {
                insertIndex++;
            }
            else
            {
                break;
            }
        }
        return insertIndex;
    }

    public static int GetInsertIndexBeforeReturn(SyntaxList<StatementSyntax> statements)
    {
        if (statements.Count > 0 && statements.Last() is ReturnStatementSyntax)
        {
            return statements.Count - 1;
        }
        return statements.Count;
    }

    public static string? GetMethodName(InvocationExpressionSyntax inv)
    {
        return inv.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Name switch
            {
                GenericNameSyntax gen => gen.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            },
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax gen => gen.Identifier.Text,
            _ => null
        };
    }

    public static InvocationExpressionSyntax? GetRootInvocation(ExpressionSyntax expr)
    {
        while (expr is InvocationExpressionSyntax inv)
        {
            if (inv.Expression is MemberAccessExpressionSyntax ma)
            {
                if (ma.Name is GenericNameSyntax gen && gen.Identifier.Text == "CreateMap")
                {
                    return inv;
                }
                expr = ma.Expression;
            }
            else if (inv.Expression is GenericNameSyntax gen && gen.Identifier.Text == "CreateMap")
            {
                return inv;
            }
            else
            {
                break;
            }
        }

        if (expr is InvocationExpressionSyntax directInv &&
            directInv.Expression is GenericNameSyntax directGen &&
            directGen.Identifier.Text == "CreateMap")
        {
            return directInv;
        }

        return null;
    }

    public static InvocationExpressionSyntax? GetRootInvocationForMatching(InvocationExpressionSyntax inv)
    {
        if (inv.Expression is MemberAccessExpressionSyntax ma)
        {
            if (ma.Expression is InvocationExpressionSyntax parentInv)
            {
                var parentName = GetMethodName(parentInv);
                if (parentName == "CreateMap" || parentName?.StartsWith("AddScoped") == true ||
                    parentName?.StartsWith("AddTransient") == true || parentName?.StartsWith("AddSingleton") == true)
                {
                    return parentInv;
                }
                return GetRootInvocationForMatching(parentInv) ?? parentInv;
            }
        }
        return inv;
    }

    public static List<string> GetTypeArguments(InvocationExpressionSyntax inv)
    {
        var typeArgs = new List<string>();

        GenericNameSyntax? genericName = inv.Expression switch
        {
            GenericNameSyntax gen => gen,
            MemberAccessExpressionSyntax ma when ma.Name is GenericNameSyntax gen => gen,
            _ => null
        };

        if (genericName?.TypeArgumentList != null)
        {
            foreach (var arg in genericName.TypeArgumentList.Arguments)
            {
                typeArgs.Add(arg.ToString());
            }
        }

        return typeArgs;
    }

    public static InvocationInfo? ExtractInvocationInfoFromStatement(StatementSyntax? statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return null;

        var invocation = expr.Expression as InvocationExpressionSyntax;
        if (invocation == null && expr.Expression is InvocationExpressionSyntax chainedInv)
        {
            invocation = chainedInv;
        }

        if (invocation == null) return null;

        var rootInv = GetRootInvocationForMatching(invocation);
        if (rootInv == null) rootInv = invocation;

        var methodName = GetMethodName(rootInv);
        if (methodName == null) return null;

        var typeArgs = GetTypeArguments(rootInv);

        return new InvocationInfo(methodName, typeArgs);
    }
}
