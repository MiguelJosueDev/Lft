using Lft.Domain.Models;
using Lft.Engine.Templates;
using Lft.Engine.Variables;

namespace Lft.Engine.Steps;

public sealed class StepExecutor
{
    private readonly string _templatesRoot;
    private readonly ITemplateRenderer _renderer;

    public StepExecutor(string templatesRoot, ITemplateRenderer renderer)
    {
        _templatesRoot = templatesRoot;
        _renderer = renderer;
    }

    public async Task<IReadOnlyList<GeneratedFile>> ExecuteAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        CancellationToken ct = default)
    {
        var result = new List<GeneratedFile>();
        await ExecuteInternalAsync(step, request, vars, result, ct);
        return result;
    }

    private async Task ExecuteInternalAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        List<GeneratedFile> files,
        CancellationToken ct)
    {
        switch (step.Action.ToLowerInvariant())
        {
            case "group":
                foreach (var child in step.Steps)
                    await ExecuteInternalAsync(child, request, vars, files, ct);
                break;

            case "create":
                await ExecuteCreateAsync(step, request, vars, files, ct);
                break;

            default:
                // Ignore unknown actions for now
                break;
        }
    }

    private async Task ExecuteCreateAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        List<GeneratedFile> files,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(step.Source))
            throw new InvalidOperationException($"Step '{step.Name}' of type 'create' must have a 'source'.");

        // 1. Read source template
        var sourcePath = Path.Combine(_templatesRoot, request.TemplatePack, step.Source);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Template source not found: {sourcePath}");
        }

        var templateContent = await File.ReadAllTextAsync(sourcePath, ct);

        // 2. Render content
        var rendered = _renderer.Render(templateContent, vars.AsReadOnly());

        // 3. Resolve output path
        var relativeOutputPath = _renderer.Render(step.Output ?? "", vars.AsReadOnly());

        // In a real scenario we might combine with OutputDirectory here or later.
        // For now, we return the relative path as per GeneratedFile contract, 
        // but the CLI might prepend OutputDir when writing.
        // The prompt said "files.Add(new GeneratedFile(finalPath, rendered));"
        // Let's keep it relative so the CLI can decide where to put it, or absolute if we want.
        // The GenerationRequest has OutputDirectory.
        // Let's make it relative to the OutputDirectory.

        files.Add(new GeneratedFile(relativeOutputPath, rendered));
    }
}
