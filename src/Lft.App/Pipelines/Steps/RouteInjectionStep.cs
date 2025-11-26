using Lft.Ast.CSharp;
using Lft.Domain.Models;

namespace Lft.App.Pipelines.Steps;

public sealed class RouteInjectionStep : IGenerationStep
{
    private readonly ICSharpInjectionService _injectionService;

    public RouteInjectionStep(ICSharpInjectionService injectionService)
    {
        _injectionService = injectionService;
    }

    public async Task ExecuteAsync(GenerationRequest request, GenerationResult result, CancellationToken ct = default)
    {
        var outputDir = request.OutputDirectory ?? Directory.GetCurrentDirectory();

        // Get suffix from profile config (e.g., "Extensions" or "Extension")
        var routesSuffix = result.GetVariable("RoutesExtensionSuffix", "Extensions");
        var searchPattern = $"*Routes{routesSuffix}.cs";

        var files = Directory.GetFiles(outputDir, searchPattern, SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            Console.WriteLine($"[WARN] No {searchPattern} found. Skipping route injection.");
            return;
        }

        var targetFile = files[0];
        Console.WriteLine($"[INFO] Found routes extension file: {targetFile}");

        var entityName = request.EntityName;
        var snippet = $"endpoints.Map{entityName}sEndpoints();";

        // Get method name from profile config (e.g., "MapModelRoutes" or "MapGroup")
        var routePattern = result.GetVariable("RoutePattern", "MapModelRoutes");

        var injectionRequest = new CodeInjectionRequest(
            FilePath: targetFile,
            ClassNameSuffix: $"Routes{routesSuffix}",
            MethodName: routePattern,
            Snippet: snippet,
            Position: CodeInjectionPosition.End
        );

        try
        {
            await _injectionService.InjectIntoMethodAsync(injectionRequest, ct);
            Console.WriteLine($"[INFO] Injected route mapping for '{entityName}' into '{targetFile}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Failed to inject route mapping: {ex.Message}");
        }
    }
}
