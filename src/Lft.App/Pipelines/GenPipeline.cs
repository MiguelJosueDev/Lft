using Lft.Domain.Models;
using Lft.Engine;
using Lft.Integration;

namespace Lft.App.Pipelines;

public class GenPipeline
{
    private readonly ICodeGenerationEngine _engine;
    private readonly IFileIntegrationService _integrationService;
    private readonly IFileWriter _writer;
    private readonly IEnumerable<IGenerationStep> _steps;

    public GenPipeline(
        ICodeGenerationEngine engine,
        IFileIntegrationService integrationService,
        IFileWriter writer,
        IEnumerable<IGenerationStep> steps)
    {
        _engine = engine;
        _integrationService = integrationService;
        _writer = writer;
        _steps = steps;
    }

    public async Task ExecuteAsync(GenerationRequest request, bool dryRun, CancellationToken ct = default)
    {
        // 1. Generate (In-Memory) - paths are resolved by StepExecutor
        var result = await _engine.GenerateAsync(request, ct);

        if (result.Files.Count == 0)
        {
            Console.WriteLine("[LFT] No files generated.");
            return;
        }

        // 2. Integrate & Plan
        var plans = new List<FileChangePlan>();
        foreach (var file in result.Files)
        {
            // Paths come correctly resolved from StepExecutor
            var fullPath = file.Path;

            // Calculate plan (Create or Modify)
            // Use Replace strategy for injected files, Anchor for others
            var options = new IntegrationOptions
            {
                Strategy = file.IsModification ? IntegrationStrategy.Replace : IntegrationStrategy.Anchor,
                CheckIdempotency = true
            };
            var plan = await _integrationService.IntegrateAsync(fullPath, file.Content, options, ct);
            plans.Add(plan);
        }

        // 3. Dry Run / Execute
        if (dryRun)
        {
            Console.WriteLine("\n[DRY-RUN] Proposed Changes:");
            foreach (var plan in plans)
            {
                Console.WriteLine($"\nFile: {plan.Path}");
                Console.WriteLine($"Type: {plan.Type}");
                if (plan.Type == ChangeType.Skip)
                {
                    Console.WriteLine("Action: Skip (Content identical)");
                }
                else if (plan.Type == ChangeType.Create)
                {
                    Console.WriteLine("Action: Create new file");
                }
                else
                {
                    Console.WriteLine("Action: Modify existing file (Anchor insertion)");
                }
            }
        }
        else
        {
            Console.WriteLine("\n[LFT] Applying changes...");
            foreach (var plan in plans)
            {
                if (plan.Type == ChangeType.Skip)
                {
                    Console.WriteLine($"[SKIP] {plan.Path}");
                    continue;
                }

                await _writer.WriteFileAsync(plan.Path, plan.NewContent, overwrite: true);
            }

            // 4. Post-Generation Steps
            if (_steps.Any())
            {
                Console.WriteLine("\n[LFT] Running post-generation steps...");
                foreach (var step in _steps)
                {
                    await step.ExecuteAsync(request, result, ct);
                }
            }
        }
    }
}
