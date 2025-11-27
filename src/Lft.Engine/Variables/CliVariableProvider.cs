using Lft.Domain.Models;

namespace Lft.Engine.Variables;

public sealed class CliVariableProvider : IVariableProvider
{
    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        ctx.Set("_EntityName", request.EntityName);
        ctx.Set("_Language", request.Language);
        ctx.Set("_TemplatePack", request.TemplatePack);

        // Apply --set key=value variables from CLI
        foreach (var (key, value) in request.Variables)
        {
            // Parse boolean values
            if (bool.TryParse(value, out var boolValue))
            {
                ctx.Set(key, boolValue);
            }
            else
            {
                ctx.Set(key, value);
            }
        }
    }
}
