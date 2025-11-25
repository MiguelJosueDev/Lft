namespace Lft.Analyzer.Core;

public class AnalysisReport
{
    public DateTime AnalyzedAt { get; } = DateTime.UtcNow;
    public List<Violation> Violations { get; } = new();
    public bool IsSuccess => !Violations.Any(v => v.Severity == "Error");

    public void AddViolation(Violation violation)
    {
        Violations.Add(violation);
    }
}
