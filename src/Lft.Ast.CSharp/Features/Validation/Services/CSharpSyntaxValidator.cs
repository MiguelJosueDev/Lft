using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lft.Ast.CSharp.Features.Validation.Services;

public class CSharpSyntaxValidator : ICSharpSyntaxValidator
{
    public IEnumerable<string> Validate(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return Array.Empty<string>();
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var diagnostics = syntaxTree.GetDiagnostics();

        // Filter only errors (Severity == Error)
        // We ignore warnings for now as generated code might have unused usings etc.
        return diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString());
    }
}
