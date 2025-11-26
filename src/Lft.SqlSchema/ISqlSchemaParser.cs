namespace Lft.SqlSchema;

public interface ISqlSchemaParser
{
    SqlObjectSchema Parse(string sql, SqlObjectKind? kindHint = null, string? objectNameHint = null);
}
