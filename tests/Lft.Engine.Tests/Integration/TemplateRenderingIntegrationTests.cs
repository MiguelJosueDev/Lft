using FluentAssertions;
using Lft.Domain.Models;
using Lft.Engine.Steps;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Integration;

public class TemplateRenderingIntegrationTests
{
    private readonly LiquidTemplateRenderer _renderer = new();

    [Fact]
    public void RenderTemplate_WithBasicVariables_ShouldReplace()
    {
        // Arrange
        var template = "public class {{ _ModelName }}Model { }";
        var variables = new Dictionary<string, object?>
        {
            ["_ModelName"] = "Product"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("public class ProductModel");
    }

    [Fact]
    public void RenderTemplate_WithNamespace_ShouldReplace()
    {
        // Arrange
        var template = "namespace {{ BaseNamespaceName }}.Models;";
        var variables = new Dictionary<string, object?>
        {
            ["BaseNamespaceName"] = "MyApp"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("namespace MyApp.Models;");
    }

    [Fact]
    public void RenderTemplate_WithMultipleVariables_ShouldReplaceAll()
    {
        // Arrange
        var template = @"namespace {{ BaseNamespaceName }}.Models;

public class {{ _ModelName }}Model
{
    public {{ keyType }} Id { get; set; }
}";
        var variables = new Dictionary<string, object?>
        {
            ["BaseNamespaceName"] = "MyApp",
            ["_ModelName"] = "Product",
            ["keyType"] = "long"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("namespace MyApp.Models;");
        result.Should().Contain("public class ProductModel");
        result.Should().Contain("public long Id");
    }

    [Fact]
    public void RenderTemplate_WithConditional_ShouldRenderCorrectly()
    {
        // Arrange
        var template = @"public interface IRepository
{
{%- if isMql %}
    Task<MqlResult> QueryAsync(string query);
{%- endif %}
}";
        var variablesWithMql = new Dictionary<string, object?>
        {
            ["isMql"] = true
        };
        var variablesWithoutMql = new Dictionary<string, object?>
        {
            ["isMql"] = false
        };

        // Act
        var resultWithMql = _renderer.Render(template, variablesWithMql);
        var resultWithoutMql = _renderer.Render(template, variablesWithoutMql);

        // Assert
        resultWithMql.Should().Contain("Task<MqlResult> QueryAsync");
        resultWithoutMql.Should().NotContain("Task<MqlResult> QueryAsync");
    }

    [Fact]
    public void RenderTemplate_WithLoop_ShouldIterateCorrectly()
    {
        // Arrange
        var template = @"public class Model
{
{%- for property in properties %}
    public {{ property.type }} {{ property.name }} { get; set; }
{%- endfor %}
}";

        // Use ExpandoObject for properties in loops
        dynamic prop1 = new System.Dynamic.ExpandoObject();
        prop1.type = "string";
        prop1.name = "Name";

        dynamic prop2 = new System.Dynamic.ExpandoObject();
        prop2.type = "int";
        prop2.name = "Age";

        dynamic prop3 = new System.Dynamic.ExpandoObject();
        prop3.type = "bool";
        prop3.name = "IsActive";

        var variables = new Dictionary<string, object?>
        {
            ["properties"] = new[] { prop1, prop2, prop3 }
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("public string Name { get; set; }");
        result.Should().Contain("public int Age { get; set; }");
        result.Should().Contain("public bool IsActive { get; set; }");
    }

    [Fact]
    public void RenderTemplate_WithCaseSensitiveVariables_ShouldDistinguish()
    {
        // Arrange
        var template = "{{ _ModelName }} vs {{ _moduleName }}";
        var variables = new Dictionary<string, object?>
        {
            ["_ModelName"] = "Product",
            ["_moduleName"] = "products"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("Product vs products");
    }

    [Fact]
    public void RenderTemplate_WithComplexObject_ShouldAccessProperties()
    {
        // Arrange
        var template = "Table: {{ modelDefinition.entity.table }}, Schema: {{ modelDefinition.entity.schema }}";

        // Use ExpandoObject instead of anonymous objects for Liquid compatibility
        dynamic modelDefinition = new System.Dynamic.ExpandoObject();
        dynamic entity = new System.Dynamic.ExpandoObject();
        entity.table = "Products";
        entity.schema = "dbo";
        modelDefinition.entity = entity;

        var variables = new Dictionary<string, object?>
        {
            ["modelDefinition"] = modelDefinition
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Contain("Table: Products, Schema: dbo");
    }

    [Fact]
    public void RenderTemplate_WithFilter_ShouldApplyDefault()
    {
        // Arrange
        var template = "Namespace: {{ _Namespace | default: 'DefaultNamespace' }}";
        var variablesWithValue = new Dictionary<string, object?>
        {
            ["_Namespace"] = "CustomNamespace"
        };
        var variablesWithoutValue = new Dictionary<string, object?>();

        // Act
        var resultWithValue = _renderer.Render(template, variablesWithValue);
        var resultWithoutValue = _renderer.Render(template, variablesWithoutValue);

        // Assert
        resultWithValue.Should().Contain("Namespace: CustomNamespace");
        resultWithoutValue.Should().Contain("Namespace: DefaultNamespace");
    }

    [Fact]
    public void RenderFullCRUDTemplate_WithAllVariables_ShouldGenerateValidCode()
    {
        // Arrange
        var template = @"using AutoMapper;
using {{ BaseNamespaceName }}.Models;

namespace {{ BaseNamespaceName }}.Repositories;

public interface I{{ _ModuleName }}Repository : IRepository<{{ _ModelName }}Model, {{ keyType }}>
{
}

public class {{ _ModuleName }}Repository : BaseRepository<{{ _ModelName }}Model, {{ _ModelName }}Entity, {{ keyType }}>, I{{ _ModuleName }}Repository
{
}";
        var request = new GenerationRequest("Product", "csharp");
        var resolver = new VariableResolver(new IVariableProvider[]
        {
            new CliVariableProvider(),
            new ConventionsVariableProvider()
        });
        var context = resolver.Resolve(request);

        // Act
        var result = _renderer.Render(template, context.AsReadOnly());

        // Assert
        result.Should().Contain("using Lft.Generated.Models;");
        result.Should().Contain("namespace Lft.Generated.Repositories;");
        result.Should().Contain("public interface IProductsRepository : IRepository<ProductModel, long>");
        result.Should().Contain("public class ProductsRepository : BaseRepository<ProductModel, ProductEntity, long>");
    }

    [Theory]
    [InlineData("Person", "People")]
    [InlineData("Category", "Categories")]
    [InlineData("Child", "Children")]
    public void RenderTemplate_WithIrregularPlurals_ShouldUseCorrectPlural(string singular, string expectedPlural)
    {
        // Arrange
        var template = "public class {{ _ModuleName }}Repository { }";
        var request = new GenerationRequest(singular, "csharp");
        var resolver = new VariableResolver(new IVariableProvider[]
        {
            new ConventionsVariableProvider()
        });
        var context = resolver.Resolve(request);

        // Act
        var result = _renderer.Render(template, context.AsReadOnly());

        // Assert
        result.Should().Contain($"public class {expectedPlural}Repository");
    }

    [Fact]
    public void RenderTemplate_WithEmptyTemplate_ShouldReturnEmpty()
    {
        // Arrange
        var template = "";
        var variables = new Dictionary<string, object?>();

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RenderTemplate_WithWhitespaceTemplate_ShouldReturnEmpty()
    {
        // Arrange
        var template = "   \n  \t  ";
        var variables = new Dictionary<string, object?>();

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RenderTemplate_WithInvalidSyntax_ShouldThrowException()
    {
        // Arrange
        var template = "{{ unclosed variable";
        var variables = new Dictionary<string, object?>();

        // Act
        Action act = () => _renderer.Render(template, variables);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to parse Liquid template:*");
    }
}