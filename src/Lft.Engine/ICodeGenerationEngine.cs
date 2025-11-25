using Lft.Domain.Models;

namespace Lft.Engine;

public interface ICodeGenerationEngine
{
    Task<GenerationResult> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default);
}
