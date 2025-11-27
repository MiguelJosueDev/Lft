namespace Lft.Ast.CSharp.Tests;

public class CSharpInjectionServiceTests
{
    private readonly CSharpInjectionService _sut = new();

    #region AddScopedBlock Pattern Tests

    [Fact]
    public void InjectAddScoped_GroupsWithExistingAddScopedCalls_BeforeAddMqlQueries()
    {
        const string source = @"
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAccountsRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPhoneTypesRepository, PhoneTypesRepository>();
        services.AddMqlQueries(typeof(IPhoneTypesRepository).Assembly);
        return services;
    }
}";
        var snippet = "services.AddScoped<INewEntityRepository, NewEntityRepository>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsRepositories",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        // Verify: AddScoped calls are grouped BEFORE AddMqlQueries
        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var addScopedIndex1 = Array.FindIndex(lines, l => l.Contains("IPhoneTypesRepository, PhoneTypesRepository"));
        var addScopedIndex2 = Array.FindIndex(lines, l => l.Contains("INewEntityRepository, NewEntityRepository"));
        var addMqlIndex = Array.FindIndex(lines, l => l.Contains("AddMqlQueries"));

        Assert.True(addScopedIndex1 >= 0, "First AddScoped not found");
        Assert.True(addScopedIndex2 >= 0, "Second AddScoped not found");
        Assert.True(addMqlIndex >= 0, "AddMqlQueries not found");
        Assert.True(addScopedIndex1 < addScopedIndex2, "First AddScoped should come before second");
        Assert.True(addScopedIndex2 < addMqlIndex, "Second AddScoped should come before AddMqlQueries");
    }

    [Fact]
    public void InjectAddScoped_InsertsAfterExistingBlock_WhenMultipleAddScopedExist()
    {
        const string source = @"
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAccountsServices(this IServiceCollection services)
    {
        services.AddScoped<IFirstService, FirstService>();
        services.AddScoped<ISecondService, SecondService>();
        services.AddMqlQueries(typeof(IFirstService).Assembly);
        return services;
    }
}";
        var snippet = "services.AddScoped<IThirdService, ThirdService>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsServices",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var firstIndex = Array.FindIndex(lines, l => l.Contains("IFirstService, FirstService"));
        var secondIndex = Array.FindIndex(lines, l => l.Contains("ISecondService, SecondService"));
        var thirdIndex = Array.FindIndex(lines, l => l.Contains("IThirdService, ThirdService"));
        var mqlIndex = Array.FindIndex(lines, l => l.Contains("AddMqlQueries"));

        Assert.True(firstIndex < secondIndex);
        Assert.True(secondIndex < thirdIndex);
        Assert.True(thirdIndex < mqlIndex);
    }

    [Fact]
    public void InjectAddScoped_InsertsAtBeginning_WhenNoExistingServiceRegistrations()
    {
        const string source = @"
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAccountsRepositories(this IServiceCollection services)
    {
        services.AddMqlQueries(typeof(IPhoneTypesRepository).Assembly);
        return services;
    }
}";
        var snippet = "services.AddScoped<INewRepository, NewRepository>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsRepositories",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var addScopedIndex = Array.FindIndex(lines, l => l.Contains("INewRepository, NewRepository"));
        var mqlIndex = Array.FindIndex(lines, l => l.Contains("AddMqlQueries"));

        Assert.True(addScopedIndex < mqlIndex, "AddScoped should be inserted before AddMqlQueries");
    }

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
        var firstIndex = Array.FindIndex(lines, l => l.Contains("IFirstService"));
        var secondIndex = Array.FindIndex(lines, l => l.Contains("ISecondService"));
        var thirdIndex = Array.FindIndex(lines, l => l.Contains("IThirdService"));
        var configureIndex = Array.FindIndex(lines, l => l.Contains("Configure<Options>"));

        Assert.True(thirdIndex > secondIndex, "New singleton should be after existing services");
        Assert.True(thirdIndex < configureIndex, "New singleton should be before Configure");
    }

    #endregion

    #region CreateMapBlock Pattern Tests

    [Fact]
    public void InjectCreateMap_GroupsWithExistingCreateMapCalls()
    {
        const string source = @"
public class AccountsMappingProfile : Profile
{
    public AccountsMappingProfile()
    {
        CreateMap<PhoneTypeModel, PhoneTypeEntity>().ReverseMap();
    }
}";
        var snippet = "CreateMap<NewModel, NewEntity>().ReverseMap();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "AccountsMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        Assert.Contains("CreateMap<PhoneTypeModel, PhoneTypeEntity>", result);
        Assert.Contains("CreateMap<NewModel, NewEntity>", result);
    }

    [Fact]
    public void InjectCreateMap_InsertsAfterLastCreateMap()
    {
        const string source = @"
public class AccountsMappingProfile : Profile
{
    public AccountsMappingProfile()
    {
        CreateMap<FirstModel, FirstEntity>().ReverseMap();
        CreateMap<SecondModel, SecondEntity>().ReverseMap();
    }
}";
        var snippet = "CreateMap<ThirdModel, ThirdEntity>().ReverseMap();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "AccountsMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
        var firstIndex = Array.FindIndex(lines, l => l.Contains("FirstModel"));
        var secondIndex = Array.FindIndex(lines, l => l.Contains("SecondModel"));
        var thirdIndex = Array.FindIndex(lines, l => l.Contains("ThirdModel"));

        Assert.True(firstIndex < secondIndex);
        Assert.True(secondIndex < thirdIndex);
    }

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

    #endregion

    #region Idempotency Tests

    [Fact]
    public void Idempotency_DoesNotDuplicateExistingCreateMapStatement()
    {
        const string source = @"
public class AccountsMappingProfile : Profile
{
    public AccountsMappingProfile()
    {
        CreateMap<PhoneTypeModel, PhoneTypeEntity>().ReverseMap();
    }
}";
        var snippet = "CreateMap<PhoneTypeModel, PhoneTypeEntity>().ReverseMap();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "MappingProfile",
            methodName: "AccountsMappingProfile",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.CreateMapBlock);

        var createMapCount = result.Split("CreateMap<PhoneTypeModel, PhoneTypeEntity>").Length - 1;
        Assert.Equal(1, createMapCount);
    }

    [Fact]
    public void Idempotency_DoesNotDuplicateExistingAddScopedStatement()
    {
        const string source = @"
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAccountsRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPhoneTypesRepository, PhoneTypesRepository>();
        return services;
    }
}";
        var snippet = "services.AddScoped<IPhoneTypesRepository, PhoneTypesRepository>();";

        var result = _sut.InjectIntoMethodSource(
            source,
            classNameSuffix: "ServiceRegistrationExtensions",
            methodName: "AddAccountsRepositories",
            snippet: snippet,
            position: CodeInjectionPosition.End,
            pattern: CodeInjectionPattern.AddScopedBlock);

        var addScopedCount = result.Split("AddScoped<IPhoneTypesRepository, PhoneTypesRepository>").Length - 1;
        Assert.Equal(1, addScopedCount);
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
