using Lft.Ast.CSharp;
using Lft.Domain.Models;

namespace Lft.App.Pipelines.Steps.Strategies;

public class ServiceInjectionStrategy : IInjectionStrategy
{
    public bool CanHandle(GeneratedFile file)
    {
        return file.Path.Contains("Service") && file.Path.EndsWith(".cs");
    }

    public async Task InjectAsync(GenerationRequest request, GenerationResult result, GeneratedFile file, ICSharpInjectionService injector, CancellationToken ct = default)
    {
        // Only trigger once per entity (on Service implementation, not interface)
        if (!file.Path.EndsWith($"{request.EntityName}sService.cs") &&
            !file.Path.EndsWith($"{request.EntityName}Service.cs"))
        {
            return;
        }

        // Skip interface files
        if (file.Path.Contains($"I{request.EntityName}"))
        {
            return;
        }

        var outputDir = request.OutputDirectory ?? Directory.GetCurrentDirectory();

        var targetFiles = Directory.GetFiles(outputDir, "ServiceRegistrationExtensions.cs", SearchOption.AllDirectories)
            .Where(f => f.Contains("Services") && !f.Contains("Repositories"))
            .ToList();

        if (!targetFiles.Any())
        {
            Console.WriteLine("[WARN] No Service ServiceRegistrationExtensions found.");
            return;
        }

        var targetFile = targetFiles.First();
        var entity = request.EntityName;
        var moduleName = result.GetVariable("MainModuleName", "Module");

        var snippet = $"services.AddScoped<I{entity}sService, {entity}sService>();";

        var injectionRequest = new CodeInjectionRequest(
            FilePath: targetFile,
            ClassNameSuffix: "ServiceRegistrationExtensions",
            MethodName: $"Add{moduleName}Services",
            Snippet: snippet,
            Position: CodeInjectionPosition.End
        );

        Console.WriteLine($"[INFO] Injecting service registration for {entity} into {targetFile}");
        await injector.InjectIntoMethodAsync(injectionRequest, ct);
    }
}
