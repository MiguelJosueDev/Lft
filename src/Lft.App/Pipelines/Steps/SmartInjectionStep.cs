using Lft.App.Pipelines.Steps.Strategies;
using Lft.Ast.CSharp;
using Lft.Domain.Models;

namespace Lft.App.Pipelines.Steps;

public sealed class SmartInjectionStep : IGenerationStep
{
    private readonly ICSharpInjectionService _injector;
    private readonly IEnumerable<IInjectionStrategy> _strategies;

    public SmartInjectionStep(ICSharpInjectionService injector, IEnumerable<IInjectionStrategy> strategies)
    {
        _injector = injector;
        _strategies = strategies;
    }

    public async Task ExecuteAsync(GenerationRequest request, GenerationResult result, CancellationToken ct = default)
    {
        Console.WriteLine("[LFT] Running Smart Dependency Injection...");

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
                        Console.WriteLine($"[WARN] Injection failed for {file.Path}: {ex.Message}");
                    }
                }
            }
        }
    }
}
