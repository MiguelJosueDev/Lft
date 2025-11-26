using Lft.Ast.CSharp;
using Lft.Domain.Models;
using Lft.App.Services;

namespace Lft.App.Pipelines.Steps.Strategies;

public class MapperInjectionStrategy : IInjectionStrategy
{
    private readonly ISmartPathResolver _pathResolver;

    public MapperInjectionStrategy(ISmartPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
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
            Console.WriteLine("[WARN] No MappingProfile found.");
            return;
        }

        var targetFiles = Directory.GetFiles(resolution.Directory, "*MappingProfile.cs");

        if (!targetFiles.Any())
        {
            Console.WriteLine($"[WARN] No MappingProfile found in {resolution.Directory}.");
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
            Position: CodeInjectionPosition.End
        );

        Console.WriteLine($"[INFO] Injecting mapping for {entity} into {targetFile}");

        await injector.InjectIntoMethodAsync(injectionRequest, ct);
    }
}
