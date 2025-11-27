using Lft.Discovery;
using Lft.Engine.Variables;

namespace Lft.Engine.Discovery;

public interface IDiscoveryService
{
    Task<ProjectManifest> AnalyzeAndPopulateAsync(string profileRoot, VariableContext ctx, CancellationToken ct = default);
}
