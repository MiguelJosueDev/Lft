using System.Text;
using Humanizer;
using Lft.Domain.Models;

namespace Lft.Engine.Variables;

public sealed class ConventionsVariableProvider : IVariableProvider
{
    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        var entity = request.EntityName;

        // Entity variations - use PascalCase for class names
        ctx.Set("_EntityPascal", entity);                            // FundingType
        ctx.Set("_EntityPlural", entity.Pluralize());                // FundingTypes (using Humanizer)
        ctx.Set("_EntityKebab", entity.Kebaberize());                // funding-type (using Humanizer)

        // Model variations (same as entity for simple case)
        ctx.Set("_ModelName", entity);                               // FundingType

        // Module variations (plural)
        var plural = entity.Pluralize();                             // Use Humanizer for proper pluralization
        ctx.Set("_ModuleName", plural);                              // FundingTypes

        // Base namespace
        ctx.Set("BaseNamespaceName", "Lft.Generated");

        // Default configuration values
        ctx.Set("keyType", "long");
        ctx.Set("isMql", false);
        ctx.Set("isRepositoryView", false);

        // Main module (can be overridden if needed)
        ctx.Set("MainModuleName", "Generated");
        ctx.Set("_MainModuleName", "Generated");

        // Connection factory and UnitOfWork names
        ctx.Set("IConnectionFactoryName", "IConnectionFactory");
        ctx.Set("IUnitOfWorkName", "IUnitOfWork");

        // Empty model definition (no properties by default)
        ctx.Set("modelDefinition", new { properties = new object[] { }, entity = new { table = entity, schema = "dbo", primary = new { dbName = "Id", dbType = "DbType.Int64" } } });
    }
}
