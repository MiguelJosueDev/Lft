using Lft.Domain.Models;

namespace Lft.Integration;

public interface IFileIntegrationService
{
    Task<FileChangePlan> IntegrateAsync(
        string filePath,
        string newFragment,
        IntegrationOptions options,
        CancellationToken ct = default);
}
