using Lft.Domain.Models;

namespace Lft.Engine.Variables;

public interface IVariableProvider
{
    void Populate(VariableContext ctx, GenerationRequest request);
}
