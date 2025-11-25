using Lft.Analyzer.Core;
using Xunit;

namespace Lft.Analyzer.Tests;

public class AnalyzerEngineTests
{
    class MockRule : IRule
    {
        public string Id { get; }
        public string Description => "Mock Rule";
        private readonly List<Violation> _violations;
        private readonly bool _shouldThrow;

        public MockRule(string id, List<Violation>? violations = null, bool shouldThrow = false)
        {
            Id = id;
            _violations = violations ?? new List<Violation>();
            _shouldThrow = shouldThrow;
        }

        public Task<IEnumerable<Violation>> EvaluateAsync(IEnumerable<ArchNode> nodes)
        {
            if (_shouldThrow) throw new Exception("Rule failed");
            return Task.FromResult<IEnumerable<Violation>>(_violations);
        }
    }

    [Fact]
    public async Task RunAnalysisAsync_ExecutesAllRules_AndAggregatesViolations()
    {
        var rule1 = new MockRule("RULE1", new List<Violation> { new Violation("RULE1", "Fail 1", "file.cs") });
        var rule2 = new MockRule("RULE2", new List<Violation> { new Violation("RULE2", "Fail 2", "file.cs") });

        var engine = new AnalyzerEngine(new[] { rule1, rule2 });
        var config = new AnalysisConfiguration();

        var report = await engine.RunAnalysisAsync(new List<ArchNode>(), config);

        Assert.Equal(2, report.Violations.Count);
        Assert.Contains(report.Violations, v => v.RuleId == "RULE1");
        Assert.Contains(report.Violations, v => v.RuleId == "RULE2");
    }

    [Fact]
    public async Task RunAnalysisAsync_RespectsExcludedRules()
    {
        var rule1 = new MockRule("RULE1", new List<Violation> { new Violation("RULE1", "Fail 1", "file.cs") });
        var rule2 = new MockRule("RULE2", new List<Violation> { new Violation("RULE2", "Fail 2", "file.cs") });

        var engine = new AnalyzerEngine(new[] { rule1, rule2 });
        var config = new AnalysisConfiguration { ExcludedRules = new List<string> { "RULE2" } };

        var report = await engine.RunAnalysisAsync(new List<ArchNode>(), config);

        Assert.Single(report.Violations);
        Assert.Contains(report.Violations, v => v.RuleId == "RULE1");
        Assert.DoesNotContain(report.Violations, v => v.RuleId == "RULE2");
    }

    [Fact]
    public async Task RunAnalysisAsync_HandlesRuleExceptions()
    {
        var rule1 = new MockRule("RULE1", shouldThrow: true);

        var engine = new AnalyzerEngine(new[] { rule1 });
        var config = new AnalysisConfiguration();

        var report = await engine.RunAnalysisAsync(new List<ArchNode>(), config);

        Assert.Single(report.Violations);
        Assert.Equal("ANALYZER_ERROR", report.Violations[0].RuleId);
    }
}
