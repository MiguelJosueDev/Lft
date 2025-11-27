using Lft.Ast.CSharp.Features.Injection.Services;
using Lft.Domain.Models;

namespace Lft.App.Pipelines.Steps.Strategies;

public interface IInjectionStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the generated file (e.g., is it a Repository?).
    /// </summary>
    bool CanHandle(GeneratedFile file);

    /// <summary>
    /// Injects the necessary code into the existing codebase based on the generated file.
    /// </summary>
    Task InjectAsync(GenerationRequest request, GenerationResult result, GeneratedFile file, ICSharpInjectionService injector, CancellationToken ct = default);
}
