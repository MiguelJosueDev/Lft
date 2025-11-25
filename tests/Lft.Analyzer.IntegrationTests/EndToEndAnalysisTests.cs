using Lft.Analyzer.Core;
using Lft.Analyzer.Core.Rules;
using Lft.Ast.CSharp;
using Xunit;

namespace Lft.Analyzer.IntegrationTests;

public class EndToEndAnalysisTests : IDisposable
{
    private readonly string _tempDir;

    public EndToEndAnalysisTests()
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
    public async Task AnalyzeProject_DetectsLayerViolations()
    {
        // 1. Setup a project with a violation: Domain -> Infrastructure
        var projectPath = Path.Combine(_tempDir, "BadProject.csproj");
        var domainFile = Path.Combine(_tempDir, "DomainClass.cs");
        var infraFile = Path.Combine(_tempDir, "InfraClass.cs");

        await File.WriteAllTextAsync(projectPath, @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>");

        // Infra class
        await File.WriteAllTextAsync(infraFile, @"
namespace MyApp.Infrastructure;
public class InfraClass { }
");

        // Domain class depending on Infra
        await File.WriteAllTextAsync(domainFile, @"
using MyApp.Infrastructure;
namespace MyApp.Domain;
public class DomainClass 
{
    public InfraClass Dependency { get; set; }
}
");

        // 2. Load Codebase
        var loader = new CSharpCodebaseLoader();
        CSharpCodebase codebase;
        try
        {
            codebase = await loader.LoadProjectAsync(projectPath);
        }
        catch (Exception ex)
        {
            // Skip if environment issue (no MSBuild)
            // But for this test we want to see it fail if logic is wrong
            throw new Exception($"Load failed: {ex.Message}", ex);
        }

        // 3. Enrich with Layers
        var layerPatterns = new Dictionary<Layer, IReadOnlyList<string>>
        {
            { Layer.Domain, new[] { ".Domain" } },
            { Layer.Infrastructure, new[] { ".Infrastructure" } }
        };

        var enrichedNodes = codebase.ArchNodes.Select(node =>
            node with { Layer = CSharpLayerHelper.InferLayerFromNamespace(node.Namespace, layerPatterns) }
        ).ToList();

        // 4. Run Analysis
        var rules = new[] { new LayerDependencyRule() };
        var engine = new AnalyzerEngine(rules);
        var config = new AnalysisConfiguration();

        var report = await engine.RunAnalysisAsync(enrichedNodes, config);

        // 5. Verify
        Assert.False(report.IsSuccess, "Report should have violations");
        Assert.Contains(report.Violations, v => v.RuleId == "ARCH001");
        Assert.Contains(report.Violations, v => v.Message.Contains("Domain"));
        Assert.Contains(report.Violations, v => v.Message.Contains("Infrastructure"));
    }
}
