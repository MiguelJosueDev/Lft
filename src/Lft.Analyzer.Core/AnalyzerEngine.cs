using Lft.Analyzer.Core;

namespace Lft.Analyzer.Core;

public class AnalyzerEngine : IAnalyzerEngine
{
    private readonly IEnumerable<IRule> _allRules;

    public AnalyzerEngine(IEnumerable<IRule> allRules)
    {
        _allRules = allRules;
    }

    public async Task<AnalysisReport> RunAnalysisAsync(string rootPath, AnalysisConfiguration config, IProjectModel projectModel)
    {
        // Note: IProjectModel is passed here or created inside? 
        // The interface in the plan was: RunAnalysisAsync(string rootPath, AnalysisConfiguration config);
        // But we need a way to get the IProjectModel. 
        // For now, let's assume we might need a IProjectModelFactory or we pass it in.
        // To keep it simple and match the interface, let's assume we can't pass IProjectModel in the interface method defined previously?
        // Wait, I defined the interface as: Task<AnalysisReport> RunAnalysisAsync(string rootPath, AnalysisConfiguration config);
        // This implies the engine is responsible for creating the model from the path.
        // But for decoupling, maybe we need a IProjectModelLoader.

        // Let's modify the interface to accept IProjectModel OR add a Loader dependency.
        // Given the constraints, let's add a IProjectModelLoader interface and use it.

        throw new NotImplementedException("Need to resolve how ProjectModel is created.");
    }

    // Let's overload for now to allow testing with injected model
    public async Task<AnalysisReport> RunAnalysisAsync(IEnumerable<ArchNode> nodes, AnalysisConfiguration config)
    {
        var report = new AnalysisReport();

        foreach (var rule in _allRules)
        {
            if (config.ExcludedRules.Contains(rule.Id)) continue;
            if (config.IncludedRules.Any() && !config.IncludedRules.Contains(rule.Id)) continue;

            try
            {
                var violations = await rule.EvaluateAsync(nodes);
                foreach (var v in violations)
                {
                    report.AddViolation(v);
                }
            }
            catch (Exception ex)
            {
                // Report internal error as a violation or log it?
                report.AddViolation(new Violation("ANALYZER_ERROR", $"Rule {rule.Id} failed: {ex.Message}", "N/A", 0, "Error"));
            }
        }

        return report;
    }

    public Task<AnalysisReport> RunAnalysisAsync(string rootPath, AnalysisConfiguration config)
    {
        // This would normally use a loader. For this task, we might not implement the loader yet.
        // Let's just throw or return empty for the string-based overload if we don't have a loader.
        throw new NotImplementedException("String-based analysis requires a ProjectModelLoader which is not yet implemented.");
    }
}
