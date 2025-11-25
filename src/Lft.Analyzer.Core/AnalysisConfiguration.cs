namespace Lft.Analyzer.Core;

public class AnalysisConfiguration
{
    public List<string> IncludedRules { get; set; } = new();
    public List<string> ExcludedRules { get; set; } = new();
    public bool TreatWarningsAsErrors { get; set; }
}
