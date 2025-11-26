using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Lft.SqlSchema;

/// <summary>
/// Factory abstraction to create the ScriptDom parser and script generator so that
/// <see cref="SqlServerSchemaParser"/> stays decoupled from concrete versions or
/// alternative implementations.
/// </summary>
public interface ISqlScriptDomFactory
{
    TSqlParserBase CreateParser();

    SqlScriptGenerator CreateScriptGenerator();
}
