using FluentAssertions;
using Lft.Domain.Models;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Variables;

public class CliVariableProviderTests
{
    private readonly CliVariableProvider _provider = new();

    [Fact]
    public void Populate_ShouldSetEntityName()
    {
        // Arrange
        var request = new GenerationRequest("Product", "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityName"].Should().Be("Product");
    }

    [Fact]
    public void Populate_ShouldSetLanguage()
    {
        // Arrange
        var request = new GenerationRequest("Product", "typescript");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_Language"].Should().Be("typescript");
    }

    [Fact]
    public void Populate_ShouldSetTemplatePack()
    {
        // Arrange
        var request = new GenerationRequest("Product", "csharp", templatePack: "custom");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_TemplatePack"].Should().Be("custom");
    }

    [Fact]
    public void Populate_ShouldSetDefaultTemplatePack()
    {
        // Arrange
        var request = new GenerationRequest("Product", "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_TemplatePack"].Should().Be("main");
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("User")]
    [InlineData("VeryLongEntityNameForTesting")]
    [InlineData("A")]
    [InlineData("ABC123")]
    public void Populate_ShouldHandleVariousEntityNames(string entityName)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_EntityName"].Should().Be(entityName);
    }

    [Theory]
    [InlineData("csharp")]
    [InlineData("typescript")]
    [InlineData("javascript")]
    [InlineData("python")]
    [InlineData("go")]
    public void Populate_ShouldHandleVariousLanguages(string language)
    {
        // Arrange
        var request = new GenerationRequest("Product", language);
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables["_Language"].Should().Be(language);
    }

    [Fact]
    public void Populate_ShouldSetExactlyThreeVariables()
    {
        // Arrange
        var request = new GenerationRequest("Product", "csharp");
        var context = new VariableContext();

        // Act
        _provider.Populate(context, request);

        // Assert
        var variables = context.AsReadOnly();
        variables.Should().HaveCount(3);
        variables.Should().ContainKey("_EntityName");
        variables.Should().ContainKey("_Language");
        variables.Should().ContainKey("_TemplatePack");
    }
}