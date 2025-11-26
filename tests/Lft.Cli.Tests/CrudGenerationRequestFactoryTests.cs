using FluentAssertions;
using Lft.Cli;
using Lft.Engine.Variables;
using Lft.SqlSchema;

namespace Lft.Cli.Tests;

public class CrudGenerationRequestFactoryTests
{
    private readonly SqlServerSchemaParser _parser = new();
    private readonly SqlObjectToCrudMapper _mapper = new();

    [Fact]
    public async Task CreateAsync_WithDdl_ShouldPopulateCrudSchemaDefinition()
    {
        var ddl = @"CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserName nvarchar(100) NOT NULL,
    IsActive bit NOT NULL
);";

        var factory = new CrudGenerationRequestFactory(_parser, _mapper);
        var options = new CrudGenerationOptions("User", "csharp", Ddl: ddl);

        var request = await factory.CreateAsync(options);

        request.CrudSchemaDefinition.Should().NotBeNull();
        var crudSchema = request.CrudSchemaDefinition!;
        crudSchema.Fields.Should().Contain(f => f.Name == "Id" && f.IsPrimaryKey);
        crudSchema.Fields.Should().Contain(f => f.Name == "UserName" && f.ClrType == "string");
        crudSchema.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithDdl_ShouldPopulateModelDefinitionVariables()
    {
        var ddl = @"CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserName nvarchar(100) NOT NULL,
    IsActive bit NULL
);";

        var factory = new CrudGenerationRequestFactory(_parser, _mapper);
        var options = new CrudGenerationOptions("User", "csharp", Ddl: ddl);
        var request = await factory.CreateAsync(options);

        var resolver = new VariableResolver(new IVariableProvider[]
        {
            new CliVariableProvider(),
            new ConventionsVariableProvider()
        });

        var context = resolver.Resolve(request);
        var variables = context.AsReadOnly();

        variables.Should().ContainKey("modelDefinition");
        dynamic modelDefinition = variables["modelDefinition"]!;
        ((string)modelDefinition.entity.table).Should().Be("Users");
        ((string)modelDefinition.entity.schema).Should().Be("dbo");

        var properties = (IEnumerable<object>)modelDefinition.properties;
        properties.Should().ContainSingle(p => ((dynamic)p).name == "UserName");
        properties.Should().Contain(p => ((dynamic)p).name == "IsActive");
    }
}
