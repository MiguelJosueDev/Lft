namespace Lft.Analyzer.Core;

public interface IRule
{
    string Id { get; }
    string Description { get; }
    Task<IEnumerable<Violation>> EvaluateAsync(IEnumerable<ArchNode> nodes);
}
