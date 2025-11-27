using Lft.Domain.Models;
using Lft.Engine.Variables;

namespace Lft.CrudGeneration.Tests;

/// <summary>
/// Variable provider for tests that simulates CLI --set flags.
/// Variables set here take precedence over defaults from ConventionsVariableProvider.
/// </summary>
public class TestVariableProvider : IVariableProvider
{
    private readonly Dictionary<string, object> _variables;

    public TestVariableProvider(Dictionary<string, object> variables)
    {
        _variables = variables;
    }

    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        foreach (var (key, value) in _variables)
        {
            ctx.Set(key, value);
        }
    }
}
