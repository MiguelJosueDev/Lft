using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Lft.SqlSchema;

public sealed class SqlServerSchemaParser : ISqlSchemaParser
{
    private readonly ISqlScriptDomFactory _scriptDomFactory;

    public SqlServerSchemaParser(ISqlScriptDomFactory? scriptDomFactory = null)
    {
        _scriptDomFactory = scriptDomFactory ?? new SqlScriptDomFactory();
    }

    public SqlObjectSchema Parse(string sql, SqlObjectKind? kindHint = null, string? objectNameHint = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL content cannot be null or empty.", nameof(sql));
        }

        var parser = _scriptDomFactory.CreateParser();
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out var errors);

        if (errors is { Count: > 0 })
        {
            var message = string.Join(Environment.NewLine, errors.Select(e => $"Line {e.Line}, Col {e.Column}: {e.Message}"));
            throw new InvalidOperationException($"Failed to parse SQL script:{Environment.NewLine}{message}");
        }

        var collector = new SchemaCollector(_scriptDomFactory);
        fragment.Accept(collector);

        var schema = collector.Build(kindHint, objectNameHint);
        if (schema is null)
        {
            throw new InvalidOperationException("No recognizable SQL schema object found in the provided script.");
        }

        return schema;
    }

    private sealed class SchemaCollector : TSqlFragmentVisitor
    {
        private readonly ObjectSchemaRegistry _registry = new();
        private readonly ColumnSchemaFactory _columnFactory;
        private readonly ViewColumnExtractor _viewColumnExtractor;

        public SchemaCollector(ISqlScriptDomFactory scriptDomFactory)
        {
            _columnFactory = new ColumnSchemaFactory(scriptDomFactory);
            _viewColumnExtractor = new ViewColumnExtractor();
        }

        public override void Visit(CreateTableStatement node)
        {
            var identifier = NameParser.From(node.SchemaObjectName);
            var builder = _registry.GetOrAdd(identifier, SqlObjectKind.Table);

            var primaryKeyColumns = PrimaryKeyResolver.FromConstraints(node.Definition?.TableConstraints);
            foreach (var column in node.Definition?.ColumnDefinitions ?? Enumerable.Empty<ColumnDefinition>())
            {
                var columnSchema = _columnFactory.FromColumnDefinition(column, primaryKeyColumns.Contains(column.ColumnIdentifier.Value));
                builder.AddOrUpdateColumn(columnSchema);
            }

            builder.MarkPrimaryKey(primaryKeyColumns);
        }

        public override void Visit(AlterTableAddTableElementStatement node)
        {
            var identifier = NameParser.From(node.SchemaObjectName);
            var builder = _registry.GetOrAdd(identifier, SqlObjectKind.Table);

            foreach (var element in node.Definition?.TableElements ?? Enumerable.Empty<TableDefinitionElement>())
            {
                switch (element)
                {
                    case ColumnDefinition column:
                        builder.AddOrUpdateColumn(_columnFactory.FromColumnDefinition(column, false));
                        break;
                    case PrimaryKeyConstraintDefinition primaryKey:
                        builder.MarkPrimaryKey(PrimaryKeyResolver.FromConstraint(primaryKey));
                        break;
                }
            }
        }

        public override void Visit(CreateViewStatement node)
        {
            var identifier = NameParser.From(node.SchemaName);
            var builder = _registry.GetOrAdd(identifier, SqlObjectKind.View);

            foreach (var column in _viewColumnExtractor.Extract(node))
            {
                builder.AddOrUpdateColumn(column);
            }

            builder.SetKind(SqlObjectKind.View);
        }

        public SqlObjectSchema? Build(SqlObjectKind? kindHint, string? objectNameHint)
        {
            return _registry.Build(kindHint, objectNameHint);
        }
    }

    private sealed class ColumnSchemaFactory
    {
        private readonly ISqlScriptDomFactory _scriptDomFactory;

        public ColumnSchemaFactory(ISqlScriptDomFactory scriptDomFactory)
        {
            _scriptDomFactory = scriptDomFactory;
        }

        public SqlColumnSchema FromColumnDefinition(ColumnDefinition column, bool isPrimaryFromTableConstraint)
        {
            var sqlType = ScriptFragment(column.DataType);
            var isNullable = column.Nullable ?? true;
            var isPrimaryKey = isPrimaryFromTableConstraint || column.Constraints.OfType<PrimaryKeyConstraintDefinition>().Any();
            var isIdentity = column.IdentityOptions is not null;
            var defaultValue = column.DefaultConstraint?.Expression is { } expression ? ScriptFragment(expression) : null;
            var maxLength = GetMaxLength(column.DataType);

            return new SqlColumnSchema(
                column.ColumnIdentifier.Value,
                sqlType,
                isNullable,
                isPrimaryKey,
                isIdentity,
                maxLength,
                defaultValue);
        }

        private static int? GetMaxLength(DataTypeReference dataType)
        {
            if (dataType is SqlDataTypeReference { Parameters: { Count: > 0 } parameters })
            {
                if (parameters[0] is Literal literal)
                {
                    if (literal.Value.Equals("max", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    if (int.TryParse(literal.Value, out var length))
                    {
                        return length;
                    }
                }
            }

            return null;
        }

        private string ScriptFragment(TSqlFragment fragment)
        {
            var generator = _scriptDomFactory.CreateScriptGenerator();
            generator.GenerateScript(fragment, out var script);
            return script.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\t", string.Empty, StringComparison.Ordinal)
                .Trim();
        }
    }

    private sealed class ViewColumnExtractor
    {
        public IReadOnlyList<SqlColumnSchema> Extract(CreateViewStatement node)
        {
            var columns = new List<SqlColumnSchema>();

            if (node.Columns is { Count: > 0 })
            {
                columns.AddRange(node.Columns.Select(identifier => BuildColumn(identifier.Value)));
                return columns;
            }

            if (node.SelectStatement?.QueryExpression is QuerySpecification query)
            {
                foreach (var element in query.SelectElements.OfType<SelectScalarExpression>())
                {
                    var columnName = element.ColumnName?.Value
                                     ?? ExpressionNameExtractor.TryExtract(element.Expression)
                                     ?? $"Column{columns.Count}";

                    columns.Add(BuildColumn(columnName));
                }
            }

            return columns;
        }

        private static SqlColumnSchema BuildColumn(string columnName)
        {
            return new SqlColumnSchema(columnName, "sql_variant", true, false, false, null, null);
        }
    }

    private sealed class ObjectSchemaRegistry
    {
        private readonly Dictionary<SchemaObjectIdentifier, SqlObjectSchemaBuilder> _builders = new(new SchemaObjectIdentifierComparer());

        public SqlObjectSchemaBuilder GetOrAdd(SchemaObjectIdentifier identifier, SqlObjectKind defaultKind)
        {
            if (_builders.TryGetValue(identifier, out var builder))
            {
                return builder;
            }

            builder = new SqlObjectSchemaBuilder(identifier.Schema, identifier.Name, defaultKind);
            _builders[identifier] = builder;
            return builder;
        }

        public SqlObjectSchema? Build(SqlObjectKind? kindHint, string? objectNameHint)
        {
            IEnumerable<SqlObjectSchemaBuilder> candidates = _builders.Values;

            if (!string.IsNullOrWhiteSpace(objectNameHint))
            {
                candidates = candidates.Where(b => string.Equals(b.Name, objectNameHint, StringComparison.OrdinalIgnoreCase));
            }

            if (kindHint.HasValue)
            {
                foreach (var builder in candidates)
                {
                    builder.SetKind(kindHint.Value);
                }

                candidates = candidates.Where(b => b.Kind == kindHint.Value);
            }

            var selected = candidates.FirstOrDefault() ?? _builders.Values.FirstOrDefault();
            return selected?.Build();
        }
    }

    private sealed record SchemaObjectIdentifier(string Schema, string Name);

    private sealed class SchemaObjectIdentifierComparer : IEqualityComparer<SchemaObjectIdentifier>
    {
        public bool Equals(SchemaObjectIdentifier? x, SchemaObjectIdentifier? y)
        {
            return string.Equals(x?.Schema, y?.Schema, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(SchemaObjectIdentifier obj)
        {
            return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Schema), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
        }
    }

    private sealed class SqlObjectSchemaBuilder
    {
        private readonly List<SqlColumnSchema> _columns = new();

        public SqlObjectSchemaBuilder(string schemaName, string name, SqlObjectKind kind)
        {
            SchemaName = schemaName;
            Name = name;
            Kind = kind;
        }

        public string SchemaName { get; }
        public string Name { get; }
        public SqlObjectKind Kind { get; private set; }

        public void SetKind(SqlObjectKind kind) => Kind = kind;

        public void AddOrUpdateColumn(SqlColumnSchema column)
        {
            var existingIndex = _columns.FindIndex(c => string.Equals(c.Name, column.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                _columns[existingIndex] = Merge(_columns[existingIndex], column);
                return;
            }

            _columns.Add(column);
        }

        public void MarkPrimaryKey(IEnumerable<string> columnNames)
        {
            foreach (var columnName in columnNames)
            {
                var index = _columns.FindIndex(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    _columns[index] = _columns[index] with { IsPrimaryKey = true, IsNullable = false };
                }
            }
        }

        public SqlObjectSchema Build() => new(SchemaName, Name, Kind, _columns.ToList());

        private static SqlColumnSchema Merge(SqlColumnSchema existing, SqlColumnSchema incoming)
        {
            return incoming with
            {
                IsPrimaryKey = existing.IsPrimaryKey || incoming.IsPrimaryKey,
                IsIdentity = existing.IsIdentity || incoming.IsIdentity,
                IsNullable = incoming.IsNullable,
                SqlType = string.IsNullOrWhiteSpace(incoming.SqlType) ? existing.SqlType : incoming.SqlType,
                MaxLength = incoming.MaxLength ?? existing.MaxLength,
                DefaultValue = incoming.DefaultValue ?? existing.DefaultValue
            };
        }
    }

    private static class NameParser
    {
        public static SchemaObjectIdentifier From(SchemaObjectName? name)
        {
            return name?.BaseIdentifier is null
                ? new SchemaObjectIdentifier("dbo", string.Empty)
                : new SchemaObjectIdentifier(name.SchemaIdentifier?.Value ?? "dbo", name.BaseIdentifier.Value);
        }

        public static SchemaObjectIdentifier From(SchemaObjectNameOrIdentifier name)
        {
            if (name is null)
            {
                return new SchemaObjectIdentifier("dbo", string.Empty);
            }

            if (name.SchemaObjectName is not null)
            {
                return From(name.SchemaObjectName);
            }

            var identifiers = name.Identifier.Identifiers;
            var schema = identifiers.Count > 1 ? identifiers[^2].Value : "dbo";
            var objectName = identifiers[^1].Value;
            return new SchemaObjectIdentifier(schema, objectName);
        }
    }

    private static class PrimaryKeyResolver
    {
        public static HashSet<string> FromConstraints(IList<ConstraintDefinition>? constraints)
        {
            if (constraints is null)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var constraint in constraints.OfType<PrimaryKeyConstraintDefinition>())
            {
                AddColumns(set, constraint);
            }

            return set;
        }

        public static IEnumerable<string> FromConstraint(PrimaryKeyConstraintDefinition constraint)
        {
            var names = new List<string>();
            AddColumns(names, constraint);
            return names;
        }

        private static void AddColumns(ICollection<string> target, PrimaryKeyConstraintDefinition constraint)
        {
            foreach (var column in constraint.Columns)
            {
                var name = column.Column.MultiPartIdentifier.Identifiers.Last().Value;
                target.Add(name);
            }
        }
    }

    private static class ExpressionNameExtractor
    {
        public static string? TryExtract(ScalarExpression expression)
        {
            return expression switch
            {
                ColumnReferenceExpression columnRef => columnRef.MultiPartIdentifier.Identifiers.Last().Value,
                FunctionCall { CallTarget: ColumnReferenceExpression columnTarget } => columnTarget.MultiPartIdentifier.Identifiers.Last().Value,
                _ => null
            };
        }
    }
}
