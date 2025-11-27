using Lft.Ast.CSharp;
using Lft.Domain.Models;
using Lft.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.App.Pipelines.Steps.Strategies;

public class MapperInjectionStrategy : IInjectionStrategy
{
    private readonly ISmartPathResolver _pathResolver;
    private readonly ILogger<MapperInjectionStrategy> _logger;

    public MapperInjectionStrategy(ISmartPathResolver pathResolver, ILogger<MapperInjectionStrategy>? logger = null)
    {
        _pathResolver = pathResolver;
        _logger = logger ?? NullLogger<MapperInjectionStrategy>.Instance;
    }

    public bool CanHandle(GeneratedFile file)
    {
        return file.Path.EndsWith("Entity.cs") || file.Path.EndsWith("Model.cs");
    }

    public async Task InjectAsync(GenerationRequest request, GenerationResult result, GeneratedFile file, ICSharpInjectionService injector, CancellationToken ct = default)
    {
        // Only run once per entity (on Entity.cs)
        if (!file.Path.EndsWith($"{request.EntityName}Entity.cs"))
        {
            return;
        }

        var outputDir = request.OutputDirectory ?? Directory.GetCurrentDirectory();

        var resolution = _pathResolver.Resolve("MappingProfile.cs", outputDir, request.Profile);

        if (resolution == null)
        {
            _logger.LogWarning("No MappingProfile found.");
            return;
        }

        var targetFiles = Directory.GetFiles(resolution.Directory, "*MappingProfile.cs");

        if (!targetFiles.Any())
        {
            _logger.LogWarning("No MappingProfile found in {Directory}.", resolution.Directory);
            return;
        }

        var targetFile = targetFiles.First();
        var entity = request.EntityName;

        var className = Path.GetFileNameWithoutExtension(targetFile);

        var snippet = $"CreateMap<{entity}Model, {entity}Entity>().ReverseMap();";

        var injectionRequest = new CodeInjectionRequest(
            FilePath: targetFile,
            ClassNameSuffix: "MappingProfile",
            MethodName: className,
            Snippet: snippet,
            Position: CodeInjectionPosition.End,
            Pattern: CodeInjectionPattern.CreateMapBlock
        );

        _logger.LogInformation("Injecting mapping for {Entity} into {File}", entity, targetFile);

        await injector.InjectIntoMethodAsync(injectionRequest, ct);
    }
}
