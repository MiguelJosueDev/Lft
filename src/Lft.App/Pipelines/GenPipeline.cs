using Lft.Domain.Models;
using Lft.Engine;
using Lft.Integration;

namespace Lft.App.Pipelines;

public class GenPipeline
{
    private readonly ICodeGenerationEngine _engine;
    private readonly IFileIntegrationService _integrationService;
    private readonly IFileWriter _writer;

    public GenPipeline(
        ICodeGenerationEngine engine,
        IFileIntegrationService integrationService,
        IFileWriter writer)
    {
        _engine = engine;
        _integrationService = integrationService;
        _writer = writer;
    }

    public async Task ExecuteAsync(GenerationRequest request, bool dryRun, CancellationToken ct = default)
    {
        // 1. Generate (In-Memory)
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
            var fullPath = Path.Combine(request.OutputDirectory ?? Directory.GetCurrentDirectory(), file.Path);
            
            // Calculate plan (Create or Modify)
            var plan = await _integrationService.IntegrateAsync(fullPath, file.Content, ct);
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
                // In a real diff tool we would show the diff here.
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
        }
    }
}
