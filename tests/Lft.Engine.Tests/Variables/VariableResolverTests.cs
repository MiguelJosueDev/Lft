using FluentAssertions;
using Lft.Domain.Models;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Variables;

public class VariableResolverTests
{
    [Fact]
    public void Resolve_ShouldCombineMultipleProviders()
    {
        // Arrange
        var providers = new IVariableProvider[]
        {
            new CliVariableProvider(),
            new ConventionsVariableProvider()
        };
        var resolver = new VariableResolver(providers);
        var request = new GenerationRequest("Product", "csharp");

        // Act
        var context = resolver.Resolve(request);

        // Assert
        var variables = context.AsReadOnly();

        // From CliVariableProvider
        variables["_EntityName"].Should().Be("Product");
        variables["_Language"].Should().Be("csharp");
        variables["_TemplatePack"].Should().Be("main");

        // From ConventionsVariableProvider
        variables["_ModelName"].Should().Be("Product");
        variables["_ModuleName"].Should().Be("Products");
        variables["BaseNamespaceName"].Should().Be("Lft.Generated");
    }

    [Fact]
    public void Resolve_ShouldMaintainVariableCaseSensitivity()
    {
        // Arrange
        var providers = new IVariableProvider[]
        {
            new ConventionsVariableProvider()
        };
        var resolver = new VariableResolver(providers);
        var request = new GenerationRequest("TestEntity", "csharp");

        // Act
        var context = resolver.Resolve(request);

        // Assert
        var variables = context.AsReadOnly();

        // These should be different keys (case-sensitive)
        variables.Should().ContainKey("_ModelName");
        variables["_ModelName"].Should().Be("TestEntity");
    }

    [Fact]
    public void Resolve_WithEmptyProviders_ShouldReturnEmptyContext()
    {
        // Arrange
        var resolver = new VariableResolver(Array.Empty<IVariableProvider>());
        var request = new GenerationRequest("Product", "csharp");

        // Act
        var context = resolver.Resolve(request);

        // Assert
        var variables = context.AsReadOnly();
        variables.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithMultipleProvidersSettingSameKey_LastProviderShouldWin()
    {
        // Arrange
        var providers = new IVariableProvider[]
        {
            new TestProviderA(),
            new TestProviderB()
        };
        var resolver = new VariableResolver(providers);
        var request = new GenerationRequest("Product", "csharp");

        // Act
        var context = resolver.Resolve(request);

        // Assert
        var variables = context.AsReadOnly();
        variables["TestKey"].Should().Be("ValueB"); // TestProviderB wins
    }

    // Test helper providers
    private class TestProviderA : IVariableProvider
    {
        public void Populate(VariableContext ctx, GenerationRequest request)
        {
            ctx.Set("TestKey", "ValueA");
        }
    }

    private class TestProviderB : IVariableProvider
    {
        public void Populate(VariableContext ctx, GenerationRequest request)
        {
            ctx.Set("TestKey", "ValueB");
        }
    }
}