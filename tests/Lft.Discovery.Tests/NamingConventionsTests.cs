using Xunit;

namespace Lft.Discovery.Tests;

public class NamingConventionsTests
{
    #region CreateDefault Tests

    [Fact]
    public void CreateDefault_WithAppName_GeneratesCorrectConventions()
    {
        // Act
        var conventions = NamingConventions.CreateDefault("Accounts");

        // Assert
        Assert.Equal("AccountsServicesExtensions", conventions.ServiceExtensionClass);
        Assert.Equal("AddAccountsServices", conventions.ServiceExtensionMethod);
        Assert.Equal("AccountsRoutesExtensions", conventions.RoutesExtensionClass);
        Assert.Equal("AddAccountsRoutes", conventions.RoutesExtensionMethod);
        Assert.Equal("AddAccountsRepositories", conventions.RepoExtensionMethod);
        Assert.Equal("AccountsMappingProfile", conventions.MappingProfileClass);
        Assert.False(conventions.UsesSingularExtension);
    }

    [Theory]
    [InlineData("Accounts")]
    [InlineData("Transactions")]
    [InlineData("Cellular")]
    [InlineData("Ticketing")]
    [InlineData("Payments")]
    [InlineData("Users")]
    public void CreateDefault_WithVariousAppNames_GeneratesConsistentPattern(string appName)
    {
        // Act
        var conventions = NamingConventions.CreateDefault(appName);

        // Assert
        Assert.Equal($"{appName}ServicesExtensions", conventions.ServiceExtensionClass);
        Assert.Equal($"Add{appName}Services", conventions.ServiceExtensionMethod);
        Assert.Equal($"{appName}RoutesExtensions", conventions.RoutesExtensionClass);
        Assert.Equal($"Add{appName}Routes", conventions.RoutesExtensionMethod);
        Assert.Equal($"Add{appName}Repositories", conventions.RepoExtensionMethod);
        Assert.Equal($"{appName}MappingProfile", conventions.MappingProfileClass);
    }

    #endregion

    #region MappingProfileConstructor Tests

    [Fact]
    public void MappingProfileConstructor_ReturnsSameAsClassName()
    {
        // Arrange
        var conventions = NamingConventions.CreateDefault("Test");

        // Act & Assert
        Assert.Equal(conventions.MappingProfileClass, conventions.MappingProfileConstructor);
    }

    [Fact]
    public void MappingProfileConstructor_WithCustomClassName_ReturnsCustomName()
    {
        // Arrange
        var conventions = new NamingConventions
        {
            ServiceExtensionClass = "TestServicesExtensions",
            ServiceExtensionMethod = "AddTestServices",
            RoutesExtensionClass = "TestRoutesExtensions",
            RoutesExtensionMethod = "AddTestRoutes",
            RepoExtensionMethod = "AddTestRepositories",
            MappingProfileClass = "CustomMappingProfile",
            UsesSingularExtension = false
        };

        // Act
        var constructorName = conventions.MappingProfileConstructor;

        // Assert
        Assert.Equal("CustomMappingProfile", constructorName);
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void Conventions_WithModifier_CreatesNewInstance()
    {
        // Arrange
        var original = NamingConventions.CreateDefault("Test");

        // Act
        var modified = original with { UsesSingularExtension = true };

        // Assert
        Assert.False(original.UsesSingularExtension);
        Assert.True(modified.UsesSingularExtension);
        Assert.NotSame(original, modified);
    }

    [Fact]
    public void Conventions_WithMultipleModifiers_PreservesUnmodifiedValues()
    {
        // Arrange
        var original = NamingConventions.CreateDefault("Test");

        // Act
        var modified = original with
        {
            MappingProfileClass = "CustomProfile",
            UsesSingularExtension = true
        };

        // Assert
        Assert.Equal("TestServicesExtensions", modified.ServiceExtensionClass);
        Assert.Equal("AddTestServices", modified.ServiceExtensionMethod);
        Assert.Equal("CustomProfile", modified.MappingProfileClass);
        Assert.True(modified.UsesSingularExtension);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateDefault_WithEmptyString_GeneratesEmptyPrefixConventions()
    {
        // Act
        var conventions = NamingConventions.CreateDefault("");

        // Assert
        Assert.Equal("ServicesExtensions", conventions.ServiceExtensionClass);
        Assert.Equal("AddServices", conventions.ServiceExtensionMethod);
    }

    [Fact]
    public void CreateDefault_WithSingleCharacter_GeneratesConventions()
    {
        // Act
        var conventions = NamingConventions.CreateDefault("X");

        // Assert
        Assert.Equal("XServicesExtensions", conventions.ServiceExtensionClass);
        Assert.Equal("AddXServices", conventions.ServiceExtensionMethod);
    }

    [Fact]
    public void CreateDefault_WithLongAppName_GeneratesConventions()
    {
        // Arrange
        var longName = "VeryLongApplicationNameForTesting";

        // Act
        var conventions = NamingConventions.CreateDefault(longName);

        // Assert
        Assert.Equal($"{longName}ServicesExtensions", conventions.ServiceExtensionClass);
        Assert.Equal($"Add{longName}Services", conventions.ServiceExtensionMethod);
    }

    #endregion
}
