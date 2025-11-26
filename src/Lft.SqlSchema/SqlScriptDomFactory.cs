using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Lft.SqlSchema;

/// <summary>
/// Default ScriptDom factory that keeps the parser configuration in one place and
/// can be swapped out for a different T-SQL version or alternative parser when needed.
/// </summary>
public sealed class SqlScriptDomFactory : ISqlScriptDomFactory
{
    public TSqlParserBase CreateParser()
    {
        return new TSql160Parser(initialQuotedIdentifiers: true);
    }

    public SqlScriptGenerator CreateScriptGenerator()
    {
        return new Sql160ScriptGenerator(new SqlScriptGeneratorOptions
        {
            IncludeSemicolons = false,
            SqlVersion = SqlVersion.Sql160
        });
    }
}
