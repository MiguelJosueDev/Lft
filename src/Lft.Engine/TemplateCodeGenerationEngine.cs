using Lft.Domain.Models;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;

namespace Lft.Engine;

public sealed class TemplateCodeGenerationEngine : ICodeGenerationEngine
{
    private readonly TemplatePackLoader _packLoader;
    private readonly VariableResolver _variableResolver;
    private readonly StepExecutor _stepExecutor;

    public TemplateCodeGenerationEngine(
        TemplatePackLoader packLoader,
        VariableResolver variableResolver,
        StepExecutor stepExecutor)
    {
        _packLoader = packLoader;
        _variableResolver = variableResolver;
        _stepExecutor = stepExecutor;
    }

    public async Task<GenerationResult> GenerateAsync(
        GenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var pack = await _packLoader.LoadAsync(request.TemplatePack, cancellationToken);

        var crudEntry = pack.EntryPoints
            .FirstOrDefault(s =>
                string.Equals(s.CommandName, request.CommandName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Name, request.CommandName, StringComparison.OrdinalIgnoreCase));

        if (crudEntry is null)
        {
            throw new InvalidOperationException(
                $"Command '{request.CommandName}' not found in template pack '{request.TemplatePack}'.");
        }

        var vars = _variableResolver.Resolve(request);
        var files = await _stepExecutor.ExecuteAsync(crudEntry, request, vars, cancellationToken);

        return new GenerationResult(files);
    }
}
