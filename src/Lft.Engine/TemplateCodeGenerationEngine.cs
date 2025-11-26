using Lft.Discovery;
using Lft.Domain.Models;
using Lft.Engine.Discovery;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;

namespace Lft.Engine;

public sealed class TemplateCodeGenerationEngine : ICodeGenerationEngine
{
    private readonly TemplatePackLoader _packLoader;
    private readonly VariableResolver _variableResolver;
    private readonly StepExecutor _stepExecutor;
    private readonly IProjectAnalyzer? _projectAnalyzer;

    public TemplateCodeGenerationEngine(
        TemplatePackLoader packLoader,
        VariableResolver variableResolver,
        StepExecutor stepExecutor,
        IProjectAnalyzer? projectAnalyzer = null)
    {
        _packLoader = packLoader;
        _variableResolver = variableResolver;
        _stepExecutor = stepExecutor;
        _projectAnalyzer = projectAnalyzer;
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

        // Run project discovery if profile root is available
        var profileRoot = vars.AsReadOnly().TryGetValue("_ProfileRoot", out var rootVal) ? rootVal as string : null;
        if (!string.IsNullOrEmpty(profileRoot) && _projectAnalyzer != null)
        {
            try
            {
                var discoveryService = new DiscoveryService(_projectAnalyzer);
                var manifest = await discoveryService.AnalyzeAndPopulateAsync(profileRoot, vars, cancellationToken);
                _stepExecutor.SetManifest(manifest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LFT] Discovery warning: {ex.Message}");
                // Continue without discovery - fall back to legacy mode
            }
        }

        var files = await _stepExecutor.ExecuteAsync(crudEntry, request, vars, cancellationToken);

        return new GenerationResult(files, vars.AsReadOnly());
    }
}
