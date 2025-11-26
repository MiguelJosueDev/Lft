using Lft.Discovery;
using Lft.Cli;
using Lft.Domain.Models;
using Lft.Domain.Services;
using Lft.Engine;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.App.Pipelines;
using Lft.Integration;
using Lft.Ast.CSharp;
using Lft.App.Pipelines.Steps;
using Lft.App.Pipelines.Steps.Strategies;
using Lft.App.Services;
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

    // Parse flags
    var language = "csharp";
    var dryRun = false;
    string? profile = null;

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
        else if (string.Equals(args[i], "--profile", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            profile = args[i + 1];
            i++;
        }
    }

    var request = new GenerationRequest(
        entityName: entityName,
        language: language,
        outputDirectory: Directory.GetCurrentDirectory(),
        commandName: "crud",
        templatePack: "main",
        profile: profile
    );

    // Setup dependencies (Manual DI for now)
    // Look for templates in multiple locations:
    // 1. Next to the executable (for installed/published tool)
    // 2. In the LFT project root (for development when running from other directories)
    var templatesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");

    if (!Directory.Exists(templatesRoot))
    {
        // Development fallback: find templates relative to the Lft.Cli project
        // This allows running `dotnet run --project /path/to/Lft.Cli` from any directory
        var assemblyLocation = typeof(Program).Assembly.Location;
        var projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", "..", "..", ".."));
        templatesRoot = Path.Combine(projectRoot, "templates");
    }

    if (!Directory.Exists(templatesRoot))
    {
        // Last fallback: current directory (original behavior)
        templatesRoot = Path.Combine(Directory.GetCurrentDirectory(), "templates");
    }

    var packLoader = new TemplatePackLoader(templatesRoot);

    // Path Resolver (implements both ISmartPathResolver and IPathResolver)
    var pathResolver = new SuffixBasedPathResolver();

    var variableResolver = new VariableResolver(new IVariableProvider[]
    {
        new CliVariableProvider(),
        new ProjectConfigVariableProvider(profile),  // Load from lft.config.json first
        new ConventionsVariableProvider(),           // Then apply conventions (can use config values)
        new SmartContextVariableProvider(pathResolver, Directory.GetCurrentDirectory(), profile)
    });
    var renderer = new LiquidTemplateRenderer();

    // StepExecutor now receives the path resolver for discovery mode and code injector
    var codeInjector = new CSharpCodeInjector();
    var stepExecutor = new StepExecutor(templatesRoot, renderer, pathResolver, codeInjector);

    // Project analyzer for intelligent discovery
    var projectAnalyzer = new ProjectAnalyzer();

    ICodeGenerationEngine engine = new TemplateCodeGenerationEngine(
        packLoader,
        variableResolver,
        stepExecutor,
        projectAnalyzer
    );

    IFileWriter fileWriter = new DiskFileWriter();
    IFileIntegrationService integrationService = new AnchorIntegrationService();

    // Syntax validation step (injections now happen via template 'inject' action)
    ICSharpSyntaxValidator syntaxValidator = new CSharpSyntaxValidator();
    var validationStep = new SyntaxValidationStep(syntaxValidator);
    ISqlSchemaParser sqlSchemaParser = new SqlServerSchemaParser();
    var crudMapper = new SqlObjectToCrudMapper();
    var requestFactory = new CrudGenerationRequestFactory(sqlSchemaParser, crudMapper);

    var request = await requestFactory.CreateAsync(options);

    // Pipeline
    var pipeline = new GenPipeline(engine, integrationService, fileWriter);

    var steps = new IGenerationStep[] { validationStep };

    // Pipeline (no longer needs pathResolver - StepExecutor handles path resolution)
    var pipeline = new GenPipeline(engine, integrationService, fileWriter, steps);

    Console.WriteLine($"[LFT] Generating CRUD for entity '{entityName}' (lang: {language})...");
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
    Console.WriteLine("  lft gen crud <EntityName> [--lang <language>] [--profile <profile>] [--dry-run]");
    Console.WriteLine("  lft gen crud <EntityName> [--lang <language>]");
    Console.WriteLine("  lft gen crud <EntityName> [--ddl \"<sql-script>\"]");
    Console.WriteLine("  lft gen crud <EntityName> [--ddl-file <path-to-sql>]");
    Console.WriteLine("  lft gen crud <EntityName> [--sql-object-kind table|view] [--sql-object-name <name>]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --lang <language>    Target language (default: csharp)");
    Console.WriteLine("  --profile <profile>  Config profile from lft.config.json (e.g., accounts, transactions-app)");
    Console.WriteLine("  --dry-run            Preview changes without writing files");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  lft gen crud User --lang csharp");
    Console.WriteLine("  lft gen crud PhoneType --profile transactions-app");
    Console.WriteLine("  lft gen crud User --ddl \"CREATE TABLE dbo.Users (...)\"");
    Console.WriteLine("  lft gen crud User --ddl-file ./sql/Users.sql");
}
