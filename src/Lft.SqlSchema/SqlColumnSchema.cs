namespace Lft.SqlSchema;

public sealed record SqlColumnSchema(
    string Name,
    string SqlType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsIdentity,
    int? MaxLength,
    string? DefaultValue);
