using Xunit;

namespace Lft.Discovery.Tests;

public class InjectionPointLocatorTests : IDisposable
{
    private readonly InjectionPointLocator _sut = new();
    private readonly string _tempDir;

    public InjectionPointLocatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LftInjectionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { }
    }

    #region ServiceRegistration Tests

    [Fact]
    public async Task LocateAsync_ServiceRegistration_FindsExtensionMethod()
    {
        // Arrange
        CreateFile("api/LiveFree.Accounts.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Accounts.Services.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddAccountsServices(this IServiceCollection services)
                {
                    // LFT-TOKEN - Services -
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Single(points);
        var point = points[0];
        Assert.Equal(InjectionTarget.ServiceRegistration, point.Target);
        Assert.Equal("ServiceRegistrationExtensions", point.ClassName);
        Assert.Equal("AddAccountsServices", point.MethodName);
        Assert.Contains("LFT-TOKEN", point.TokenMarker);
        Assert.Equal(InjectionPosition.End, point.DefaultPosition);
    }

    [Fact]
    public async Task LocateAsync_ServiceRegistration_FindsMultipleMethods()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddTestServices(this IServiceCollection services)
                {
                    return services;
                }

                public static IServiceCollection AddTestServicesCore(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Equal(2, points.Count);
        Assert.Contains(points, p => p.MethodName == "AddTestServices");
        Assert.Contains(points, p => p.MethodName == "AddTestServicesCore");
    }

    [Fact]
    public async Task LocateAsync_ServiceRegistration_NoMatchWhenNoAddServicesMethod()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection ConfigureServices(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Empty(points);
    }

    #endregion

    #region EndpointRegistration Tests

    [Fact]
    public async Task LocateAsync_EndpointRegistration_FindsServicesExtensions()
    {
        // Arrange
        CreateFile("api/LiveFree.Accounts.Api/Extensions/AccountsServicesExtensions.cs", """
            namespace LiveFree.Accounts.Api.Extensions;

            public static class AccountsServicesExtensions
            {
                public static WebApplicationBuilder AddAccountsServices(this WebApplicationBuilder builder)
                {
                    var services = builder.Services;
                    // LFT-TOKEN - Endpoints -
                    return builder;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.EndpointRegistration);

        // Assert
        Assert.Single(points);
        var point = points[0];
        Assert.Equal("AccountsServicesExtensions", point.ClassName);
        Assert.Equal("AddAccountsServices", point.MethodName);
    }

    [Fact]
    public async Task LocateAsync_EndpointRegistration_FindsSingularExtension()
    {
        // Arrange
        CreateFile("api/LiveFree.Cellular.Api/Extensions/CellularServicesExtension.cs", """
            namespace LiveFree.Cellular.Api.Extensions;

            public static class CellularServicesExtension
            {
                public static WebApplicationBuilder AddCellularServices(this WebApplicationBuilder builder)
                {
                    return builder;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.EndpointRegistration);

        // Assert
        Assert.Single(points);
        Assert.Equal("CellularServicesExtension", points[0].ClassName);
    }

    #endregion

    #region RouteRegistration Tests

    [Fact]
    public async Task LocateAsync_RouteRegistration_FindsRoutesExtensions()
    {
        // Arrange
        CreateFile("api/LiveFree.Accounts.Api/Extensions/AccountsRoutesExtensions.cs", """
            namespace LiveFree.Accounts.Api.Extensions;

            public static class AccountsRoutesExtensions
            {
                public static WebApplication AddAccountsRoutes(this WebApplication app, string basePrefix = "accounts")
                {
                    // LFT-TOKEN - Routes -
                    return app;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.RouteRegistration);

        // Assert
        Assert.Single(points);
        var point = points[0];
        Assert.Equal("AccountsRoutesExtensions", point.ClassName);
        Assert.Equal("AddAccountsRoutes", point.MethodName);
        Assert.Equal(InjectionPosition.Beginning, point.DefaultPosition);
    }

    [Fact]
    public async Task LocateAsync_RouteRegistration_DefaultPositionIsBeginning()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Api/Extensions/TestRoutesExtensions.cs", """
            namespace LiveFree.Test.Api.Extensions;

            public static class TestRoutesExtensions
            {
                public static WebApplication AddTestRoutes(this WebApplication app)
                {
                    return app;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.RouteRegistration);

        // Assert
        Assert.Single(points);
        Assert.Equal(InjectionPosition.Beginning, points[0].DefaultPosition);
    }

    #endregion

    #region RepositoryRegistration Tests

    [Fact]
    public async Task LocateAsync_RepositoryRegistration_FindsInRepositoriesSqlServer()
    {
        // Arrange
        CreateFile("api/LiveFree.Accounts.Repositories.SqlServer/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Accounts.Repositories.SqlServer.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddAccountsRepositories(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.RepositoryRegistration);

        // Assert
        Assert.Single(points);
        Assert.Equal("AddAccountsRepositories", points[0].MethodName);
    }

    [Fact]
    public async Task LocateAsync_RepositoryRegistration_FindsSqlSuffixMethod()
    {
        // Arrange - Cellular uses AddCellularRepositoriesSql
        CreateFile("api/LiveFree.Cellular.Repositories.SqlServer/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Cellular.Repositories.SqlServer.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddCellularRepositoriesSql(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.RepositoryRegistration);

        // Assert
        // Pattern "Add\w+Repositories" does partial match "AddCellularRepositories" within "AddCellularRepositoriesSql"
        // This allows finding methods with suffixes like "Sql" or "Provider"
        Assert.Single(points);
        Assert.Equal("AddCellularRepositoriesSql", points[0].MethodName);
    }

    #endregion

    #region MappingProfile Tests

    [Fact]
    public async Task LocateAsync_MappingProfile_FindsMappingProfile()
    {
        // Arrange
        CreateFile("api/LiveFree.Accounts.Repositories.SqlServer/Mappers/AccountsMappingProfile.cs", """
            namespace LiveFree.Accounts.Repositories.SqlServer.Mappers;

            public class AccountsMappingProfile : Profile
            {
                public AccountsMappingProfile()
                {
                    // LFT-TOKEN - Mappings -
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.MappingProfile);

        // Assert
        Assert.Single(points);
        var point = points[0];
        Assert.Equal("AccountsMappingProfile", point.ClassName);
        Assert.Equal("AccountsMappingProfile", point.MethodName); // Constructor
    }

    [Fact]
    public async Task LocateAsync_MappingProfile_FindsNonStandardName()
    {
        // Arrange - Some apps use singular "Transaction" not "Transactions"
        CreateFile("api/LiveFree.Transactions.Repositories.SqlServer/Mappers/TransactionMappingProfile.cs", """
            namespace LiveFree.Transactions.Repositories.SqlServer.Mappers;

            public class TransactionMappingProfile : Profile
            {
                public TransactionMappingProfile()
                {
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.MappingProfile);

        // Assert
        Assert.Single(points);
        Assert.Equal("TransactionMappingProfile", points[0].ClassName);
    }

    #endregion

    #region LocateAllAsync Tests

    [Fact]
    public async Task LocateAllAsync_FindsAllTargetTypes()
    {
        // Arrange - Create files for multiple target types
        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;
            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddTestServices(this IServiceCollection s) => s;
            }
            """);

        CreateFile("api/LiveFree.Test.Api/Extensions/TestServicesExtensions.cs", """
            namespace LiveFree.Test.Api.Extensions;
            public static class TestServicesExtensions
            {
                public static WebApplicationBuilder AddTestServices(this WebApplicationBuilder b) => b;
            }
            """);

        CreateFile("api/LiveFree.Test.Api/Extensions/TestRoutesExtensions.cs", """
            namespace LiveFree.Test.Api.Extensions;
            public static class TestRoutesExtensions
            {
                public static WebApplication AddTestRoutes(this WebApplication a) => a;
            }
            """);

        CreateFile("api/LiveFree.Test.Repositories.SqlServer/Mappers/TestMappingProfile.cs", """
            namespace LiveFree.Test.Repositories.SqlServer.Mappers;
            public class TestMappingProfile : Profile
            {
                public TestMappingProfile() { }
            }
            """);

        // Act
        var points = await _sut.LocateAllAsync(_tempDir);

        // Assert
        Assert.True(points.Count >= 4);
        Assert.Contains(points, p => p.Target == InjectionTarget.ServiceRegistration);
        Assert.Contains(points, p => p.Target == InjectionTarget.EndpointRegistration);
        Assert.Contains(points, p => p.Target == InjectionTarget.RouteRegistration);
        Assert.Contains(points, p => p.Target == InjectionTarget.MappingProfile);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task LocateAsync_EmptyDirectory_ReturnsEmpty()
    {
        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public async Task LocateAsync_NonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "NonExistent");

        // Act
        var points = await _sut.LocateAsync(nonExistentPath, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Empty(points);
    }

    [Fact]
    public async Task LocateAsync_MalformedCSharp_HandlesGracefully()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions
            // Malformed - missing braces
            public static class ServiceRegistrationExtensions
                public static IServiceCollection AddTestServices(this IServiceCollection services)
                    return services;
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        // Should not throw, may or may not find points
        Assert.NotNull(points);
    }

    [Fact]
    public async Task LocateAsync_WithCancellation_Cancels()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;
            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddTestServices(this IServiceCollection s) => s;
            }
            """);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration, cts.Token));
    }

    [Fact]
    public async Task LocateAsync_DeepNestedStructure_FindsFiles()
    {
        // Arrange - Deep nesting
        CreateFile("api/deep/nested/path/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;
            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddTestServices(this IServiceCollection s) => s;
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Single(points);
    }

    [Fact]
    public async Task LocateAsync_MultipleFilesInSameFolder_FindsAll()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Api/Extensions/TestServicesExtensions.cs", """
            namespace LiveFree.Test.Api.Extensions;
            public static class TestServicesExtensions
            {
                public static WebApplicationBuilder AddTestServices(this WebApplicationBuilder b) => b;
            }
            """);

        CreateFile("api/LiveFree.Test.Api/Extensions/OtherServicesExtensions.cs", """
            namespace LiveFree.Test.Api.Extensions;
            public static class OtherServicesExtensions
            {
                public static WebApplicationBuilder AddOtherServices(this WebApplicationBuilder b) => b;
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.EndpointRegistration);

        // Assert
        Assert.Equal(2, points.Count);
    }

    #endregion

    #region LFT-TOKEN Detection Tests

    [Fact]
    public async Task LocateAsync_WithLftToken_IncludesTokenInResult()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddTestServices(this IServiceCollection services)
                {
                    services.AddScoped<IFoo, Foo>();
                    // LFT-TOKEN - Services -
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Single(points);
        Assert.NotNull(points[0].TokenMarker);
        Assert.Contains("LFT-TOKEN", points[0].TokenMarker);
    }

    [Fact]
    public async Task LocateAsync_WithoutLftToken_TokenMarkerIsNull()
    {
        // Arrange
        CreateFile("api/LiveFree.Test.Services/Extensions/ServiceRegistrationExtensions.cs", """
            namespace LiveFree.Test.Services.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection AddTestServices(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        // Act
        var points = await _sut.LocateAsync(_tempDir, InjectionTarget.ServiceRegistration);

        // Assert
        Assert.Single(points);
        Assert.Null(points[0].TokenMarker);
    }

    #endregion

    #region Helper Methods

    private void CreateFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content);
    }

    #endregion
}
