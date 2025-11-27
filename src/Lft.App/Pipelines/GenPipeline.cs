using Lft.Domain.Models;
using Lft.Engine;
using Lft.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.App.Pipelines;

public class GenPipeline
{
    private readonly ICodeGenerationEngine _engine;
    private readonly IFileIntegrationService _integrationService;
    private readonly IFileWriter _writer;
    private readonly IEnumerable<IGenerationStep> _steps;
    private readonly ILogger<GenPipeline> _logger;

    public GenPipeline(
        ICodeGenerationEngine engine,
        IFileIntegrationService integrationService,
        IFileWriter writer,
        IEnumerable<IGenerationStep> steps,
        ILogger<GenPipeline>? logger = null)
    {
        _engine = engine;
        _integrationService = integrationService;
        _writer = writer;
        _steps = steps;
        _logger = logger ?? NullLogger<GenPipeline>.Instance;
    }

    public async Task ExecuteAsync(GenerationRequest request, bool dryRun, CancellationToken ct = default)
    {
        // 1. Generate (In-Memory) - paths are resolved by StepExecutor
        var result = await _engine.GenerateAsync(request, ct);

        if (result.Files.Count == 0)
        {
            _logger.LogInformation("No files generated.");
            return;
        }

        // 2. Integrate & Plan
        var plans = new List<FileChangePlan>();
        foreach (var file in result.Files)
        {
            // Paths come correctly resolved from StepExecutor
            var fullPath = file.Path;

            // Calculate plan (Create or Modify)
            // Use Replace strategy for both new files and modifications
            // Anchor strategy is only for injection snippets (which have IsModification=true)
            var options = new IntegrationOptions
            {
                Strategy = IntegrationStrategy.Replace,
                CheckIdempotency = true
            };
            var plan = await _integrationService.IntegrateAsync(fullPath, file.Content, options, ct);
            plans.Add(plan);
        }

        // 3. Dry Run / Execute
        if (dryRun)
        {
            LogDryRunPlans(plans);
            return;
        }

        _logger.LogInformation("Applying {Count} change plan(s)...", plans.Count);
        foreach (var plan in plans)
        {
            if (plan.Type == ChangeType.Skip)
            {
                _logger.LogInformation("Skipping unchanged file {Path}", plan.Path);
                continue;
            }

            await _writer.WriteFileAsync(plan.Path, plan.NewContent, overwrite: true);
        }

        if (_steps.Any())
        {
            _logger.LogInformation("Running {Count} post-generation steps...", _steps.Count());
            foreach (var step in _steps)
            {
                await step.ExecuteAsync(request, result, ct);
            }
        }
    }

    private void LogDryRunPlans(IEnumerable<FileChangePlan> plans)
    {
        foreach (var plan in plans)
        {
            _logger.LogInformation("[DRY-RUN] {Type} {Path}", plan.Type, plan.Path);
        }
    }
}
