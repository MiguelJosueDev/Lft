using Lft.SqlSchema;

namespace Lft.Cli;

public sealed record CrudGenerationOptions(
    string EntityName,
    string Language,
    bool DryRun = false,
    string? Ddl = null,
    string? DdlFile = null,
    SqlObjectKind? SqlObjectKindHint = null,
    string? SqlObjectNameHint = null,
    string? Profile = null);
