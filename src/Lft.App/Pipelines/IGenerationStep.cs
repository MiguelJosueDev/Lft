using Lft.Domain.Models;

namespace Lft.App.Pipelines;

public interface IGenerationStep
{
    Task ExecuteAsync(GenerationRequest request, GenerationResult result, CancellationToken ct = default);
}
