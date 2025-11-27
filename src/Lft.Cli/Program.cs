using Lft.App.Pipelines;
using Lft.App.Pipelines.Steps;
using Lft.App.Pipelines.Steps.Strategies;
using Lft.App.Services;
using Lft.Ast.CSharp.Features.Injection.Adapters;
using Lft.Ast.CSharp.Features.Injection.Services;
using Lft.Ast.CSharp.Features.Validation.Services;
using Lft.Ast.JavaScript;
using Lft.Discovery;
using Lft.Engine.Discovery;
using Lft.Domain.Models;
using Lft.Domain.Services;
using Lft.Engine;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.Engine.Steps;
using Lft.Integration;
using Lft.SqlSchema;
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

static string ResolveTemplatesRoot()
{
    // Look for templates in multiple locations:
    // 1. Next to the executable (for installed/published tool)
    var templatesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");

    if (!Directory.Exists(templatesRoot))
    {
        // 2. In the LFT project root (for development when running from other directories)
        var assemblyLocation = typeof(Program).Assembly.Location;
        var projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", "..", "..", ".."));
        templatesRoot = Path.Combine(projectRoot, "templates");
    }

    if (!Directory.Exists(templatesRoot))
    {
        // 3. Current working directory
        templatesRoot = Path.Combine(Directory.GetCurrentDirectory(), "templates");
    }

    return templatesRoot;
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
    services.AddSingleton<SuffixBasedPathResolver>();
    services.AddSingleton<ISmartPathResolver>(sp => sp.GetRequiredService<SuffixBasedPathResolver>());
    services.AddSingleton<IPathResolver>(sp => sp.GetRequiredService<SuffixBasedPathResolver>());
    services.AddSingleton<ICodeInjector, CSharpCodeInjector>();
    services.AddSingleton<IJavaScriptInjectionService, JavaScriptInjectionService>();
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
        sp.GetRequiredService<IJavaScriptInjectionService>(),
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

    if (args.Count < 2)
    {
        return false;
    }

    var subcommand = args[1];
    if (!string.Equals(subcommand, "crud", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    // Parse flags and --set variables
    var language = "csharp";
    string? entityName = null;
    var variables = new Dictionary<string, string>();

    for (var i = 2; i < args.Count; i++)
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
        else if (string.Equals(args[i], "--set", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
        {
            // Parse key=value
            var keyValue = args[i + 1];
            var eqIndex = keyValue.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = keyValue[..eqIndex];
                var value = keyValue[(eqIndex + 1)..];
                variables[key] = value;

                // Extract entity name from modelName variable
                if (string.Equals(key, "modelName", StringComparison.OrdinalIgnoreCase))
                {
                    entityName = value;
                }
            }
            i++;
        }
        else if (!args[i].StartsWith("--"))
        {
            // Positional argument - entity name
            entityName ??= args[i];
        }
    }

    if (string.IsNullOrEmpty(entityName))
    {
        Console.WriteLine("[ERROR] Entity name is required. Use: lft gen crud <EntityName> or --set modelName=<EntityName>");
        return false;
    }

    request = new GenerationRequest(
        entityName: entityName,
        language: language,
        outputDirectory: Directory.GetCurrentDirectory(),
        commandName: "crud",
        templatePack: "main",
        profile: profile,
        variables: variables
    );

    return true;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  lft gen crud <EntityName> [options]");
    Console.WriteLine("  lft gen crud --set modelName=<EntityName> [--set key=value ...] [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --set key=value      Set a variable (can be repeated)");
    Console.WriteLine("  --lang <language>    Target language (default: csharp)");
    Console.WriteLine("  --profile <profile>  Config profile from lft.config.json");
    Console.WriteLine("  --dry-run            Preview changes without writing files");
    Console.WriteLine();
    Console.WriteLine("Common variables:");
    Console.WriteLine("  modelName            Entity name (e.g., PhoneType)");
    Console.WriteLine("  isMql                Enable MQL support (true/false)");
    Console.WriteLine("  keyType              Primary key type (byte, int, long, Guid)");
    Console.WriteLine("  isRepositoryView     Is a read-only view (true/false)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  lft gen crud User");
    Console.WriteLine("  lft gen crud PhoneType --profile accounts");
    Console.WriteLine("  lft gen crud --set modelName=PhoneType --set isMql=true --set keyType=byte");
}
