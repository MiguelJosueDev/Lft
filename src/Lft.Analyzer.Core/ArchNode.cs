namespace Lft.Analyzer.Core;

public sealed record ArchNode(
    string Id,                      // e.g. "MyApp.Infrastructure.Repositories.UserRepository"
    string Name,                    // e.g. "UserRepository"
    string Namespace,               // e.g. "MyApp.Infrastructure.Repositories"
    string Language,                // "csharp"
    Layer Layer,                    // initially Unknown
    IReadOnlyList<string> DependsOnIds,
    IReadOnlyDictionary<string, string> Metadata
);
