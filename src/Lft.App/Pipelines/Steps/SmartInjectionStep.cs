using Lft.App.Pipelines.Steps.Strategies;
using Lft.Ast.CSharp;
using Lft.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.App.Pipelines.Steps;

public sealed class SmartInjectionStep : IGenerationStep
{
    private readonly ICSharpInjectionService _injector;
    private readonly IEnumerable<IInjectionStrategy> _strategies;
    private readonly ILogger<SmartInjectionStep> _logger;

    public SmartInjectionStep(
        ICSharpInjectionService injector,
        IEnumerable<IInjectionStrategy> strategies,
        ILogger<SmartInjectionStep>? logger = null)
    {
        _injector = injector;
        _strategies = strategies;
        _logger = logger ?? NullLogger<SmartInjectionStep>.Instance;
    }

    public async Task ExecuteAsync(GenerationRequest request, GenerationResult result, CancellationToken ct = default)
    {
        _logger.LogInformation("Running Smart Dependency Injection...");

        foreach (var file in result.Files)
        {
            foreach (var strategy in _strategies)
            {
                if (strategy.CanHandle(file))
                {
                    try
                    {
                        await strategy.InjectAsync(request, result, file, _injector, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Injection failed for {File}", file.Path);
                    }
                }
            }
        }
    }
}
