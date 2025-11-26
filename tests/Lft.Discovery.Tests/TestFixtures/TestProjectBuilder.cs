namespace Lft.Discovery.Tests.TestFixtures;

/// <summary>
/// Helper class to build test project structures on disk.
/// </summary>
public sealed class TestProjectBuilder : IDisposable
{
    private readonly string _rootPath;
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _createdDirectories = new();

    public string RootPath => _rootPath;

    public TestProjectBuilder()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "LftDiscoveryTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_rootPath);
        _createdDirectories.Add(_rootPath);
    }

    /// <summary>
    /// Creates a standard Artemis-style multi-layer project structure.
    /// </summary>
    public TestProjectBuilder WithStandardArtemisStructure(string appName, string? namespacePrefix = null)
    {
        var ns = namespacePrefix ?? $"LiveFree.{appName}";

        // API layer
        CreateProject($"api/{ns}.Api", $"{ns}.Api");
        CreateCsFile($"api/{ns}.Api/Extensions/{appName}ServicesExtensions.cs", $$"""
            namespace {{ns}}.Api.Extensions;

            public static class {{appName}}ServicesExtensions
            {
                public static WebApplicationBuilder Add{{appName}}Services(this WebApplicationBuilder builder)
                {
                    var services = builder.Services;
                    // LFT-TOKEN - Services -
                    return builder;
                }
            }
            """);
        CreateCsFile($"api/{ns}.Api/Extensions/{appName}RoutesExtensions.cs", $$"""
            namespace {{ns}}.Api.Extensions;

            public static class {{appName}}RoutesExtensions
            {
                public static WebApplication Add{{appName}}Routes(this WebApplication app, string basePrefix = "{{appName.ToLower()}}")
                {
                    // LFT-TOKEN - Routes -
                    return app;
                }
            }
            """);
        CreateDirectory($"api/{ns}.Api/Endpoints");
        CreateDirectory($"api/{ns}.Api/Routes");

        // Services layer
        CreateProject($"api/{ns}.Services", $"{ns}.Services");
        CreateCsFile($"api/{ns}.Services/Extensions/ServiceRegistrationExtensions.cs", $$"""
            namespace {{ns}}.Services.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection Add{{appName}}Services(this IServiceCollection services)
                {
                    // LFT-TOKEN - Services -
                    return services;
                }
            }
            """);

        // Repositories layer
        CreateProject($"api/{ns}.Repositories.SqlServer", $"{ns}.Repositories.SqlServer");
        CreateCsFile($"api/{ns}.Repositories.SqlServer/Extensions/ServiceRegistrationExtensions.cs", $$"""
            namespace {{ns}}.Repositories.SqlServer.Extensions;

            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection Add{{appName}}Repositories(this IServiceCollection services)
                {
                    // LFT-TOKEN - Repositories -
                    return services;
                }
            }
            """);
        CreateCsFile($"api/{ns}.Repositories.SqlServer/Mappers/{appName}MappingProfile.cs", $$"""
            namespace {{ns}}.Repositories.SqlServer.Mappers;

            public class {{appName}}MappingProfile : Profile
            {
                public {{appName}}MappingProfile()
                {
                    // LFT-TOKEN - Mappings -
                }
            }
            """);
        CreateDirectory($"api/{ns}.Repositories.SqlServer/Entities");

        // Models layer
        CreateProject($"api/{ns}.Models", $"{ns}.Models");

        // Frontend app
        CreateDirectory("app/src/features");
        CreateDirectory("app/src/core/services");

        return this;
    }

    /// <summary>
    /// Creates a project with singular "Extension" suffix (like Cellular).
    /// </summary>
    public TestProjectBuilder WithSingularExtensionNaming(string appName)
    {
        var ns = $"LiveFree.{appName}";

        CreateProject($"api/{ns}.Api", $"{ns}.Api");
        CreateCsFile($"api/{ns}.Api/Extensions/{appName}ServicesExtension.cs", $$"""
            namespace {{ns}}.Api.Extensions;

            public static class {{appName}}ServicesExtension
            {
                public static WebApplicationBuilder Add{{appName}}Services(this WebApplicationBuilder builder)
                {
                    var services = builder.Services;
                    return builder;
                }
            }
            """);
        CreateCsFile($"api/{ns}.Api/Extensions/{appName}RoutesExtension.cs", $$"""
            namespace {{ns}}.Api.Extensions;

            public static class {{appName}}RoutesExtension
            {
                public static WebApplication Add{{appName}}Routes(this WebApplication app)
                {
                    return app;
                }
            }
            """);

        return this;
    }

    /// <summary>
    /// Creates a project with custom namespace prefix (like LiveFree.Artemis.Ticketing).
    /// </summary>
    public TestProjectBuilder WithCustomNamespacePrefix(string appName, string fullPrefix)
    {
        CreateProject($"api/{fullPrefix}.Api", $"{fullPrefix}.Api");
        CreateCsFile($"api/{fullPrefix}.Api/Extensions/{appName}ServicesExtensions.cs", $$"""
            namespace {{fullPrefix}}.Api.Extensions;

            public static class {{appName}}ServicesExtensions
            {
                public static WebApplicationBuilder Add{{appName}}Services(this WebApplicationBuilder builder)
                {
                    return builder;
                }
            }
            """);

        CreateProject($"api/{fullPrefix}.Services", $"{fullPrefix}.Services");
        CreateProject($"api/{fullPrefix}.Models", $"{fullPrefix}.Models");

        return this;
    }

    /// <summary>
    /// Creates a flat structure without api/app subdirectories.
    /// </summary>
    public TestProjectBuilder WithFlatStructure(string appName)
    {
        var ns = $"LiveFree.{appName}";

        CreateProject($"{ns}.Api", $"{ns}.Api");
        CreateCsFile($"{ns}.Api/Extensions/{appName}ServicesExtensions.cs", $$"""
            namespace {{ns}}.Api.Extensions;
            public static class {{appName}}ServicesExtensions { }
            """);

        CreateProject($"{ns}.Services", $"{ns}.Services");
        CreateProject($"{ns}.Models", $"{ns}.Models");

        return this;
    }

    /// <summary>
    /// Creates a minimal single-project structure.
    /// </summary>
    public TestProjectBuilder WithMinimalStructure(string appName)
    {
        var ns = $"LiveFree.{appName}";
        CreateProject($"{ns}", ns);
        CreateCsFile($"{ns}/Program.cs", $$"""
            namespace {{ns}};
            public class Program { }
            """);

        return this;
    }

    /// <summary>
    /// Creates an empty directory structure (no .csproj files).
    /// </summary>
    public TestProjectBuilder WithEmptyStructure()
    {
        CreateDirectory("api");
        CreateDirectory("app");
        return this;
    }

    /// <summary>
    /// Creates a project with multiple repository types (SQL + Providers).
    /// </summary>
    public TestProjectBuilder WithMultipleRepositoryTypes(string appName)
    {
        var ns = $"LiveFree.{appName}";

        CreateProject($"api/{ns}.Repositories.SqlServer", $"{ns}.Repositories.SqlServer");
        CreateCsFile($"api/{ns}.Repositories.SqlServer/Extensions/ServiceRegistrationExtensions.cs", $$"""
            namespace {{ns}}.Repositories.SqlServer.Extensions;
            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection Add{{appName}}RepositoriesSql(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        CreateProject($"api/{ns}.Repositories.Providers", $"{ns}.Repositories.Providers");
        CreateCsFile($"api/{ns}.Repositories.Providers/Extensions/ServiceRegistrationExtensions.cs", $$"""
            namespace {{ns}}.Repositories.Providers.Extensions;
            public static class ServiceRegistrationExtensions
            {
                public static IServiceCollection Add{{appName}}Repositories(this IServiceCollection services)
                {
                    return services;
                }
            }
            """);

        return this;
    }

    /// <summary>
    /// Creates a Functions project alongside the API.
    /// </summary>
    public TestProjectBuilder WithFunctionsProject(string appName)
    {
        var ns = $"LiveFree.{appName}";

        CreateProject($"api/{ns}.Functions", $"{ns}.Functions");
        CreateCsFile($"api/{ns}.Functions/Program.cs", $$"""
            namespace {{ns}}.Functions;
            public class Program { }
            """);

        return this;
    }

    /// <summary>
    /// Creates a Host project alongside the API.
    /// </summary>
    public TestProjectBuilder WithHostProject(string appName)
    {
        var ns = $"LiveFree.{appName}";

        CreateProject($"api/{ns}.Host", $"{ns}.Host");
        CreateCsFile($"api/{ns}.Host/Program.cs", $$"""
            namespace {{ns}}.Host;
            public class Program { }
            """);

        return this;
    }

    /// <summary>
    /// Adds a mapping profile with non-standard naming.
    /// </summary>
    public TestProjectBuilder WithNonStandardMappingProfile(string appName, string profileName)
    {
        var ns = $"LiveFree.{appName}";

        CreateCsFile($"api/{ns}.Repositories.SqlServer/Mappers/{profileName}.cs", $$"""
            namespace {{ns}}.Repositories.SqlServer.Mappers;
            public class {{profileName}} : Profile
            {
                public {{profileName}}() { }
            }
            """);

        return this;
    }

    private void CreateProject(string relativePath, string projectName)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        Directory.CreateDirectory(fullPath);
        _createdDirectories.Add(fullPath);

        var csprojPath = Path.Combine(fullPath, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        _createdFiles.Add(csprojPath);
    }

    private void CreateCsFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _createdDirectories.Add(directory);
        }

        File.WriteAllText(fullPath, content);
        _createdFiles.Add(fullPath);
    }

    private void CreateDirectory(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            _createdDirectories.Add(fullPath);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
