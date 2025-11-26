namespace Lft.SqlSchema;

public sealed record SqlObjectSchema(
    string SchemaName,
    string Name,
    SqlObjectKind Kind,
    IReadOnlyList<SqlColumnSchema> Columns);
