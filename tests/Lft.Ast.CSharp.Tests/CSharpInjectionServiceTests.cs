using Lft.Ast.CSharp.Features.Injection.Models;
using Lft.Ast.CSharp.Features.Injection.Services;

namespace Lft.Ast.CSharp.Tests;

public class CSharpInjectionServiceTests
{
    private readonly CSharpInjectionService _sut = new();

    /// <summary>
    /// Helper to load sample files from the Samples folder.
    /// </summary>
    private static string LoadSample(string relativePath)
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "Samples", relativePath);
        return File.ReadAllText(basePath);
    }

    #region Real Sample Tests - Api Layer

    [Fact]
    public void InjectAddScoped_Api_AccountsServicesExtensions_InsertsEndpointAfterLastEndpoint()
    {
        // Real AccountsServicesExtensions.cs from lf-artemis
        var source = LoadSample("Api/AccountsServicesExtensions.cs");
        var snippet = "services.AddScoped<IUsersEndpoint, UsersEndpoint>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "AccountsServicesExtensions",
            methodName: "AddAccountsServices",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var phoneTypesIndex = Array.FindIndex(lines, l => l.Contains("IPhoneTypesEndpoint"));
        var usersIndex = Array.FindIndex(lines, l => l.Contains("IUsersEndpoint"));
        var returnIndex = Array.FindIndex(lines, l => l.StartsWith("return"));

        Assert.True(usersIndex > phoneTypesIndex, "UsersEndpoint should be after PhoneTypesEndpoint");
        Assert.True(usersIndex < returnIndex, "UsersEndpoint should be before return");
    }

    [Fact]
    public void InjectMapRoutes_Api_AccountsRoutesExtensions_InsertsAfterLastRoute()
    {
        // Real AccountsRoutesExtensions.cs from lf-artemis
        var source = LoadSample("Api/AccountsRoutesExtensions.cs");
        var snippet = "app.MapUsersRoutes(basePrefix: basePrefix, prefix: \"users\");";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "AccountsRoutesExtensions",
            methodName: "AddAccountsRoutes",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.MapRoutesBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var phoneTypesIndex = Array.FindIndex(lines, l => l.Contains("MapPhoneTypesRoutes"));
        var usersIndex = Array.FindIndex(lines, l => l.Contains("MapUsersRoutes"));
        var returnIndex = Array.FindIndex(lines, l => l.StartsWith("return"));

        Assert.True(usersIndex > phoneTypesIndex, "MapUsersRoutes should be after MapPhoneTypesRoutes");
        Assert.True(usersIndex < returnIndex, "MapUsersRoutes should be before return");
    }

    #endregion

    #region Real Sample Tests - Services Layer

    [Fact]
    public void InjectAddScoped_Services_ServiceRegistrationExtensions_InsertsAfterLastService()
    {
        // Real Services/ServiceRegistrationExtensions.cs from lf-artemis
        var source = LoadSample("Services/ServiceRegistrationExtensions.cs");
        var snippet = "services.AddScoped<IUsersService, UsersService>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsServices",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var phoneTypesIndex = Array.FindIndex(lines, l => l.Contains("IPhoneTypesService"));
        var usersIndex = Array.FindIndex(lines, l => l.Contains("IUsersService"));
        var returnIndex = Array.FindIndex(lines, l => l.StartsWith("return"));

        Assert.True(usersIndex > phoneTypesIndex, "UsersService should be after PhoneTypesService");
        Assert.True(usersIndex < returnIndex, "UsersService should be before return");
    }

    [Fact]
    public void Idempotency_Services_DoesNotDuplicatePhoneTypesService()
    {
        var source = LoadSample("Services/ServiceRegistrationExtensions.cs");
        var snippet = "services.AddScoped<IPhoneTypesService, PhoneTypesService>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsServices",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var count = result.Split("IPhoneTypesService, PhoneTypesService").Length - 1;
        Assert.Equal(1, count);
    }

    #endregion

    #region Real Sample Tests - Repositories Layer

    [Fact]
    public void InjectAddScoped_Repositories_ServiceRegistrationExtensions_InsertsBeforeAddMqlQueries()
    {
        // Real Repositories/ServiceRegistrationExtensions.cs from lf-artemis
        var source = LoadSample("Repositories/ServiceRegistrationExtensions.cs");
        var snippet = "services.AddScoped<IUsersRepository, UsersRepository>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsRepositories",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var phoneTypesIndex = Array.FindIndex(lines, l => l.Contains("IPhoneTypesRepository, PhoneTypesRepository"));
        var usersIndex = Array.FindIndex(lines, l => l.Contains("IUsersRepository, UsersRepository"));
        var mqlIndex = Array.FindIndex(lines, l => l.Contains("AddMqlQueries"));

        Assert.True(usersIndex > phoneTypesIndex, "UsersRepository should be after PhoneTypesRepository");
        Assert.True(usersIndex < mqlIndex, "UsersRepository should be before AddMqlQueries");
    }

    [Fact]
    public void Idempotency_Repositories_DoesNotDuplicatePhoneTypesRepository()
    {
        var source = LoadSample("Repositories/ServiceRegistrationExtensions.cs");
        var snippet = "services.AddScoped<IPhoneTypesRepository, PhoneTypesRepository>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsRepositories",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var count = result.Split("IPhoneTypesRepository, PhoneTypesRepository").Length - 1;
        Assert.Equal(1, count);
    }

    [Fact]
    public void InjectCreateMap_Repositories_AccountsMappingProfile_InsertsAfterLastCreateMap()
    {
        // Real AccountsMappingProfile.cs from lf-artemis
        var source = LoadSample("Repositories/AccountsMappingProfile.cs");
        var snippet = "CreateMap<UserModel, UserEntity>().ReverseMap();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "AccountsMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        // Verify the snippet was added
        Assert.Contains("CreateMap<UserModel, UserEntity>", result);

        // Verify order: should be after the last CreateMap in constructor
        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var accountModelIndex = Array.FindIndex(lines, l => l.Contains("CreateMap<AccountModel, DealerModel>"));
        var userModelIndex = Array.FindIndex(lines, l => l.Contains("CreateMap<UserModel, UserEntity>"));

        Assert.True(userModelIndex > accountModelIndex, "UserModel mapping should be after last existing mapping");
    }

    [Fact]
    public void Idempotency_Repositories_DoesNotDuplicatePhoneTypeMapping()
    {
        var source = LoadSample("Repositories/AccountsMappingProfile.cs");
        var snippet = "CreateMap<PhoneTypeModel, PhoneTypeEntity>().ReverseMap();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "AccountsMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        var count = result.Split("CreateMap<PhoneTypeModel, PhoneTypeEntity>").Length - 1;
        Assert.Equal(1, count);
    }

    #endregion

    #region AddScopedBlock Pattern Tests (Inline)

    [Fact]
    public void InjectAddTransient_AlsoGroupsWithAddScopedBlock()
    {
        const string source = @"
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IFirstService, FirstService>();
        services.AddTransient<ISecondService, SecondService>();
        services.Configure<Options>(o => { });
        return services;
    }
}";
        var snippet = "services.AddSingleton<IThirdService, ThirdService>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddServices",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var secondIndex = Array.FindIndex(lines, l => l.Contains("ISecondService"));
        var thirdIndex = Array.FindIndex(lines, l => l.Contains("IThirdService"));
        var configureIndex = Array.FindIndex(lines, l => l.Contains("Configure<Options>"));

        Assert.True(thirdIndex > secondIndex, "New singleton should be after existing services");
        Assert.True(thirdIndex < configureIndex, "New singleton should be before Configure");
    }

    #endregion

    #region CreateMapBlock Pattern Tests (Inline)

    [Fact]
    public void InjectCreateMap_HandlesChainedMethods()
    {
        const string source = @"
public class TestMappingProfile : Profile
{
    public TestMappingProfile()
    {
        CreateMap<SourceModel, DestEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Key))
            .ReverseMap();
    }
}";
        var snippet = "CreateMap<NewModel, NewEntity>().ReverseMap();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "TestMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        Assert.Contains("CreateMap<SourceModel, DestEntity>", result);
        Assert.Contains("CreateMap<NewModel, NewEntity>", result);
    }

    [Fact]
    public void Idempotency_MatchesByTypeArgumentsForCreateMap()
    {
        const string source = @"
public class TestMappingProfile : Profile
{
    public TestMappingProfile()
    {
        CreateMap<MyModel, MyEntity>().ReverseMap();
    }
}";
        // Same type arguments but different chained method
        var snippet = "CreateMap<MyModel, MyEntity>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "TestMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        // Should detect as duplicate based on type arguments
        var createMapCount = result.Split("CreateMap<MyModel, MyEntity>").Length - 1;
        Assert.Equal(1, createMapCount);
    }

    #endregion

    #region MapRoutesBlock Pattern Tests (Inline)

    [Fact]
    public void InjectMapRoutes_InsertsBeforeReturn_WhenNoExistingMapRoutes()
    {
        const string source = @"
public static class RoutesExtensions
{
    public static WebApplication AddRoutes(this WebApplication app)
    {
        app.UseHttpsRedirection();
        return app;
    }
}";
        var snippet = "app.MapUsersRoutes();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "RoutesExtensions",
            methodName: "AddRoutes",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.MapRoutesBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var usersIndex = Array.FindIndex(lines, l => l.Contains("MapUsersRoutes"));
        var returnIndex = Array.FindIndex(lines, l => l.StartsWith("return"));

        Assert.True(usersIndex < returnIndex, "MapUsersRoutes should be before return");
    }

    [Fact]
    public void Idempotency_DoesNotDuplicateMapRoutesStatement()
    {
        const string source = @"
public static class RoutesExtensions
{
    public static WebApplication AddRoutes(this WebApplication app)
    {
        app.MapUsersRoutes(basePrefix: ""api"", prefix: ""users"");
        return app;
    }
}";
        var snippet = "app.MapUsersRoutes(basePrefix: \"api\", prefix: \"users\");";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "RoutesExtensions",
            methodName: "AddRoutes",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.MapRoutesBlock);

        var mapRoutesCount = result.Split("MapUsersRoutes").Length - 1;
        Assert.Equal(1, mapRoutesCount);
    }

    #endregion

    #region Default Pattern Tests

    [Fact]
    public void DefaultPattern_InsertsBeforeReturn_WhenPositionIsEnd()
    {
        const string source = @"
public class TestClass
{
    public IServiceCollection Configure(IServiceCollection services)
    {
        services.AddLogging();
        return services;
    }
}";
        var snippet = "services.AddOptions();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "TestClass",
            methodName: "Configure",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.Default);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var addOptionsIndex = Array.FindIndex(lines, l => l.Contains("AddOptions"));
        var returnIndex = Array.FindIndex(lines, l => l.StartsWith("return"));

        Assert.True(addOptionsIndex < returnIndex, "New statement should be before return");
    }

    [Fact]
    public void DefaultPattern_InsertsAfterDeclarations_WhenPositionIsBeginning()
    {
        const string source = @"
public class TestClass
{
    public void Process()
    {
        var config = new Config();
        DoSomething();
    }
}";
        var snippet = "ValidateConfig(config);";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "TestClass",
            methodName: "Process",
            snippet: snippet,
            position: CodeInjectionPosition.Beginning,
            pattern: CodeInjectionPattern.Default);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var declarationIndex = Array.FindIndex(lines, l => l.Contains("var config"));
        var validateIndex = Array.FindIndex(lines, l => l.Contains("ValidateConfig"));
        var doSomethingIndex = Array.FindIndex(lines, l => l.Contains("DoSomething"));

        Assert.True(declarationIndex < validateIndex, "Declaration should be before new statement");
        Assert.True(validateIndex < doSomethingIndex, "New statement should be before DoSomething");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ThrowsWhenClassNotFound()
    {
        const string source = @"
public class SomeOtherClass
{
    public void Method() { }
}";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.InjectIntoMethodSource(
                source,
                classNameSuffix: "NonExistentClass",
                methodName: "Method",
                snippet: "DoSomething();",
                position: CodeInjectionPosition.End));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void ThrowsWhenMethodNotFound()
    {
        const string source = @"
public class TestClass
{
    public void OtherMethod() { }
}";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.InjectIntoMethodSource(
                source,
                classNameSuffix: "TestClass",
                methodName: "NonExistentMethod",
                snippet: "DoSomething();",
                position: CodeInjectionPosition.End));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void WorksWithConstructors()
    {
        const string source = @"
public class TestProfile : Profile
{
    public TestProfile()
    {
        CreateMap<A, B>();
    }
}";
        var snippet = "CreateMap<C, D>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "Profile",
            methodName: "TestProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        Assert.Contains("CreateMap<A, B>", result);
        Assert.Contains("CreateMap<C, D>", result);
    }

    #endregion
}
