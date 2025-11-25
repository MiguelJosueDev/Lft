using Lft.Domain.Models;

namespace Lft.Engine.Variables;

public sealed class CliVariableProvider : IVariableProvider
{
    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        ctx.Set("_EntityName", request.EntityName);
        ctx.Set("_Language", request.Language);
        ctx.Set("_TemplatePack", request.TemplatePack);
    }
}
