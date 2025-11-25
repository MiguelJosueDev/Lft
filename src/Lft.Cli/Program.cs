using Lft.Domain.Models;
using Lft.Engine;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.Engine.Steps;
using Lft.App.Pipelines;
using Lft.Integration;

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

    // Parse flags
    var language = "csharp";
    var dryRun = false;

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
    }

    var request = new GenerationRequest(
        entityName: entityName,
        language: language,
        outputDirectory: Directory.GetCurrentDirectory(),
        commandName: "crud",
        templatePack: "main"
    );

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

    // Pipeline
    var pipeline = new GenPipeline(engine, integrationService, fileWriter);

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

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  lft gen crud <EntityName> [--lang <language>]");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  lft gen crud User --lang csharp");
}
