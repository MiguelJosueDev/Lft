using Lft.Domain.Models;

namespace Lft.Engine.Variables;

public sealed class VariableResolver
{
    private readonly IReadOnlyList<IVariableProvider> _providers;

    public VariableResolver(IEnumerable<IVariableProvider> providers)
    {
        _providers = providers.ToList();
    }

    public VariableContext Resolve(GenerationRequest request)
    {
        var ctx = new VariableContext();
        foreach (var provider in _providers)
        {
            provider.Populate(ctx, request);
        }
        return ctx;
    }
}
