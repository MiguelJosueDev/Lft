using FluentAssertions;
using Lft.Domain.Models;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Variables;

public class ConventionsVariableProviderTests
{
    private readonly ConventionsVariableProvider _provider = new();

    [Theory]
    [InlineData("FundingType", "FundingTypes")]
    [InlineData("Person", "People")]
    [InlineData("Category", "Categories")]
    [InlineData("Child", "Children")]
    [InlineData("Virus", "Viruses")]
    [InlineData("Octopus", "Octopi")]
    [InlineData("Criterion", "Criteria")]
    [InlineData("Datum", "Data")]
    [InlineData("Analysis", "Analyses")]
    [InlineData("Matrix", "Matrices")]
    public void Populate_ShouldPluralizeCorrectly(string singular, string expectedPlural)
    {
        // Arrange
        var request = new GenerationRequest(singular, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityPlural"].Should().Be(expectedPlural);
        variables["_ModuleName"].Should().Be(expectedPlural);
    }

    [Theory]
    [InlineData("FundingType", "funding-type")]
    [InlineData("APIKey", "api-key")]
    [InlineData("HTMLParser", "html-parser")]
    [InlineData("UserAccount", "user-account")]
    [InlineData("IODevice", "io-device")]
    [InlineData("XMLDocument", "xml-document")]
    [InlineData("HTTPRequest", "http-request")]
    public void Populate_ShouldKebabizeCorrectly(string input, string expectedKebab)
    {
        // Arrange
        var request = new GenerationRequest(input, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityKebab"].Should().Be(expectedKebab);
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("User")]
    [InlineData("Order")]
    [InlineData("Invoice")]
    public void Populate_ShouldSetModelNameCorrectly(string entityName)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_ModelName"].Should().Be(entityName);
        variables["_EntityPascal"].Should().Be(entityName);
    }

    [Fact]
    public void Populate_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new GenerationRequest("TestEntity", "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["BaseNamespaceName"].Should().Be("Lft.Generated");
        variables["keyType"].Should().Be("long");
        variables["isMql"].Should().Be(false);
        variables["isRepositoryView"].Should().Be(false);
        variables["IConnectionFactoryName"].Should().Be("IConnectionFactory");
        variables["IUnitOfWorkName"].Should().Be("IUnitOfWork");
    }

    [Fact]
    public void Populate_ShouldSetModelDefinitionWithDefaults()
    {
        // Arrange
        var request = new GenerationRequest("Product", "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables.Should().ContainKey("modelDefinition");
        var modelDef = variables["modelDefinition"];
        modelDef.Should().NotBeNull();
        modelDef.Should().BeOfType<System.Dynamic.ExpandoObject>();
    }

    [Theory]
    [InlineData("A", "A")]           // Single character (unchanged)
    [InlineData("AB", "Ab")]         // Two characters ALLCAPS -> normalized
    [InlineData("ABC", "Abc")]       // Three characters ALLCAPS -> normalized
    [InlineData("ABCD", "Abcd")]     // Four characters ALLCAPS -> normalized
    [InlineData("Ab", "Ab")]         // Mixed case preserved
    [InlineData("aB", "AB")]         // camelCase -> PascalCase
    public void Populate_ShouldHandleShortNames(string entityName, string expected)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_ModelName"].Should().Be(expected);
        variables["_EntityPascal"].Should().Be(expected);
    }

    [Theory]
    [InlineData("VeryLongEntityNameThatExceedsTypicalLimits")]
    [InlineData("SuperCalifragilisticExpialidociousEntity")]
    public void Populate_ShouldHandleLongNames(string entityName)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_ModelName"].Should().Be(entityName);
        variables["_EntityPascal"].Should().Be(entityName);
    }

    [Theory]
    [InlineData("APIEndpoint")]     // Starts with acronym
    [InlineData("HTTPSConnection")] // Contains acronym
    [InlineData("XMLHTTPRequest")]  // Multiple acronyms
    [InlineData("IOStream")]        // Two letter acronym
    public void Populate_ShouldHandleAcronyms(string entityName)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_ModelName"].Should().Be(entityName);
        variables["_EntityKebab"].Should().NotBeNull();
        variables["_EntityKebab"]!.ToString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("Student", "Students")]
    [InlineData("Teacher", "Teachers")]
    [InlineData("Class", "Classes")]
    public void Populate_ShouldHandleCommonBusinessEntities(string entityName, string expectedPlural)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_ModuleName"].Should().Be(expectedPlural);
    }

    [Fact]
    public void Populate_ShouldProvideAllRequiredVariables()
    {
        // Arrange
        var request = new GenerationRequest("TestEntity", "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();

        // Entity variations
        variables.Should().ContainKey("_EntityPascal");
        variables.Should().ContainKey("_EntityPlural");
        variables.Should().ContainKey("_EntityKebab");

        // Model variations
        variables.Should().ContainKey("_ModelName");

        // Module variations
        variables.Should().ContainKey("_ModuleName");

        // Configuration
        variables.Should().ContainKey("BaseNamespaceName");
        variables.Should().ContainKey("keyType");
        variables.Should().ContainKey("isMql");
        variables.Should().ContainKey("isRepositoryView");
        variables.Should().ContainKey("MainModuleName");
        variables.Should().ContainKey("_MainModuleName");
        variables.Should().ContainKey("IConnectionFactoryName");
        variables.Should().ContainKey("IUnitOfWorkName");
        variables.Should().ContainKey("modelDefinition");
    }

    [Theory]
    [InlineData("Product", "Products")]
    [InlineData("User", "Users")]
    [InlineData("Box", "Boxes")]
    [InlineData("Leaf", "Leaves")]
    [InlineData("Knife", "Knives")]
    [InlineData("Potato", "Potatoes")]
    [InlineData("Hero", "Heroes")]
    public void Populate_ShouldHandleRegularAndIrregularPlurals(string singular, string expectedPlural)
    {
        // Arrange
        var request = new GenerationRequest(singular, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityPlural"].Should().Be(expectedPlural);
    }

    [Theory]
    [InlineData("Mouse", "Mice")]
    [InlineData("Foot", "Feet")]
    [InlineData("Tooth", "Teeth")]
    [InlineData("Goose", "Geese")]
    public void Populate_ShouldHandleVowelChangeIrregulars(string singular, string expectedPlural)
    {
        // Arrange
        var request = new GenerationRequest(singular, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityPlural"].Should().Be(expectedPlural);
    }

    [Theory]
    [InlineData("Sheep", "Sheep")]
    [InlineData("Fish", "Fish")]
    [InlineData("Deer", "Deer")]
    [InlineData("Series", "Series")]
    [InlineData("Species", "Species")]
    public void Populate_ShouldHandleUncountableNouns(string word, string expectedPlural)
    {
        // Arrange
        var request = new GenerationRequest(word, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityPlural"].Should().Be(expectedPlural);
    }

    [Theory]
    [InlineData("user", "User", "Users")]           // lowercase -> normalized
    [InlineData("User", "User", "Users")]           // PascalCase (already correct)
    [InlineData("USER", "User", "Users")]           // UPPERCASE -> normalized
    [InlineData("fundingType", "FundingType", "FundingTypes")]  // camelCase -> normalized
    [InlineData("FundingType", "FundingType", "FundingTypes")]  // PascalCase (already correct)
    [InlineData("FUNDINGTYPE", "Fundingtype", "Fundingtypes")] // ALL CAPS compound (loses word boundary info)
    [InlineData("product", "Product", "Products")]  // lowercase -> normalized
    [InlineData("PRODUCT", "Product", "Products")]  // UPPERCASE -> normalized
    public void Populate_ShouldNormalizeInputCaseToProduceConsistentVariables(
        string input,
        string expectedPascal,
        string expectedPlural)
    {
        // Arrange
        var request = new GenerationRequest(input, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        // Note: camelCase variables (_modelName, _moduleName) are computed via Liquid filters in vars.yml
        var variables = context.AsReadOnly();
        variables["_ModelName"].Should().Be(expectedPascal, $"_ModelName for input '{input}'");
        variables["_EntityPascal"].Should().Be(expectedPascal, $"_EntityPascal for input '{input}'");
        variables["_ModuleName"].Should().Be(expectedPlural, $"_ModuleName for input '{input}'");
    }

    [Fact]
    public void Populate_WithCrudSchemaDefinition_ShouldUseMappedFields()
    {
        // Arrange
        var fields = new List<CrudFieldDefinition>
        {
            new("Id", "int", true, true, true, DbName: "Id", DbType: "DbType.Int32"),
            new("UserName", "string", true, false, false, DbName: "UserName", DbType: "DbType.String"),
            new("IsActive", "bool?", false, false, false, DbName: "IsActive", DbType: "DbType.Boolean")
        };

        var schema = new CrudSchemaDefinition("Users", fields, false, "dbo");
        var request = new GenerationRequest("User", "csharp", crudSchemaDefinition: schema);
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["keyType"].Should().Be("int");
        ((bool)variables["isReadOnly"]!).Should().BeFalse();

        // Use reflection-safe approach instead of dynamic to avoid expression tree issues
        var modelDefinition = variables["modelDefinition"] as IDictionary<string, object>;
        modelDefinition.Should().NotBeNull();

        var entity = modelDefinition!["entity"] as IDictionary<string, object>;
        entity.Should().NotBeNull();
        entity!["table"].Should().Be("Users");

        var properties = modelDefinition["properties"] as IEnumerable<object>;
        properties.Should().NotBeNull();

        var propList = properties!.Cast<IDictionary<string, object>>().ToList();
        propList.Should().Contain(p => (string)p["name"] == "UserName");
        propList.Should().Contain(p => (string)p["name"] == "IsActive");
    }
}
