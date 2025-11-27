using Lft.Ast.CSharp.Features.Validation.Services;
using Lft.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.App.Pipelines.Steps;

public sealed class SyntaxValidationStep : IGenerationStep
{
    private readonly ICSharpSyntaxValidator _validator;
    private readonly ILogger<SyntaxValidationStep> _logger;

    public SyntaxValidationStep(ICSharpSyntaxValidator validator, ILogger<SyntaxValidationStep>? logger = null)
    {
        _validator = validator;
        _logger = logger ?? NullLogger<SyntaxValidationStep>.Instance;
    }

    public Task ExecuteAsync(GenerationRequest request, GenerationResult result, CancellationToken ct = default)
    {
        _logger.LogInformation("Validating generated code syntax...");
        var hasErrors = false;

        foreach (var file in result.Files)
        {
            if (file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var errors = _validator.Validate(file.Content).ToList();
                if (errors.Any())
                {
                    hasErrors = true;
                    _logger.LogError("Syntax errors in {FilePath}:", file.Path);
                    foreach (var error in errors)
                    {
                        _logger.LogError("  - {Error}", error);
                    }
                }
            }
        }

        if (!hasErrors)
        {
            _logger.LogInformation("Syntax validation passed.");
        }
        else
        {
            _logger.LogWarning("Syntax validation failed for some files.");
        }

        return Task.CompletedTask;
    }
}
