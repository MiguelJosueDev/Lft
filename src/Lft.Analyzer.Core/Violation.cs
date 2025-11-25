namespace Lft.Analyzer.Core;

public class Violation
{
    public string RuleId { get; }
    public string Message { get; }
    public string FilePath { get; }
    public int LineNumber { get; }
    public string Severity { get; } // Error, Warning, Info

    public Violation(string ruleId, string message, string filePath, int lineNumber = 0, string severity = "Error")
    {
        RuleId = ruleId;
        Message = message;
        FilePath = filePath;
        LineNumber = lineNumber;
        Severity = severity;
    }
}
