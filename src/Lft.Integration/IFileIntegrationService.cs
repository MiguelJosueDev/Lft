using Lft.Domain.Models;

namespace Lft.Integration;

public interface IFileIntegrationService
{
    Task<FileChangePlan> IntegrateAsync(
        string filePath,
        string newFragment,
        // In the future we might pass an IntegrationOptions object or similar
        CancellationToken ct = default);
}
