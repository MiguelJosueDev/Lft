using Lft.Analyzer.Core;

namespace Lft.Ast.CSharp.Features.Analysis.Models;

public sealed record CSharpCodebase(
    IReadOnlyList<CSharpProjectInfo> Projects,
    IReadOnlyList<ArchNode> ArchNodes
);

public sealed record CSharpProjectInfo(
    string Name,
    string FilePath,
    IReadOnlyList<CSharpDocumentInfo> Documents
);

public sealed record CSharpDocumentInfo(
    string FilePath,
    string? ProjectName,
    IReadOnlyList<string> DeclaredTypes   // e.g. fully-qualified type names
);
