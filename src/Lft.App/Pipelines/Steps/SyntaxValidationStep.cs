using Lft.Ast.CSharp;
using Lft.Domain.Models;

namespace Lft.App.Pipelines.Steps;

public sealed class SyntaxValidationStep : IGenerationStep
{
    private readonly ICSharpSyntaxValidator _validator;

    public SyntaxValidationStep(ICSharpSyntaxValidator validator)
    {
        _validator = validator;
    }

    public Task ExecuteAsync(GenerationRequest request, GenerationResult result, CancellationToken ct = default)
    {
        Console.WriteLine("[LFT] Validating generated code syntax...");
        var hasErrors = false;

        foreach (var file in result.Files)
        {
            if (file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var errors = _validator.Validate(file.Content).ToList();
                if (errors.Any())
                {
                    hasErrors = true;
                    Console.WriteLine($"[ERROR] Syntax errors in {file.Path}:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }
            }
        }

        if (!hasErrors)
        {
            Console.WriteLine("[INFO] Syntax validation passed.");
        }
        else
        {
            Console.WriteLine("[WARN] Syntax validation failed for some files.");
        }

        return Task.CompletedTask;
    }
}
