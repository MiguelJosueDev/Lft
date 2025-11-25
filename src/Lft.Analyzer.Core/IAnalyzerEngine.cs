namespace Lft.Analyzer.Core;

public interface IAnalyzerEngine
{
    Task<AnalysisReport> RunAnalysisAsync(string rootPath, AnalysisConfiguration config);
}
