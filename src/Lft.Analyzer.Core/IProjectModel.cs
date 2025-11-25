namespace Lft.Analyzer.Core;

public interface IProjectModel
{
    string RootPath { get; }
    IEnumerable<string> GetFiles(string pattern);
    // Future: GetClasses(), GetReferences(), etc.
}
