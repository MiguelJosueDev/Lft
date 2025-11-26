using Lft.Discovery.Tests.TestFixtures;
using Xunit;

namespace Lft.Discovery.Tests;

public class ProjectAnalyzerTests
{
    private readonly ProjectAnalyzer _sut = new();

    #region Standard Structure Tests

    [Fact]
    public async Task AnalyzeAsync_WithStandardArtemisStructure_DetectsAppNameCorrectly()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("Accounts", manifest.AppName);
        Assert.Equal("LiveFree.Accounts", manifest.BaseNamespace);
    }

    [Fact]
    public async Task AnalyzeAsync_WithStandardArtemisStructure_DetectsAllLayers()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Transactions");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.NotNull(manifest.Api);
        Assert.NotNull(manifest.Services);
        Assert.NotNull(manifest.Repositories);
        Assert.NotNull(manifest.Models);

        Assert.Equal("LiveFree.Transactions.Api", manifest.Api.Namespace);
        Assert.Equal("LiveFree.Transactions.Services", manifest.Services.Namespace);
        Assert.Equal("LiveFree.Transactions.Repositories.SqlServer", manifest.Repositories.Namespace);
        Assert.Equal("LiveFree.Transactions.Models", manifest.Models.Namespace);
    }

    [Fact]
    public async Task AnalyzeAsync_WithStandardArtemisStructure_DetectsExtensionFolders()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.NotNull(manifest.Api?.ExtensionsPath);
        Assert.NotNull(manifest.Api?.EndpointsPath);
        Assert.NotNull(manifest.Api?.RoutesPath);
        Assert.NotNull(manifest.Repositories?.ExtensionsPath);
        Assert.NotNull(manifest.Repositories?.EntitiesPath);
        Assert.NotNull(manifest.Repositories?.MappersPath);
    }

    [Fact]
    public async Task AnalyzeAsync_WithStandardArtemisStructure_DetectsNamingConventions()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("AccountsServicesExtensions", manifest.Conventions.ServiceExtensionClass);
        Assert.Equal("AddAccountsServices", manifest.Conventions.ServiceExtensionMethod);
        Assert.Equal("AccountsRoutesExtensions", manifest.Conventions.RoutesExtensionClass);
        Assert.Equal("AddAccountsRoutes", manifest.Conventions.RoutesExtensionMethod);
        Assert.Equal("AccountsMappingProfile", manifest.Conventions.MappingProfileClass);
        Assert.False(manifest.Conventions.UsesSingularExtension);
    }

    #endregion

    #region Singular Extension Naming Tests

    [Fact]
    public async Task AnalyzeAsync_WithSingularExtensionNaming_DetectsCorrectly()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithSingularExtensionNaming("Cellular");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("CellularServicesExtension", manifest.Conventions.ServiceExtensionClass);
        Assert.Equal("CellularRoutesExtension", manifest.Conventions.RoutesExtensionClass);
        Assert.True(manifest.Conventions.UsesSingularExtension);
    }

    #endregion

    #region Custom Namespace Prefix Tests

    [Fact]
    public async Task AnalyzeAsync_WithCustomNamespacePrefix_DetectsCorrectly()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithCustomNamespacePrefix("Ticketing", "LiveFree.Artemis.Ticketing");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("Ticketing", manifest.AppName);
        Assert.Equal("LiveFree.Artemis.Ticketing", manifest.BaseNamespace);
    }

    #endregion

    #region Flat Structure Tests

    [Fact]
    public async Task AnalyzeAsync_WithFlatStructure_DetectsProjectsAtRoot()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithFlatStructure("Simple");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("Simple", manifest.AppName);
        Assert.NotNull(manifest.Api);
        Assert.NotNull(manifest.Services);
        Assert.NotNull(manifest.Models);
    }

    [Fact]
    public async Task AnalyzeAsync_WithFlatStructure_ApiRootIsProfileRoot()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithFlatStructure("Simple");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal(project.RootPath, manifest.ApiRoot);
    }

    #endregion

    #region Minimal/Edge Case Tests

    [Fact]
    public async Task AnalyzeAsync_WithMinimalStructure_HandlesGracefully()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithMinimalStructure("Minimal");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("Minimal", manifest.AppName);
        // Should have minimal info without crashing
        Assert.NotNull(manifest.Conventions);
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyStructure_ReturnsDefaultValues()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithEmptyStructure();

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        // Should not throw and should return something reasonable
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Conventions);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentPath_ThrowsOrHandlesGracefully()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        // Should handle gracefully - either throw meaningful exception or return empty manifest
        var manifest = await _sut.AnalyzeAsync(nonExistentPath);
        Assert.NotNull(manifest);
    }

    #endregion

    #region Multiple Repository Types Tests

    [Fact]
    public async Task AnalyzeAsync_WithMultipleRepositoryTypes_DetectsFirstOne()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithMultipleRepositoryTypes("Cellular");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.NotNull(manifest.Repositories);
        // Should detect one of the repository layers
        Assert.Contains("Repositories", manifest.Repositories.Namespace);
    }

    #endregion

    #region Functions and Host Projects Tests

    [Fact]
    public async Task AnalyzeAsync_WithFunctionsProject_DetectsFunctions()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts")
            .WithFunctionsProject("Accounts");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.NotNull(manifest.Functions);
        Assert.Equal("LiveFree.Accounts.Functions", manifest.Functions.Namespace);
    }

    [Fact]
    public async Task AnalyzeAsync_WithHostProject_DetectsHost()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Transactions")
            .WithHostProject("Transactions");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.NotNull(manifest.Host);
        Assert.Equal("LiveFree.Transactions.Host", manifest.Host.Namespace);
    }

    #endregion

    #region Non-Standard Naming Tests

    [Fact]
    public async Task AnalyzeAsync_WithNonStandardMappingProfile_DetectsCorrectName()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Transaction") // Note: singular
            .WithNonStandardMappingProfile("Transaction", "TransactionMappingProfile");

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        // Should detect the actual mapping profile name from the file
        Assert.Equal("TransactionMappingProfile", manifest.Conventions.MappingProfileClass);
    }

    #endregion

    #region ToVariables Tests

    [Fact]
    public async Task ToVariables_ReturnsExpectedKeys()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Act
        var variables = manifest.ToVariables();

        // Assert
        Assert.True(variables.ContainsKey("_AppName"));
        Assert.True(variables.ContainsKey("_BaseNamespace"));
        Assert.True(variables.ContainsKey("_ProfileRoot"));
        Assert.True(variables.ContainsKey("_ApiRoot"));
        Assert.True(variables.ContainsKey("_ServiceExtensionClass"));
        Assert.True(variables.ContainsKey("_RoutesExtensionMethod"));
        Assert.True(variables.ContainsKey("_MappingProfileClass"));

        Assert.Equal("Accounts", variables["_AppName"]);
        Assert.Equal("LiveFree.Accounts", variables["_BaseNamespace"]);
    }

    [Fact]
    public async Task ToVariables_IncludesLayerPaths()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Act
        var variables = manifest.ToVariables();

        // Assert
        Assert.True(variables.ContainsKey("_ApiPath"));
        Assert.True(variables.ContainsKey("_ApiNamespace"));
        Assert.True(variables.ContainsKey("_ServicesPath"));
        Assert.True(variables.ContainsKey("_RepositoriesPath"));
        Assert.True(variables.ContainsKey("_ModelsPath"));
    }

    #endregion

    #region GetLayer Tests

    [Fact]
    public async Task GetLayer_ReturnsCorrectLayer()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Act & Assert
        Assert.Same(manifest.Api, manifest.GetLayer(LayerType.Api));
        Assert.Same(manifest.Services, manifest.GetLayer(LayerType.Services));
        Assert.Same(manifest.Repositories, manifest.GetLayer(LayerType.Repositories));
        Assert.Same(manifest.Models, manifest.GetLayer(LayerType.Models));
    }

    [Fact]
    public async Task GetLayer_ReturnsNullForMissingLayer()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithMinimalStructure("Minimal");

        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Act
        var functions = manifest.GetLayer(LayerType.Functions);

        // Assert
        Assert.Null(functions);
    }

    #endregion

    #region Different App Names Tests

    [Theory]
    [InlineData("Accounts")]
    [InlineData("Transactions")]
    [InlineData("Cellular")]
    [InlineData("Ticketing")]
    [InlineData("Payments")]
    public async Task AnalyzeAsync_WithVariousAppNames_DetectsCorrectly(string appName)
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure(appName);

        // Act
        var manifest = await _sut.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal(appName, manifest.AppName);
        Assert.Equal($"LiveFree.{appName}", manifest.BaseNamespace);
        Assert.Equal($"{appName}ServicesExtensions", manifest.Conventions.ServiceExtensionClass);
        Assert.Equal($"Add{appName}Services", manifest.Conventions.ServiceExtensionMethod);
    }

    #endregion
}
