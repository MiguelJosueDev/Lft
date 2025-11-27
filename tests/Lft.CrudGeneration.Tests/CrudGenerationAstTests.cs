using System.Reflection;
using Lft.Domain.Models;
using Lft.Engine;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Lft.SqlSchema;

namespace Lft.CrudGeneration.Tests;

/// <summary>
/// Integration tests that verify the REAL code generation engine produces code
/// structurally equivalent to the expected PhoneType implementation.
///
/// These tests use:
/// - Real SQL DDL file from lf-database-core (PhoneTypes.sql)
/// - SqlServerSchemaParser + SqlObjectToCrudMapper to build CrudSchemaDefinition
/// - TestVariableProvider to simulate CLI --set flags (isMql, BaseNamespaceName, etc.)
/// - Local fixture files as the expected reference (correct types per SQL schema)
/// </summary>
public class CrudGenerationAstTests
{
    private readonly string _templatesRoot;
    private readonly string _fixturesRoot;
    private readonly string _databaseRoot;
    private readonly TemplateCodeGenerationEngine _engine;

    public CrudGenerationAstTests()
    {
        // Find directories relative to test assembly
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _templatesRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "templates"));

        if (!Directory.Exists(_templatesRoot))
        {
            _templatesRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "templates"));
        }

        // Fixtures root - local reference files with correct types per SQL schema
        _fixturesRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "Fixtures", "PhoneType"));

        // Database root - where SQL DDL files live
        _databaseRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "..", "LIVEFREE", "lf-database-core"));

        // Build engine with TestVariableProvider to simulate CLI --set flags
        var packLoader = new TemplatePackLoader(_templatesRoot);

        // Simulate: lft gen crud --set isMql=true --set BaseNamespaceName=LiveFree.Accounts ...
        var testVariables = new Dictionary<string, object>
        {
            ["isMql"] = true,
            ["BaseNamespaceName"] = "LiveFree.Accounts",
            ["IConnectionFactoryName"] = "IAccountsConnectionFactory",
            ["IUnitOfWorkName"] = "IAccountsUnitOfWork",
            ["RoutePattern"] = "MapModelRoutes"
        };

        var variableProviders = new IVariableProvider[]
        {
            new CliVariableProvider(),
            new TestVariableProvider(testVariables),
            new ConventionsVariableProvider()
        };

        var variableResolver = new VariableResolver(variableProviders);
        var renderer = new LiquidTemplateRenderer();
        var stepExecutor = new StepExecutor(_templatesRoot, renderer);

        _engine = new TemplateCodeGenerationEngine(packLoader, variableResolver, stepExecutor);
    }

    private string LoadFixtureFile(string fileName)
    {
        var fullPath = Path.Combine(_fixturesRoot, fileName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Fixture file not found: {fullPath}");
        }
        return File.ReadAllText(fullPath);
    }

    private CrudSchemaDefinition LoadSchemaFromSql(string relativePath)
    {
        var fullPath = Path.Combine(_databaseRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"SQL file not found: {fullPath}");
        }

        var sql = File.ReadAllText(fullPath);
        var parser = new SqlServerSchemaParser();
        var schema = parser.Parse(sql);
        var mapper = new SqlObjectToCrudMapper();
        return mapper.Map(schema);
    }

    private async Task<GenerationResult> GeneratePhoneTypeCrudAsync()
    {
        // Load schema from real SQL DDL - this extracts:
        // - PhoneTypeID TINYINT IDENTITY → keyType=byte, isPrimaryKey=true
        // - PhoneTypeName NVARCHAR(50) → string
        // - PhoneTypeDesc NVARCHAR(1000) → string?
        var crudSchema = LoadSchemaFromSql("dbo/Tables/PhoneTypes.sql");

        var request = new GenerationRequest(
            "PhoneType",
            "csharp",
            commandName: "crud",
            templatePack: "main",
            outputDirectory: _templatesRoot,
            crudSchemaDefinition: crudSchema);

        return await _engine.GenerateAsync(request);
    }

    private string? FindGeneratedFile(GenerationResult result, string fileName)
    {
        // Use "/" prefix to avoid matching IPhoneTypesService.cs when looking for PhoneTypesService.cs
        var file = result.Files.FirstOrDefault(f =>
            f.Path.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase) ||
            f.Path.EndsWith("\\" + fileName, StringComparison.OrdinalIgnoreCase) ||
            f.Path.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        return file?.Content;
    }

    private bool TestsCanRun()
    {
        return Directory.Exists(_fixturesRoot) && Directory.Exists(_databaseRoot);
    }

    [Fact]
    public async Task Engine_ShouldGenerateModel_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("PhoneTypeModel.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "PhoneTypeModel.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate PhoneTypeModel.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Model should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateEntity_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("PhoneTypeEntity.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "PhoneTypeEntity.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate PhoneTypeEntity.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Entity should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateRepository_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("PhoneTypesRepository.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "PhoneTypesRepository.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate PhoneTypesRepository.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Repository should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateServiceInterface_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("IPhoneTypesService.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "IPhoneTypesService.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate IPhoneTypesService.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Service Interface should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateService_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("PhoneTypesService.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "PhoneTypesService.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate PhoneTypesService.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Service should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateEndpoint_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("PhoneTypesEndpoint.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "PhoneTypesEndpoint.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate PhoneTypesEndpoint.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Endpoint should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateRoutes_MatchingExpected()
    {
        if (!TestsCanRun()) return;

        // Arrange
        var expected = LoadFixtureFile("PhoneTypesRoutes.cs");

        // Act
        var result = await GeneratePhoneTypeCrudAsync();
        var actual = FindGeneratedFile(result, "PhoneTypesRoutes.cs");

        // Assert
        actual.Should().NotBeNull("Engine should generate PhoneTypesRoutes.cs");

        var comparison = CSharpAstComparer.Compare(expected, actual!);
        comparison.IsEquivalent.Should().BeTrue(
            $"Generated Routes should match expected fixture.\n{comparison}");
    }

    [Fact]
    public async Task Engine_ShouldGenerateValidCSharpSyntax()
    {
        if (!TestsCanRun()) return;

        // Act
        var result = await GeneratePhoneTypeCrudAsync();

        // Assert - All generated C# files should have valid syntax
        var csFiles = result.Files.Where(f => f.Path.EndsWith(".cs"));

        foreach (var file in csFiles)
        {
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(file.Content);
            var diagnostics = syntaxTree.GetDiagnostics()
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();

            diagnostics.Should().BeEmpty(
                $"File '{file.Path}' should have valid C# syntax.\n" +
                $"Errors: {string.Join(", ", diagnostics.Select(d => d.GetMessage()))}");
        }
    }
}
