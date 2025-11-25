using System.Text;

namespace Lft.Analyzer.Core;

public static class ReportGenerator
{
    public static string GenerateConsoleReport(AnalysisReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== LFT Architecture Analysis Report ===");
        sb.AppendLine($"Analyzed at: {report.AnalyzedAt}");
        sb.AppendLine($"Status: {(report.IsSuccess ? "PASSED" : "FAILED")}");
        sb.AppendLine();

        if (!report.Violations.Any())
        {
            sb.AppendLine("No violations found. Great job!");
            return sb.ToString();
        }

        foreach (var v in report.Violations)
        {
            sb.AppendLine($"[{v.Severity}] {v.RuleId}: {v.Message}");
            sb.AppendLine($"  at {v.FilePath}");
        }

        return sb.ToString();
    }
}
