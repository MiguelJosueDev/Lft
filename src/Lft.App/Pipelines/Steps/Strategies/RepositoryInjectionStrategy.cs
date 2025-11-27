using Lft.Ast.CSharp;
using Lft.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.App.Pipelines.Steps.Strategies;

public class RepositoryInjectionStrategy : IInjectionStrategy
{
    private readonly ILogger<RepositoryInjectionStrategy> _logger;

    public RepositoryInjectionStrategy(ILogger<RepositoryInjectionStrategy>? logger = null)
    {
        _logger = logger ?? NullLogger<RepositoryInjectionStrategy>.Instance;
    }

    public bool CanHandle(GeneratedFile file)
    {
        return file.Path.Contains("Repository") && file.Path.EndsWith(".cs");
    }

    public async Task InjectAsync(GenerationRequest request, GenerationResult result, GeneratedFile file, ICSharpInjectionService injector, CancellationToken ct = default)
    {
        // Only trigger once per entity (on Repository file, not interface)
        if (!file.Path.EndsWith($"{request.EntityName}sRepository.cs") &&
            !file.Path.EndsWith($"{request.EntityName}Repository.cs"))
        {
            return;
        }

        var outputDir = request.OutputDirectory ?? Directory.GetCurrentDirectory();

        var targetFiles = Directory.GetFiles(outputDir, "ServiceRegistrationExtensions.cs", SearchOption.AllDirectories)
            .Where(f => f.Contains("Repositories"))
            .ToList();

        if (!targetFiles.Any())
        {
            _logger.LogWarning("No Repository ServiceRegistrationExtensions found.");
            return;
        }

        var targetFile = targetFiles.First();
        var entity = request.EntityName;
        var moduleName = result.GetVariable("MainModuleName", "Module");

        var snippet = $"services.AddScoped<I{entity}sRepository, {entity}sRepository>();";

        var injectionRequest = new CodeInjectionRequest(
            FilePath: targetFile,
            ClassNameSuffix: "ServiceRegistrationExtensions",
            MethodName: $"Add{moduleName}Repositories",
            Snippet: snippet,
            Position: CodeInjectionPosition.End,
            Pattern: CodeInjectionPattern.AddScopedBlock
        );

        _logger.LogInformation("Injecting repository registration for {Entity} into {File}", entity, targetFile);
        await injector.InjectIntoMethodAsync(injectionRequest, ct);
    }
}
