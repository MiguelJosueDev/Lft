using System.Data;
using Humanizer;
using Lft.Domain.Models;

namespace Lft.SqlSchema;

public sealed class SqlObjectToCrudMapper
{
    public CrudSchemaDefinition Map(SqlObjectSchema schema)
    {
        var fields = schema.Columns.Select(MapField).ToList();
        var isReadOnly = schema.Kind == SqlObjectKind.View;
        return new CrudSchemaDefinition(schema.Name, fields, isReadOnly, schema.SchemaName);
    }

    private static CrudFieldDefinition MapField(SqlColumnSchema column)
    {
        var (clrType, dbType) = MapSqlType(column.SqlType, column.IsNullable);
        var name = column.Name.Pascalize();
        var dbName = column.Name;
        var isRequired = !column.IsNullable;

        return new CrudFieldDefinition(
            name,
            clrType,
            isRequired,
            column.IsPrimaryKey,
            column.IsIdentity,
            dbName,
            dbType,
            column.MaxLength,
            column.DefaultValue,
            column.SqlType);
    }

    private static (string ClrType, string DbType) MapSqlType(string sqlType, bool isNullable)
    {
        var normalized = sqlType.ToLowerInvariant();
        var baseType = normalized.Split(' ', '(')[0];

        return baseType switch
        {
            "bigint" => (ApplyNullability("long", isNullable), "DbType.Int64"),
            "int" => (ApplyNullability("int", isNullable), "DbType.Int32"),
            "smallint" => (ApplyNullability("short", isNullable), "DbType.Int16"),
            "tinyint" => (ApplyNullability("byte", isNullable), "DbType.Byte"),
            "bit" => (ApplyNullability("bool", isNullable), "DbType.Boolean"),
            "uniqueidentifier" => (ApplyNullability("Guid", isNullable), "DbType.Guid"),
            "nvarchar" or "varchar" or "nchar" or "char" or "text" or "ntext" => (ApplyReferenceNullability("string", isNullable), "DbType.String"),
            "datetime" or "datetime2" or "smalldatetime" or "date" => (ApplyNullability("DateTime", isNullable), "DbType.DateTime2"),
            "time" => (ApplyNullability("TimeSpan", isNullable), "DbType.Time"),
            "decimal" or "numeric" or "money" or "smallmoney" => (ApplyNullability("decimal", isNullable), "DbType.Decimal"),
            "float" => (ApplyNullability("double", isNullable), "DbType.Double"),
            "real" => (ApplyNullability("float", isNullable), "DbType.Single"),
            "binary" or "varbinary" or "image" => (ApplyReferenceNullability("byte[]", isNullable), "DbType.Binary"),
            _ => (ApplyReferenceNullability("string", isNullable), "DbType.String"),
        };
    }

    private static string ApplyNullability(string clrType, bool isNullable)
        => isNullable && !clrType.EndsWith("?") && clrType is not "string" and not "byte[]"
            ? clrType + "?"
            : clrType;

    private static string ApplyReferenceNullability(string clrType, bool isNullable)
        => isNullable && !clrType.EndsWith("?") ? clrType + "?" : clrType;
}
