namespace Lft.Domain.Models;

public sealed record CrudFieldDefinition(
    string Name,
    string ClrType,
    bool IsRequired,
    bool IsPrimaryKey,
    bool IsIdentity,
    string? DbName = null,
    string? DbType = null,
    int? MaxLength = null,
    string? DefaultValue = null,
    string? SqlType = null);
