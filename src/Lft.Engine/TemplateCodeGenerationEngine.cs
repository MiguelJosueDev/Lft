using Lft.Domain.Models;
using Lft.Discovery;
using Lft.Engine.Discovery;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.Engine;

public sealed class TemplateCodeGenerationEngine : ICodeGenerationEngine
{
    private readonly TemplatePackLoader _packLoader;
    private readonly VariableResolver _variableResolver;
    private readonly StepExecutor _stepExecutor;
    private readonly IDiscoveryService? _discoveryService;
    private readonly ILogger<TemplateCodeGenerationEngine> _logger;

    public TemplateCodeGenerationEngine(
        TemplatePackLoader packLoader,
        VariableResolver variableResolver,
        StepExecutor stepExecutor,
        IDiscoveryService? discoveryService = null,
        ILogger<TemplateCodeGenerationEngine>? logger = null)
    {
        _packLoader = packLoader;
        _variableResolver = variableResolver;
        _stepExecutor = stepExecutor;
        _discoveryService = discoveryService;
        _logger = logger ?? NullLogger<TemplateCodeGenerationEngine>.Instance;
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
        if (!string.IsNullOrEmpty(profileRoot) && _discoveryService != null)
        {
            try
            {
                var manifest = await _discoveryService.AnalyzeAndPopulateAsync(profileRoot, vars, cancellationToken);
                _stepExecutor.SetManifest(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Discovery failed while processing profile root '{ProfileRoot}'", profileRoot);
                // Continue without discovery - fall back to legacy mode
            }
        }

        var files = await _stepExecutor.ExecuteAsync(crudEntry, request, vars, cancellationToken);

        return new GenerationResult(files, vars.AsReadOnly());
    }
}
