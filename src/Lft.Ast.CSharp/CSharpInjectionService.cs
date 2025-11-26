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
            request.Position
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
        CodeInjectionPosition position)
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
            // For a constructor, the 'methodName' should match the class name
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

        // 4. Check Idempotency (Simple string check for now, can be improved with AST matching)
        // Normalize whitespace for comparison could be better, but let's start with Contains
        // to avoid re-parsing the snippet multiple times if not needed.
        // A better approach is to check if any statement's string representation contains the snippet.
        var snippetTrimmed = snippet.Trim();
        if (body.Statements.Any(s => s.ToString().Contains(snippetTrimmed)))
        {
            return sourceCode;
        }

        // 4. Create New Statement with proper indentation
        // Get the indentation from existing statements in the body
        var existingIndent = GetBodyIndentation(body);
        var newStatement = SyntaxFactory.ParseStatement(snippet)
            .WithLeadingTrivia(SyntaxFactory.Whitespace(existingIndent))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // 5. Create new body with the statement inserted
        BlockSyntax newBody;

        if (position == CodeInjectionPosition.Beginning)
        {
            // Find the first non-declaration statement to insert after variable declarations
            var insertIndex = 0;
            foreach (var stmt in body.Statements)
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

            newBody = body.WithStatements(
                body.Statements.Insert(insertIndex, newStatement)
            );
        }
        else // End
        {
            // Insert before the last return statement if it exists
            var statements = body.Statements;
            var lastStatement = statements.LastOrDefault();

            if (lastStatement is ReturnStatementSyntax)
            {
                newBody = body.WithStatements(
                    statements.Insert(statements.Count - 1, newStatement)
                );
            }
            else
            {
                newBody = body.AddStatements(newStatement);
            }
        }

        // 6. Replace the old body with the new one
        // IMPORTANT: Do NOT call NormalizeWhitespace() as it reformats the entire method
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
        // Try to get indentation from the first statement
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
}
