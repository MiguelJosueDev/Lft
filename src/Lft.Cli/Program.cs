using Lft.App.Pipelines;
using Lft.App.Pipelines.Steps;
using Lft.App.Pipelines.Steps.Strategies;
using Lft.App.Services;
using Lft.Ast.CSharp;
using Lft.Discovery;
using Lft.Domain.Models;
using Lft.Domain.Services;
using Lft.Engine;
using Lft.Engine.Discovery;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    var language = "csharp";
    dryRun = false;
    profile = null;

    for (var i = 3; i < args.Count; i++)
    {
        if (string.Equals(args[i], "--lang", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
        {
            language = args[i + 1];
            i++;
        }
        else if (string.Equals(args[i], "--dry-run", StringComparison.OrdinalIgnoreCase))
        {
            dryRun = true;
        }
        else if (string.Equals(args[i], "--profile", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
        {
            profile = args[i + 1];
            i++;
        }
    }

    request = new GenerationRequest(
        entityName: entityName,
        language: language,
        outputDirectory: Directory.GetCurrentDirectory(),
        commandName: "crud",
        templatePack: "main",
        profile: profile);

    return true;
}

static string ResolveTemplatesRoot()
{
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

    return templatesRoot;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  lft gen crud <EntityName> [--lang <language>] [--profile <profile>] [--dry-run]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --lang <language>    Target language (default: csharp)");
    Console.WriteLine("  --profile <profile>  Config profile from lft.config.json (e.g., accounts, transactions-app)");
    Console.WriteLine("  --dry-run            Preview changes without writing files");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  lft gen crud User --lang csharp");
    Console.WriteLine("  lft gen crud PhoneType --profile transactions-app");
}
