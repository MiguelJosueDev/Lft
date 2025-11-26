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
        ctx.Set("_routeModuleName", plural.Kebaberize());            // funding-types (kebab-case) - for route prefixes

        // Base namespace (can be overridden by lft.config.json)
        ctx.SetDefault("BaseNamespaceName", "Lft.Generated");

        // Default configuration values
        ctx.Set("keyType", "long");
        ctx.Set("isMql", false);
        ctx.Set("isRepositoryView", false);
        ctx.Set("isReadOnly", request.CrudSchemaDefinition?.IsReadOnly ?? false);

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
        entityDef.table = request.CrudSchemaDefinition?.Name ?? entity;
        entityDef.schema = request.CrudSchemaDefinition?.SchemaName ?? "dbo";

        if (request.CrudSchemaDefinition is { } crudSchema)
        {
            var primaryField = crudSchema.Fields.FirstOrDefault(f => f.IsPrimaryKey);
            if (primaryField is not null)
            {
                dynamic primaryDef = new ExpandoObject();
                primaryDef.dbName = primaryField.DbName ?? primaryField.Name;
                primaryDef.dbType = primaryField.DbType ?? MapClrToDbType(primaryField.ClrType);
                entityDef.primary = primaryDef;

                ctx.Set("keyType", primaryField.ClrType);
            }

            foreach (var field in crudSchema.Fields)
            {
                if (field.IsPrimaryKey)
                {
                    continue;
                }

                dynamic propertyDef = new ExpandoObject();
                propertyDef.name = field.Name;
                propertyDef.type = field.ClrType;
                propertyDef.dbName = field.DbName ?? field.Name;
                propertyDef.dbType = field.DbType ?? MapClrToDbType(field.ClrType);
                propertyDef.isRequired = field.IsRequired;
                propertyDef.isIdentity = field.IsIdentity;
                propertyDef.maxLength = field.MaxLength;
                propertyDef.defaultValue = field.DefaultValue;
                modelDefinition.properties.Add(propertyDef);
            }
        }
        else
        {
            dynamic primaryDef = new ExpandoObject();
            primaryDef.dbName = "Id";
            primaryDef.dbType = "DbType.Int64";
            entityDef.primary = primaryDef;
        }

        modelDefinition.entity = entityDef;

        ctx.Set("modelDefinition", modelDefinition);
    }

    private static string MapClrToDbType(string clrType)
    {
        var normalized = clrType.TrimEnd('?');
        return normalized switch
        {
            "long" => "DbType.Int64",
            "int" => "DbType.Int32",
            "short" => "DbType.Int16",
            "byte" => "DbType.Byte",
            "bool" => "DbType.Boolean",
            "Guid" => "DbType.Guid",
            "DateTime" => "DbType.DateTime2",
            "TimeSpan" => "DbType.Time",
            "decimal" => "DbType.Decimal",
            "double" => "DbType.Double",
            "float" => "DbType.Single",
            "byte[]" => "DbType.Binary",
            _ => "DbType.String"
        };
    }

    private static bool IsAllUpperCase(string value)
        => !string.IsNullOrEmpty(value) && value.All(c => !char.IsLetter(c) || char.IsUpper(c));
}
