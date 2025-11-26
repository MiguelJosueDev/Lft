using Lft.Ast.CSharp;
using Lft.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.App.Pipelines.Steps;

public sealed class RouteInjectionStep : IGenerationStep
{
    private readonly ICSharpInjectionService _injectionService;
    private readonly ILogger<RouteInjectionStep> _logger;

    public RouteInjectionStep(ICSharpInjectionService injectionService, ILogger<RouteInjectionStep>? logger = null)
    {
        _injectionService = injectionService;
        _logger = logger ?? NullLogger<RouteInjectionStep>.Instance;
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
            _logger.LogWarning("No {Pattern} found. Skipping route injection.", searchPattern);
            return;
        }

        var targetFile = files[0];
        _logger.LogInformation("Found routes extension file: {File}", targetFile);

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
            _logger.LogInformation("Injected route mapping for '{Entity}' into '{File}'.", entityName, targetFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inject route mapping");
        }
    }
}
