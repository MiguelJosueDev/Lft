using Lft.Discovery.Tests.TestFixtures;
using Xunit;

namespace Lft.Discovery.Tests.IntegrationTests;

/// <summary>
/// Integration tests that simulate real Artemis project structures.
/// </summary>
public class ArtemisStyleProjectTests
{
    private readonly ProjectAnalyzer _analyzer = new();

    #region Accounts-App Style (Standard)

    [Fact]
    public async Task Accounts_App_Style_FullDiscovery()
    {
        // Arrange - Standard Artemis structure like accounts-app
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Identity
        Assert.Equal("Accounts", manifest.AppName);
        Assert.Equal("LiveFree.Accounts", manifest.BaseNamespace);

        // Assert - All layers present
        Assert.NotNull(manifest.Api);
        Assert.NotNull(manifest.Services);
        Assert.NotNull(manifest.Repositories);
        Assert.NotNull(manifest.Models);

        // Assert - Conventions
        Assert.Equal("AccountsServicesExtensions", manifest.Conventions.ServiceExtensionClass);
        Assert.Equal("AccountsRoutesExtensions", manifest.Conventions.RoutesExtensionClass);
        Assert.Equal("AccountsMappingProfile", manifest.Conventions.MappingProfileClass);
        Assert.False(manifest.Conventions.UsesSingularExtension);

        // Assert - Variables can be generated
        var vars = manifest.ToVariables();
        Assert.Equal("Accounts", vars["_AppName"]);
        Assert.Equal("LiveFree.Accounts", vars["_BaseNamespace"]);
        Assert.NotNull(vars["_ApiPath"]);
    }

    #endregion

    #region Cellular-App Style (Singular Extension)

    [Fact]
    public async Task Cellular_App_Style_SingularExtension()
    {
        // Arrange - Cellular uses singular "Extension" not "Extensions"
        using var project = new TestProjectBuilder()
            .WithSingularExtensionNaming("Cellular");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("Cellular", manifest.AppName);
        Assert.True(manifest.Conventions.UsesSingularExtension);
        Assert.Equal("CellularServicesExtension", manifest.Conventions.ServiceExtensionClass);
        Assert.Equal("CellularRoutesExtension", manifest.Conventions.RoutesExtensionClass);
    }

    [Fact]
    public async Task Cellular_App_Style_MultipleRepositoryProjects()
    {
        // Arrange - Cellular has both SqlServer and Providers repositories
        using var project = new TestProjectBuilder()
            .WithSingularExtensionNaming("Cellular")
            .WithMultipleRepositoryTypes("Cellular");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Should detect at least one repository layer
        Assert.NotNull(manifest.Repositories);
        Assert.Contains("Repositories", manifest.Repositories.Namespace);
    }

    #endregion

    #region Ticketing-App Style (Custom Namespace Prefix)

    [Fact]
    public async Task Ticketing_App_Style_ArtemisPrefix()
    {
        // Arrange - Ticketing uses "LiveFree.Artemis.Ticketing" namespace prefix
        using var project = new TestProjectBuilder()
            .WithCustomNamespacePrefix("Ticketing", "LiveFree.Artemis.Ticketing");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.Equal("Ticketing", manifest.AppName);
        Assert.Equal("LiveFree.Artemis.Ticketing", manifest.BaseNamespace);
    }

    #endregion

    #region Transactions-App Style (With Host and Functions)

    [Fact]
    public async Task Transactions_App_Style_WithHostAndFunctions()
    {
        // Arrange - Transactions has both Host and Functions projects
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Transactions")
            .WithHostProject("Transactions")
            .WithFunctionsProject("Transactions");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert
        Assert.NotNull(manifest.Host);
        Assert.NotNull(manifest.Functions);
        Assert.Equal("LiveFree.Transactions.Host", manifest.Host.Namespace);
        Assert.Equal("LiveFree.Transactions.Functions", manifest.Functions.Namespace);
    }

    [Fact]
    public async Task Transactions_App_Style_NonPluralMappingProfile()
    {
        // Arrange - Transactions uses "TransactionMappingProfile" (singular)
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Transactions")
            .WithNonStandardMappingProfile("Transactions", "TransactionMappingProfile");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Should detect actual file name
        Assert.Equal("TransactionMappingProfile", manifest.Conventions.MappingProfileClass);
    }

    #endregion

    #region Multi-App Scenarios

    [Fact]
    public async Task Multiple_Apps_In_Same_Root_AnalyzesIndependently()
    {
        // Arrange - Two apps in same workspace (shouldn't happen but test isolation)
        using var project1 = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");
        using var project2 = new TestProjectBuilder()
            .WithStandardArtemisStructure("Transactions");

        // Act
        var manifest1 = await _analyzer.AnalyzeAsync(project1.RootPath);
        var manifest2 = await _analyzer.AnalyzeAsync(project2.RootPath);

        // Assert - Each should be detected correctly
        Assert.Equal("Accounts", manifest1.AppName);
        Assert.Equal("Transactions", manifest2.AppName);
        Assert.NotEqual(manifest1.ProfileRoot, manifest2.ProfileRoot);
    }

    #endregion

    #region Frontend Detection

    [Fact]
    public async Task Project_With_Frontend_DetectsAppRoot()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Should detect app/ folder
        Assert.NotNull(manifest.AppRoot);
        Assert.True(manifest.AppRoot.EndsWith("app"));
    }

    #endregion

    #region Injection Points Discovery

    [Fact]
    public async Task Injection_Points_Discovered_For_Standard_Structure()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Should find injection points
        Assert.NotEmpty(manifest.InjectionPoints);

        // Check specific injection points
        var servicePoint = manifest.GetInjectionPoint(InjectionTarget.ServiceRegistration);
        var routePoint = manifest.GetInjectionPoint(InjectionTarget.RouteRegistration);
        var mappingPoint = manifest.GetInjectionPoint(InjectionTarget.MappingProfile);

        Assert.NotNull(servicePoint);
        Assert.NotNull(routePoint);
        Assert.NotNull(mappingPoint);
    }

    [Fact]
    public async Task GetInjectionPoint_Returns_Correct_Target()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Act
        var servicePoint = manifest.GetInjectionPoint(InjectionTarget.ServiceRegistration);

        // Assert
        Assert.NotNull(servicePoint);
        Assert.Equal(InjectionTarget.ServiceRegistration, servicePoint.Target);
        Assert.Equal("ServiceRegistrationExtensions", servicePoint.ClassName);
        Assert.Equal("AddAccountsServices", servicePoint.MethodName);
    }

    #endregion

    #region Error Recovery

    [Fact]
    public async Task Missing_Services_Layer_Still_Works()
    {
        // Arrange - Only API and Models, no Services
        using var project = new TestProjectBuilder()
            .WithFlatStructure("Minimal");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Should not throw, services is null
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Api);
        Assert.Null(manifest.Services?.ExtensionsPath ?? null); // Services exists but maybe no Extensions
    }

    [Fact]
    public async Task Corrupted_Extension_File_Still_Discovers_Others()
    {
        // Arrange
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Accounts");

        // Corrupt one file (but others remain valid)
        var extensionPath = Path.Combine(project.RootPath, "api", "LiveFree.Accounts.Api", "Extensions");
        var corruptedFile = Path.Combine(extensionPath, "CorruptedExtension.cs");
        File.WriteAllText(corruptedFile, "this is not valid C# {{{{");

        // Act
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);

        // Assert - Should still work, ignoring the corrupted file
        Assert.NotNull(manifest);
        Assert.Equal("Accounts", manifest.AppName);
        Assert.NotNull(manifest.Api);
    }

    #endregion

    #region Performance Scenarios

    [Fact]
    public async Task Large_Project_Structure_Completes_In_Reasonable_Time()
    {
        // Arrange - Create a larger project structure
        using var project = new TestProjectBuilder()
            .WithStandardArtemisStructure("Large")
            .WithFunctionsProject("Large")
            .WithHostProject("Large");

        // Add multiple entity folders to simulate a real project
        var repoPath = Path.Combine(project.RootPath, "api", "LiveFree.Large.Repositories.SqlServer");
        for (int i = 0; i < 50; i++)
        {
            var entityPath = Path.Combine(repoPath, "Entities", $"Entity{i}.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(entityPath)!);
            File.WriteAllText(entityPath, $"namespace Test; public class Entity{i} {{}}");
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var manifest = await _analyzer.AnalyzeAsync(project.RootPath);
        sw.Stop();

        // Assert - Should complete in under 5 seconds
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Analysis took too long: {sw.ElapsedMilliseconds}ms");
        Assert.NotNull(manifest);
    }

    #endregion
}
