using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp;

public class CSharpInjectionService : ICSharpInjectionService
{
    public async Task InjectIntoMethodAsync(CodeInjectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException($"File not found: {request.FilePath}");
        }

        var sourceCode = await File.ReadAllTextAsync(request.FilePath, cancellationToken);
        var newSourceCode = InjectIntoMethodSource(
            sourceCode,
            request.ClassNameSuffix,
            request.MethodName,
            request.Snippet,
            request.Position,
            request.Pattern
        );

        if (sourceCode != newSourceCode)
        {
            await File.WriteAllTextAsync(request.FilePath, newSourceCode, cancellationToken);
        }
    }

    public string InjectIntoMethodSource(
        string sourceCode,
        string classNameSuffix,
        string methodName,
        string snippet,
        CodeInjectionPosition position,
        CodeInjectionPattern pattern = CodeInjectionPattern.Default)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        // 1. Find Class
        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text.EndsWith(classNameSuffix));

        if (classDecl == null)
        {
            throw new InvalidOperationException($"Class ending with '{classNameSuffix}' not found.");
        }

        // 2. Find the method OR constructor
        var method = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == methodName);

        ConstructorDeclarationSyntax? constructor = null;
        if (method == null)
        {
            constructor = classDecl.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == methodName);
        }

        if (method == null && constructor == null)
        {
            throw new InvalidOperationException($"Method or Constructor '{methodName}' not found in class '{classDecl.Identifier.Text}'.");
        }

        // 3. Get the body
        BlockSyntax? body = method?.Body ?? constructor?.Body;
        SyntaxNode? memberToReplace = (SyntaxNode?)method ?? constructor;

        if (body == null || memberToReplace == null)
        {
            throw new InvalidOperationException($"Method/Constructor '{methodName}' does not have a body.");
        }

        // 4. Check Idempotency using AST-based comparison
        if (StatementAlreadyExists(body.Statements, snippet, pattern))
        {
            return sourceCode;
        }

        // 5. Create New Statement with proper indentation
        var existingIndent = GetBodyIndentation(body);
        var newStatement = SyntaxFactory.ParseStatement(snippet)
            .WithLeadingTrivia(SyntaxFactory.Whitespace(existingIndent))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // 6. Determine insertion index based on pattern
        int insertIndex = pattern switch
        {
            CodeInjectionPattern.AddScopedBlock => FindAddScopedBlockInsertionIndex(body.Statements),
            CodeInjectionPattern.CreateMapBlock => FindCreateMapBlockInsertionIndex(body.Statements),
            CodeInjectionPattern.MapRoutesBlock => FindMapRoutesBlockInsertionIndex(body.Statements),
            _ => position == CodeInjectionPosition.Beginning
                 ? GetInsertIndexAfterDeclarations(body.Statements)
                 : GetInsertIndexBeforeReturn(body.Statements)
        };

        // 7. Create new body with the statement inserted
        var newBody = body.WithStatements(body.Statements.Insert(insertIndex, newStatement));

        // 8. Replace the old body with the new one
        SyntaxNode newRoot;
        if (method != null)
        {
            var newMethod = method.WithBody(newBody);
            newRoot = root.ReplaceNode(method, newMethod);
        }
        else
        {
            var newConstructor = constructor!.WithBody(newBody);
            newRoot = root.ReplaceNode(constructor, newConstructor);
        }

        return newRoot.ToFullString();
    }

    #region Semantic Block Detection

    private static int FindAddScopedBlockInsertionIndex(SyntaxList<StatementSyntax> statements)
    {
        // Find the LAST contiguous block of AddScoped/AddTransient/AddSingleton calls
        // We want to insert at the end of the last block, not the first block
        int lastServiceRegIndex = -1;

        for (int i = 0; i < statements.Count; i++)
        {
            if (IsServiceRegistrationInvocation(statements[i]))
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
        return GetInsertIndexAfterDeclarations(statements);
    }

    private static bool IsServiceRegistrationInvocation(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return false;
        if (expr.Expression is not InvocationExpressionSyntax inv) return false;

        var methodName = GetMethodName(inv);
        if (methodName == null) return false;

        return methodName.StartsWith("AddScoped") ||
               methodName.StartsWith("AddTransient") ||
               methodName.StartsWith("AddSingleton");
    }

    private static int FindCreateMapBlockInsertionIndex(SyntaxList<StatementSyntax> statements)
    {
        // Find last CreateMap<> call
        int lastCreateMapIndex = -1;
        for (int i = 0; i < statements.Count; i++)
        {
            if (IsCreateMapInvocation(statements[i]))
            {
                lastCreateMapIndex = i;
            }
        }

        // Insert after last CreateMap or at end (before return if exists)
        if (lastCreateMapIndex >= 0)
        {
            return lastCreateMapIndex + 1;
        }

        return GetInsertIndexBeforeReturn(statements);
    }

    private static int FindMapRoutesBlockInsertionIndex(SyntaxList<StatementSyntax> statements)
    {
        // Find last Map*Routes call
        int lastMapRoutesIndex = -1;
        for (int i = 0; i < statements.Count; i++)
        {
            if (IsMapRoutesInvocation(statements[i]))
            {
                lastMapRoutesIndex = i;
            }
        }

        // Insert after last MapRoutes or at end (before return if exists)
        if (lastMapRoutesIndex >= 0)
        {
            return lastMapRoutesIndex + 1;
        }

        return GetInsertIndexBeforeReturn(statements);
    }

    private static bool IsMapRoutesInvocation(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return false;
        if (expr.Expression is not InvocationExpressionSyntax inv) return false;

        var methodName = GetMethodName(inv);
        if (methodName == null) return false;

        // Match Map*Routes patterns (MapAccountsRoutes, MapUsersRoutes, etc.)
        return methodName.StartsWith("Map") && methodName.EndsWith("Routes");
    }

    private static bool IsCreateMapInvocation(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return false;

        // CreateMap calls may be chained: CreateMap<A, B>().ReverseMap();
        var invocation = GetRootInvocation(expr.Expression);
        if (invocation == null) return false;

        var identifier = invocation.Expression switch
        {
            GenericNameSyntax gen => gen.Identifier.Text,
            MemberAccessExpressionSyntax ma when ma.Name is GenericNameSyntax gen => gen.Identifier.Text,
            _ => null
        };

        return identifier == "CreateMap";
    }

    private static InvocationExpressionSyntax? GetRootInvocation(ExpressionSyntax expr)
    {
        // Walk down the chain to find the root invocation
        // e.g., CreateMap<A, B>().ReverseMap() -> CreateMap<A, B>()
        while (expr is InvocationExpressionSyntax inv)
        {
            if (inv.Expression is MemberAccessExpressionSyntax ma)
            {
                // Check if this is the CreateMap call
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

        // Direct CreateMap<A, B>() call
        if (expr is InvocationExpressionSyntax directInv &&
            directInv.Expression is GenericNameSyntax directGen &&
            directGen.Identifier.Text == "CreateMap")
        {
            return directInv;
        }

        return null;
    }

    private static string? GetMethodName(InvocationExpressionSyntax inv)
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

    #endregion

    #region AST-Based Idempotency

    private static bool StatementAlreadyExists(
        SyntaxList<StatementSyntax> statements,
        string snippet,
        CodeInjectionPattern pattern)
    {
        // Parse the snippet to extract invocation info
        var snippetInfo = ExtractInvocationInfo(snippet);
        if (snippetInfo == null)
        {
            // Fallback to string comparison if we can't parse
            var snippetTrimmed = snippet.Trim();
            return statements.Any(s => s.ToString().Contains(snippetTrimmed));
        }

        // Check if any existing statement matches
        foreach (var stmt in statements)
        {
            var existingInfo = ExtractInvocationInfoFromStatement(stmt);
            if (existingInfo != null && InvocationsMatch(snippetInfo, existingInfo, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static InvocationInfo? ExtractInvocationInfo(string snippet)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(snippet);
            var root = tree.GetRoot();
            var statement = root.DescendantNodes().OfType<StatementSyntax>().FirstOrDefault();
            return ExtractInvocationInfoFromStatement(statement);
        }
        catch
        {
            return null;
        }
    }

    private static InvocationInfo? ExtractInvocationInfoFromStatement(StatementSyntax? statement)
    {
        if (statement is not ExpressionStatementSyntax expr) return null;

        // Handle chained invocations like CreateMap<A, B>().ReverseMap()
        var invocation = expr.Expression as InvocationExpressionSyntax;
        if (invocation == null && expr.Expression is InvocationExpressionSyntax chainedInv)
        {
            invocation = chainedInv;
        }

        if (invocation == null) return null;

        // Get the root invocation for chained calls
        var rootInv = GetRootInvocationForMatching(invocation);
        if (rootInv == null) rootInv = invocation;

        var methodName = GetMethodName(rootInv);
        if (methodName == null) return null;

        var typeArgs = GetTypeArguments(rootInv);

        return new InvocationInfo(methodName, typeArgs);
    }

    private static InvocationExpressionSyntax? GetRootInvocationForMatching(InvocationExpressionSyntax inv)
    {
        // For chained methods like CreateMap<A,B>().ReverseMap()
        // We want the CreateMap part
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

    private static List<string> GetTypeArguments(InvocationExpressionSyntax inv)
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

    private static bool InvocationsMatch(InvocationInfo a, InvocationInfo b, CodeInjectionPattern pattern)
    {
        // For service registration, match by method name + type arguments
        // For CreateMap, match by type arguments only
        if (pattern == CodeInjectionPattern.CreateMapBlock)
        {
            return a.TypeArguments.Count == b.TypeArguments.Count &&
                   a.TypeArguments.SequenceEqual(b.TypeArguments);
        }

        // Default: match by method name + type arguments
        return a.MethodName == b.MethodName &&
               a.TypeArguments.Count == b.TypeArguments.Count &&
               a.TypeArguments.SequenceEqual(b.TypeArguments);
    }

    private sealed record InvocationInfo(string MethodName, List<string> TypeArguments);

    #endregion

    #region Position Helpers

    private static int GetInsertIndexAfterDeclarations(SyntaxList<StatementSyntax> statements)
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

    private static int GetInsertIndexBeforeReturn(SyntaxList<StatementSyntax> statements)
    {
        if (statements.Count > 0 && statements.Last() is ReturnStatementSyntax)
        {
            return statements.Count - 1;
        }
        return statements.Count;
    }

    private static string GetBodyIndentation(BlockSyntax body)
    {
        var firstStatement = body.Statements.FirstOrDefault();
        if (firstStatement != null)
        {
            var leadingTrivia = firstStatement.GetLeadingTrivia();
            var whitespace = leadingTrivia
                .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                .LastOrDefault();

            if (whitespace != default)
            {
                return whitespace.ToString();
            }
        }

        // Default to 8 spaces (2 levels of 4-space indent)
        return "        ";
    }

    #endregion
}
