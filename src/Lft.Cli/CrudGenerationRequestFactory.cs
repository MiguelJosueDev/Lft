using Lft.Domain.Models;
using Lft.SqlSchema;

namespace Lft.Cli;

public sealed class CrudGenerationRequestFactory
{
    private readonly ISqlSchemaParser _sqlSchemaParser;
    private readonly SqlObjectToCrudMapper _crudMapper;

    public CrudGenerationRequestFactory(ISqlSchemaParser sqlSchemaParser, SqlObjectToCrudMapper crudMapper)
    {
        _sqlSchemaParser = sqlSchemaParser;
        _crudMapper = crudMapper;
    }

    public async Task<GenerationRequest> CreateAsync(CrudGenerationOptions options)
    {
        CrudSchemaDefinition? crudSchema = null;

        var sqlContent = await ReadSqlAsync(options);
        if (!string.IsNullOrWhiteSpace(sqlContent))
        {
            var schema = _sqlSchemaParser.Parse(sqlContent!, options.SqlObjectKindHint, options.SqlObjectNameHint);
            crudSchema = _crudMapper.Map(schema);
        }

        return new GenerationRequest(
            options.EntityName,
            options.Language,
            outputDirectory: Directory.GetCurrentDirectory(),
            crudSchemaDefinition: crudSchema);
    }

    private static async Task<string?> ReadSqlAsync(CrudGenerationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Ddl))
        {
            return options.Ddl;
        }

        if (!string.IsNullOrWhiteSpace(options.DdlFile))
        {
            var path = options.DdlFile!;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"SQL file not found: {path}", path);
            }

            return await File.ReadAllTextAsync(path);
        }

        return null;
    }
}
