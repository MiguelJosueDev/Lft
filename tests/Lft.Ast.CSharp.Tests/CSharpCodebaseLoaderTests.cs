using Lft.Ast.CSharp;
using Xunit;

namespace Lft.Ast.CSharp.Tests;

public class CSharpCodebaseLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public CSharpCodebaseLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task LoadProjectAsync_LoadsProjectAndExtractsTypes()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDir, "TestProject.csproj");
        var codePath = Path.Combine(_tempDir, "MyClass.cs");
        
        // Create a minimal valid csproj
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";
        
        var codeContent = @"
namespace MyNamespace;

public class MyClass
{
    public void MyMethod() { }
}";

        await File.WriteAllTextAsync(projectPath, csprojContent);
        await File.WriteAllTextAsync(codePath, codeContent);

        var loader = new CSharpCodebaseLoader();

        // Act
        // Note: MSBuildWorkspace requires MSBuild to be installed. 
        // In some CI/Test environments this might fail if full SDK isn't discoverable.
        // We'll try-catch to provide a meaningful skip or error.
        try 
        {
            var codebase = await loader.LoadProjectAsync(projectPath);

            // Assert
            Assert.Single(codebase.Projects);
            Assert.Contains(codebase.ArchNodes, node => node.Name == "MyClass" && node.Namespace == "MyNamespace");
        }
        catch (Exception ex)
        {
            // If MSBuild fails to load, we might be in an environment issue.
            // For this specific test run, we'll log it but maybe not fail if it's purely environment.
            // But ideally we want it to pass.
            throw new Exception($"Project load failed: {ex.Message}", ex);
        }
    }
}
