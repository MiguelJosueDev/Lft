namespace Lft.Domain.Models;

public sealed record CrudSchemaDefinition(
    string Name,
    IReadOnlyList<CrudFieldDefinition> Fields,
    bool IsReadOnly,
    string? SchemaName = null);
