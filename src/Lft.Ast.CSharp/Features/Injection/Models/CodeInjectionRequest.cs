namespace Lft.Ast.CSharp.Features.Injection.Models;

public sealed record CodeInjectionRequest(
    string FilePath,
    string ClassNameSuffix,   // ej. "Extensions"
    string MethodName,        // ej. "MapModelRoutes" o "MapGroup"
    string Snippet,           // ej. "endpoints.MapFundingTypeEndpoints();"
    CodeInjectionPosition Position = CodeInjectionPosition.End,
    CodeInjectionPattern Pattern = CodeInjectionPattern.Default
);
