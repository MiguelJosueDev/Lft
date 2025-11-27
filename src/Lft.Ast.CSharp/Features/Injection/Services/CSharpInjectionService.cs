using Lft.Ast.CSharp.Features.Injection.Models;
using Lft.Ast.CSharp.Features.Injection.Strategies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.Ast.CSharp.Features.Injection.Services;

public class CSharpInjectionService : ICSharpInjectionService
{
    private readonly IEnumerable<IInjectionStrategy> _strategies;

    public CSharpInjectionService()
    {
        // In a real DI scenario, these would be injected.
        // For now, we instantiate them manually or they could be passed in constructor.
        // Order matters: specific strategies first, default last (though default is handled explicitly usually)
        _strategies = new IInjectionStrategy[]
        {
            new AddScopedInjectionStrategy(),
            new CreateMapInjectionStrategy(),
            new MapRoutesInjectionStrategy()
        };
    }

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

        // 4. Resolve Strategy
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(pattern))
                       ?? new DefaultInjectionStrategy(position);

        // 5. Check Idempotency using AST-based comparison via Strategy
        if (StatementAlreadyExists(body.Statements, snippet, strategy))
        {
            return sourceCode;
        }

        // 6. Create New Statement with proper indentation
        var existingIndent = GetBodyIndentation(body);
        var newStatement = SyntaxFactory.ParseStatement(snippet)
            .WithLeadingTrivia(SyntaxFactory.Whitespace(existingIndent))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // 7. Determine insertion index
        int insertIndex = strategy.FindInsertionIndex(body);

        // 8. Create new body with the statement inserted
        var newBody = body.WithStatements(body.Statements.Insert(insertIndex, newStatement));

        // 9. Replace the old body with the new one
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

    #region AST-Based Idempotency

    private static bool StatementAlreadyExists(
        SyntaxList<StatementSyntax> statements,
        string snippet,
        IInjectionStrategy strategy)
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
            var existingInfo = InjectionStrategyHelpers.ExtractInvocationInfoFromStatement(stmt);
            if (existingInfo != null && strategy.Matches(snippetInfo, existingInfo))
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
            return InjectionStrategyHelpers.ExtractInvocationInfoFromStatement(statement);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
