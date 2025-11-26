using Lft.Cli;
using Lft.Domain.Models;
using Lft.Engine;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.App.Pipelines;
using Lft.Integration;
using Lft.SqlSchema;

if (args.Length == 0)
{
    PrintUsage();
    return;
}

// Por ahora solo soportamos: lft gen crud <EntityName> [--lang csharp]
var command = args[0];

if (string.Equals(command, "gen", StringComparison.OrdinalIgnoreCase))
{
    await HandleGenCommand(args);
}
else
{
    Console.WriteLine($"Unknown command: {command}");
    PrintUsage();
}

return;

static async Task HandleGenCommand(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Missing parameters for 'gen crud'.");
        PrintUsage();
        return;
    }

    var subcommand = args[1];
    if (!string.Equals(subcommand, "crud", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Unknown gen subcommand: {subcommand}");
        PrintUsage();
        return;
    }

    var entityName = args[2];
    var options = ParseOptions(args, entityName);

    // Setup dependencies (Manual DI for now)
    var templatesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
    // Fallback to local templates folder if not found in bin (for dev)
    if (!Directory.Exists(templatesRoot))
    {
        templatesRoot = Path.Combine(Directory.GetCurrentDirectory(), "templates");
    }

    var packLoader = new TemplatePackLoader(templatesRoot);
    var variableResolver = new VariableResolver(new IVariableProvider[]
    {
        new CliVariableProvider(),
        new ConventionsVariableProvider(),
    });
    var renderer = new LiquidTemplateRenderer();
    var stepExecutor = new StepExecutor(templatesRoot, renderer);

    ICodeGenerationEngine engine = new TemplateCodeGenerationEngine(
        packLoader,
        variableResolver,
        stepExecutor
    );

    IFileWriter fileWriter = new DiskFileWriter();
    IFileIntegrationService integrationService = new AnchorIntegrationService();

    ISqlSchemaParser sqlSchemaParser = new SqlServerSchemaParser();
    var crudMapper = new SqlObjectToCrudMapper();
    var requestFactory = new CrudGenerationRequestFactory(sqlSchemaParser, crudMapper);

    var request = await requestFactory.CreateAsync(options);

    // Pipeline
    var pipeline = new GenPipeline(engine, integrationService, fileWriter);

    Console.WriteLine($"[LFT] Generating CRUD for entity '{entityName}' (lang: {options.Language})...");

    try
    {
        await pipeline.ExecuteAsync(request, options.DryRun);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Pipeline failed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

static CrudGenerationOptions ParseOptions(string[] args, string entityName)
{
    var language = "csharp";
    var dryRun = false;
    string? ddl = null;
    string? ddlFile = null;
    SqlObjectKind? sqlObjectKindHint = null;
    string? sqlObjectNameHint = null;

    for (var i = 3; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--lang", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            language = args[i + 1];
            i++;
        }
        else if (string.Equals(args[i], "--dry-run", StringComparison.OrdinalIgnoreCase))
        {
            dryRun = true;
        }
        else if (string.Equals(args[i], "--ddl", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            ddl = args[i + 1];
            i++;
        }
        else if (string.Equals(args[i], "--ddl-file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            ddlFile = args[i + 1];
            i++;
        }
        else if (string.Equals(args[i], "--sql-object-kind", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            var kindValue = args[i + 1];
            if (string.Equals(kindValue, "table", StringComparison.OrdinalIgnoreCase))
            {
                sqlObjectKindHint = SqlObjectKind.Table;
            }
            else if (string.Equals(kindValue, "view", StringComparison.OrdinalIgnoreCase))
            {
                sqlObjectKindHint = SqlObjectKind.View;
            }
            i++;
        }
        else if (string.Equals(args[i], "--sql-object-name", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            sqlObjectNameHint = args[i + 1];
            i++;
        }
    }

    return new CrudGenerationOptions(
        EntityName: entityName,
        Language: language,
        DryRun: dryRun,
        Ddl: ddl,
        DdlFile: ddlFile,
        SqlObjectKindHint: sqlObjectKindHint,
        SqlObjectNameHint: sqlObjectNameHint);
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  lft gen crud <EntityName> [--lang <language>]");
    Console.WriteLine("  lft gen crud <EntityName> [--ddl \"<sql-script>\"]");
    Console.WriteLine("  lft gen crud <EntityName> [--ddl-file <path-to-sql>]");
    Console.WriteLine("  lft gen crud <EntityName> [--sql-object-kind table|view] [--sql-object-name <name>]");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  lft gen crud User --lang csharp");
    Console.WriteLine("  lft gen crud User --ddl \"CREATE TABLE dbo.Users (...)\"");
    Console.WriteLine("  lft gen crud User --ddl-file ./sql/Users.sql");
}
