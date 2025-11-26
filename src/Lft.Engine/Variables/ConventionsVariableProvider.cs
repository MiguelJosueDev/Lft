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

        // Model variations
        // Fluid uses case-insensitive variable lookup, so we use distinct suffixes
        // to avoid collision between PascalCase and camelCase versions
        ctx.Set("_ModelName", entity);                               // FundingType (PascalCase) - for class names
        ctx.Set("_modelNameCamel", entity.Camelize());               // fundingType (camelCase) - for JS variables

        // Module variations (plural)
        var plural = entity.Pluralize();                             // Use Humanizer for proper pluralization
        ctx.Set("_ModuleName", plural);                              // FundingTypes (PascalCase) - for class names
        ctx.Set("_moduleNameCamel", plural.Camelize());              // fundingTypes (camelCase) - for JS services

        // Base namespace (can be overridden by lft.config.json)
        ctx.SetDefault("BaseNamespaceName", "Lft.Generated");

        // Default configuration values (can be overridden by lft.config.json)
        ctx.SetDefault("keyType", "long");
        ctx.SetDefault("isMql", false);
        ctx.SetDefault("isRepositoryView", false);

        // Main module (can be overridden by lft.config.json)
        ctx.SetDefault("MainModuleName", "Generated");
        ctx.SetDefault("_MainModuleName", "Generated");

        // Connection factory and UnitOfWork names (can be overridden by lft.config.json)
        ctx.SetDefault("IConnectionFactoryName", "IConnectionFactory");
        ctx.SetDefault("IUnitOfWorkName", "IUnitOfWork");

        // Route pattern config (can be overridden by lft.config.json)
        ctx.SetDefault("RoutePattern", "MapModelRoutes");
        ctx.SetDefault("RoutesExtensionSuffix", "Extensions");

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
