using Lft.Analyzer.Core;

namespace Lft.Analyzer.Core.Rules;

public class LayerDependencyRule : IRule
{
    public string Id => "ARCH001";
    public string Description => "Enforce strict layering: Domain -> Application -> Api/Infrastructure";

    public Task<IEnumerable<Violation>> EvaluateAsync(IEnumerable<ArchNode> nodes)
    {
        var violations = new List<Violation>();
        var nodeMap = nodes.ToDictionary(n => n.Id, n => n);

        foreach (var node in nodes)
        {
            foreach (var dependencyId in node.DependsOnIds)
            {
                if (!nodeMap.TryGetValue(dependencyId, out var dependency)) continue;

                // Ignore dependencies within the same layer
                if (node.Layer == dependency.Layer) continue;

                // Ignore dependencies on Unknown layers (external libs, system types)
                if (dependency.Layer == Layer.Unknown) continue;

                if (IsViolation(node.Layer, dependency.Layer))
                {
                    violations.Add(new Violation(
                        Id,
                        $"Layer violation: {node.Layer} ({node.Name}) depends on {dependency.Layer} ({dependency.Name})",
                        node.Metadata.GetValueOrDefault("FilePath", "Unknown")
                    ));
                }
            }
        }

        return Task.FromResult<IEnumerable<Violation>>(violations);
    }

    private bool IsViolation(Layer source, Layer target)
    {
        return source switch
        {
            Layer.Domain => target != Layer.Domain, // Domain cannot depend on anything else (except Unknown/System)
            Layer.Application => target == Layer.Api || target == Layer.Infrastructure, // Application cannot depend on Api or Infra
            Layer.Infrastructure => target == Layer.Api, // Infra cannot depend on Api
            Layer.Api => false, // Api can depend on anything (usually App/Infra)
            _ => false
        };
    }
}
