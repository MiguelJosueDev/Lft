using System.Dynamic;
using System.Text;
using Humanizer;
using Lft.Domain.Models;

namespace Lft.Engine.Variables;

public sealed class ConventionsVariableProvider : IVariableProvider
{
    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        // Normalize input to PascalCase - ensures "user", "User", "USER" all become "User"
        // Note: Pascalize() doesn't handle ALLCAPS, so we lowercase first only if entirely uppercase
        var raw = request.EntityName;
        var entity = IsAllUpperCase(raw) ? raw.ToLower().Pascalize() : raw.Pascalize();

        // Entity variations - use PascalCase for class names
        ctx.Set("_EntityPascal", entity);                            // FundingType
        ctx.Set("_EntityPlural", entity.Pluralize());                // FundingTypes (using Humanizer)
        ctx.Set("_EntityKebab", entity.Kebaberize());                // funding-type (using Humanizer)

        // Model variations (same as entity for simple case)
        // Note: _modelName and _moduleName are computed in vars.yml using Liquid filters
        // to avoid case-insensitive collision in Fluid
        ctx.Set("_ModelName", entity);                               // FundingType (PascalCase)

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

        // Model definition using ExpandoObject for Liquid compatibility
        // This allows Liquid to access nested properties like {{ modelDefinition.entity.table }}
        dynamic modelDefinition = new ExpandoObject();
        modelDefinition.properties = new List<object>();

        dynamic entityDef = new ExpandoObject();
        entityDef.table = entity;
        entityDef.schema = "dbo";

        dynamic primaryDef = new ExpandoObject();
        primaryDef.dbName = "Id";
        primaryDef.dbType = "DbType.Int64";

        entityDef.primary = primaryDef;
        modelDefinition.entity = entityDef;

        ctx.Set("modelDefinition", modelDefinition);
    }

    private static bool IsAllUpperCase(string value)
        => !string.IsNullOrEmpty(value) && value.All(c => !char.IsLetter(c) || char.IsUpper(c));
}
