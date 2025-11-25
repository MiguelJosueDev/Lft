namespace Lft.Domain.Diff;

public enum DiffLineKind
{
    Unchanged,
    Added,
    Removed
}

public sealed record DiffLine(DiffLineKind Kind, string Text);

public sealed record DiffHunk(
    int OldStartLine,
    int OldLineCount,
    int NewStartLine,
    int NewLineCount,
    IReadOnlyList<DiffLine> Lines);

public sealed record FileDiff(string FilePath, IReadOnlyList<DiffHunk> Hunks);

public interface IFileDiffService
{
    FileDiff Compute(string filePath, string oldText, string newText);
}
