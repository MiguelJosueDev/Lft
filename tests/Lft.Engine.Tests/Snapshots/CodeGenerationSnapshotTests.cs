using Lft.Domain.Models;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Snapshots;

public class CodeGenerationSnapshotTests
{
    private readonly TemplateCodeGenerationEngine _engine;
    private readonly string _templatesRoot;

    public CodeGenerationSnapshotTests()
    {
        // Get the templates directory relative to test execution
        var testDirectory = AppContext.BaseDirectory;
        _templatesRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", "..", "..", "templates"));

        var packLoader = new TemplatePackLoader(_templatesRoot);
        var variableResolver = new VariableResolver(new IVariableProvider[]
        {
            new CliVariableProvider(),
            new ConventionsVariableProvider()
        });
        var renderer = new LiquidTemplateRenderer();
        var stepExecutor = new StepExecutor(_templatesRoot, renderer);

        _engine = new TemplateCodeGenerationEngine(packLoader, variableResolver, stepExecutor);
    }

    [Fact]
    public async Task GenerateCrud_ForUser_ShouldMatchSnapshot()
    {
        // Arrange
        var request = new GenerationRequest("User", "csharp",
            commandName: "crud",
            templatePack: "main");

        // Act
        var result = await _engine.GenerateAsync(request);

        // Assert - Create a dictionary of filename -> content for cleaner snapshots
        var snapshot = result.Files
            .OrderBy(f => f.Path)
            .ToDictionary(
                f => GetRelativePath(f.Path),
                f => f.Content
            );

        await Verify(snapshot)
            .UseDirectory("Verified")
            .UseFileName("User_Crud");
    }

    [Fact]
    public async Task GenerateCrud_ForPhoneType_ShouldMatchSnapshot()
    {
        // Arrange - PhoneType is our static reference from Artemis accounts-app
        var request = new GenerationRequest("PhoneType", "csharp",
            commandName: "crud",
            templatePack: "main");

        // Act
        var result = await _engine.GenerateAsync(request);

        // Assert
        var snapshot = result.Files
            .OrderBy(f => f.Path)
            .ToDictionary(
                f => GetRelativePath(f.Path),
                f => f.Content
            );

        await Verify(snapshot)
            .UseDirectory("Verified")
            .UseFileName("PhoneType_Crud");
    }

    [Theory]
    [InlineData("Person")]
    [InlineData("Category")]
    [InlineData("Invoice")]
    public async Task GenerateCrud_ForVariousEntities_ShouldMatchSnapshot(string entityName)
    {
        // Arrange
        var request = new GenerationRequest(entityName, "csharp",
            commandName: "crud",
            templatePack: "main");

        // Act
        var result = await _engine.GenerateAsync(request);

        // Assert
        var snapshot = result.Files
            .OrderBy(f => f.Path)
            .ToDictionary(
                f => GetRelativePath(f.Path),
                f => f.Content
            );

        await Verify(snapshot)
            .UseDirectory("Verified")
            .UseParameters(entityName);
    }

    [Fact]
    public async Task GenerateCrud_WithLowercaseInput_ShouldNormalizeAndMatchSnapshot()
    {
        // Arrange - lowercase input should produce same result as PascalCase
        var request = new GenerationRequest("user", "csharp",
            commandName: "crud",
            templatePack: "main");

        // Act
        var result = await _engine.GenerateAsync(request);

        // Assert - Should be identical to User (PascalCase) snapshot
        var snapshot = result.Files
            .OrderBy(f => f.Path)
            .ToDictionary(
                f => GetRelativePath(f.Path),
                f => f.Content
            );

        await Verify(snapshot)
            .UseDirectory("Verified")
            .UseFileName("User_Crud_Lowercase");
    }

    private static string GetRelativePath(string fullPath)
    {
        // Extract just the meaningful part of the path for snapshot keys
        // e.g., "/Users/.../Models/UserModel.cs" -> "Models/UserModel.cs"
        // e.g., "features/phoneTypes/views/index.js" -> "features/phoneTypes/views/index.js"
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Find common folder markers and take path from there
        var markers = new[] { "Models", "Entities", "Repositories", "Services", "Interfaces", "Endpoints", "Routes", "App", "features", "core" };

        for (int i = 0; i < parts.Length; i++)
        {
            if (markers.Contains(parts[i]))
            {
                return string.Join("/", parts.Skip(i));
            }
        }

        // Fallback: return the full relative path (for short paths without markers)
        return fullPath;
    }
}