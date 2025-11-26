using Lft.App.Pipelines;
using Lft.App.Pipelines.Steps;
using Lft.App.Pipelines.Steps.Strategies;
using Lft.App.Services;
using Lft.Ast.CSharp;
using Lft.Discovery;
using Lft.Cli;
using Lft.Domain.Models;
using Lft.Domain.Services;
using Lft.Engine;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.Engine.Steps;
using Lft.App.Pipelines;
using Lft.Integration;
using Lft.Ast.CSharp;
using Lft.App.Pipelines.Steps;
using Lft.App.Pipelines.Steps.Strategies;
using Lft.App.Services;

if (!TryParseGenerationCommand(args, out var request, out var dryRun, out var profile))
{
    PrintUsage();
    return;
}

var templatesRoot = ResolveTemplatesRoot();

using var provider = ConfigureServices(templatesRoot, profile);
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Lft.Cli");

logger.LogInformation(
    "Generating CRUD for entity '{Entity}' (lang: {Language})...",
    request.EntityName,
    request.Language);

var pipeline = provider.GetRequiredService<GenPipeline>();

try
{
    await pipeline.ExecuteAsync(request, dryRun);
}
catch (Exception ex)
{
    logger.LogError(ex, "Pipeline failed");
}

static ServiceProvider ConfigureServices(string templatesRoot, string? profile)
{
    var services = new ServiceCollection();

    services.AddLogging(builder =>
    {
        builder.AddSimpleConsole(options => options.SingleLine = true);
        builder.SetMinimumLevel(LogLevel.Information);
    });

    services.AddSingleton(new TemplatePackLoader(templatesRoot));
    services.AddSingleton<ITemplateRenderer, LiquidTemplateRenderer>();
    services.AddSingleton<ISmartPathResolver, SuffixBasedPathResolver>();
    services.AddSingleton<IPathResolver>(sp => sp.GetRequiredService<ISmartPathResolver>());
    services.AddSingleton<ICodeInjector, CSharpCodeInjector>();
    services.AddSingleton<VariableResolver>();
    services.AddSingleton<IVariableProvider, CliVariableProvider>();
    services.AddSingleton<IVariableProvider>(sp =>
        new ProjectConfigVariableProvider(profile, sp.GetRequiredService<ILogger<ProjectConfigVariableProvider>>()));
    services.AddSingleton<IVariableProvider, ConventionsVariableProvider>();
    services.AddSingleton<IVariableProvider>(sp =>
        new SmartContextVariableProvider(
            sp.GetRequiredService<ISmartPathResolver>(),
            Directory.GetCurrentDirectory(),
            profile));

    services.AddSingleton<StepExecutor>(sp => new StepExecutor(
        templatesRoot,
        sp.GetRequiredService<ITemplateRenderer>(),
        sp.GetRequiredService<IPathResolver>(),
        sp.GetRequiredService<ICodeInjector>(),
        sp.GetRequiredService<ILogger<StepExecutor>>()));

    services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
    services.AddSingleton<IDiscoveryService>(sp => new DiscoveryService(
        sp.GetRequiredService<IProjectAnalyzer>(),
        sp.GetRequiredService<ILogger<DiscoveryService>>()));
    services.AddSingleton<ICodeGenerationEngine, TemplateCodeGenerationEngine>();

    services.AddSingleton<IFileWriter, DiskFileWriter>();
    services.AddSingleton<IFileIntegrationService, AnchorIntegrationService>();

    services.AddSingleton<ICSharpSyntaxValidator, CSharpSyntaxValidator>();
    services.AddSingleton<IGenerationStep, SyntaxValidationStep>();
    services.AddSingleton<IInjectionStrategy, RepositoryInjectionStrategy>();
    services.AddSingleton<IInjectionStrategy, ServiceInjectionStrategy>();
    services.AddSingleton<IInjectionStrategy, MapperInjectionStrategy>();
    services.AddSingleton<ICSharpInjectionService, CSharpInjectionService>();
    services.AddSingleton<IGenerationStep, SmartInjectionStep>();
    services.AddSingleton<IGenerationStep, RouteInjectionStep>();

    services.AddSingleton<GenPipeline>();

    return services.BuildServiceProvider();
}

static bool TryParseGenerationCommand(
    IReadOnlyList<string> args,
    out GenerationRequest request,
    out bool dryRun,
    out string? profile)
{
    request = default!;
    dryRun = false;
    profile = null;

    if (args.Count == 0 || !string.Equals(args[0], "gen", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (args.Count < 3)
    {
        return false;
    }

    var subcommand = args[1];
    if (!string.Equals(subcommand, "crud", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var entityName = args[2];

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
        var assemblyLocation = typeof(Program).Assembly.Location;
        var projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", "..", "..", ".."));
        templatesRoot = Path.Combine(projectRoot, "templates");
    }

    if (!Directory.Exists(templatesRoot))
    {
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

    var steps = new IGenerationStep[] { validationStep };

    // Pipeline (no longer needs pathResolver - StepExecutor handles path resolution)
    var pipeline = new GenPipeline(engine, integrationService, fileWriter, steps);

    Console.WriteLine($"[LFT] Generating CRUD for entity '{entityName}' (lang: {language})...");

    try
    {
        await pipeline.ExecuteAsync(request, dryRun);
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
        SqlObjectNameHint: sqlObjectNameHint,
        Profile: profile);
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
