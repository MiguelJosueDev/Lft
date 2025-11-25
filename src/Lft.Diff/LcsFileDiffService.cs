using Lft.Domain.Diff;

namespace Lft.Diff;

public sealed class LcsFileDiffService : IFileDiffService
{
    public FileDiff Compute(string filePath, string oldText, string newText)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);

        var lcsTable = BuildLcsTable(oldLines, newLines);
        var diffLines = BacktrackDiff(oldLines, newLines, lcsTable);
        var hunks = BuildHunks(diffLines, contextLines: 1);

        return new FileDiff(filePath, hunks);
    }

    private static IReadOnlyList<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        var normalized = text.Replace("\r\n", "\n");
        return normalized.Split('\n');
    }

    private static int[,] BuildLcsTable(IReadOnlyList<string> oldLines, IReadOnlyList<string> newLines)
    {
        var table = new int[oldLines.Count + 1, newLines.Count + 1];

        for (var i = 1; i <= oldLines.Count; i++)
        {
            for (var j = 1; j <= newLines.Count; j++)
            {
                if (oldLines[i - 1] == newLines[j - 1])
                {
                    table[i, j] = table[i - 1, j - 1] + 1;
                }
                else
                {
                    table[i, j] = Math.Max(table[i - 1, j], table[i, j - 1]);
                }
            }
        }

        return table;
    }

    private static List<DiffLine> BacktrackDiff(
        IReadOnlyList<string> oldLines,
        IReadOnlyList<string> newLines,
        int[,] lcsTable)
    {
        var result = new List<DiffLine>();
        var i = oldLines.Count;
        var j = newLines.Count;

        while (i > 0 && j > 0)
        {
            if (oldLines[i - 1] == newLines[j - 1])
            {
                result.Add(new DiffLine(DiffLineKind.Unchanged, oldLines[i - 1]));
                i--;
                j--;
            }
            else if (lcsTable[i - 1, j] >= lcsTable[i, j - 1])
            {
                result.Add(new DiffLine(DiffLineKind.Removed, oldLines[i - 1]));
                i--;
            }
            else
            {
                result.Add(new DiffLine(DiffLineKind.Added, newLines[j - 1]));
                j--;
            }
        }

        while (i > 0)
        {
            result.Add(new DiffLine(DiffLineKind.Removed, oldLines[i - 1]));
            i--;
        }

        while (j > 0)
        {
            result.Add(new DiffLine(DiffLineKind.Added, newLines[j - 1]));
            j--;
        }

        result.Reverse();
        return result;
    }

    private static IReadOnlyList<DiffHunk> BuildHunks(IReadOnlyList<DiffLine> diffLines, int contextLines)
    {
        var hunks = new List<DiffHunk>();
        var index = 0;

        while (index < diffLines.Count)
        {
            if (diffLines[index].Kind == DiffLineKind.Unchanged)
            {
                index++;
                continue;
            }

            var hunkStart = Math.Max(index - contextLines, 0);
            var hunkEnd = FindHunkEnd(diffLines, index, contextLines);

            var (oldStartLine, newStartLine) = GetLinePositions(diffLines, hunkStart);
            var (oldCount, newCount, hunkLines) = BuildHunkLines(diffLines, hunkStart, hunkEnd);

            hunks.Add(new DiffHunk(oldStartLine, oldCount, newStartLine, newCount, hunkLines));
            index = hunkEnd;
        }

        return hunks;
    }

    private static (int OldCount, int NewCount, List<DiffLine> Lines) BuildHunkLines(
        IReadOnlyList<DiffLine> diffLines,
        int start,
        int end)
    {
        var oldCount = 0;
        var newCount = 0;
        var lines = new List<DiffLine>(end - start);

        for (var i = start; i < end; i++)
        {
            var line = diffLines[i];
            lines.Add(line);

            switch (line.Kind)
            {
                case DiffLineKind.Unchanged:
                    oldCount++;
                    newCount++;
                    break;
                case DiffLineKind.Removed:
                    oldCount++;
                    break;
                case DiffLineKind.Added:
                    newCount++;
                    break;
            }
        }

        return (oldCount, newCount, lines);
    }

    private static int FindHunkEnd(IReadOnlyList<DiffLine> diffLines, int changeIndex, int contextLines)
    {
        var lastChange = changeIndex;

        for (var i = changeIndex + 1; i < diffLines.Count; i++)
        {
            if (diffLines[i].Kind != DiffLineKind.Unchanged)
            {
                lastChange = i;
                continue;
            }

            if (i - lastChange > contextLines)
            {
                return Math.Min(lastChange + contextLines + 1, diffLines.Count);
            }
        }

        return diffLines.Count;
    }

    private static (int OldLine, int NewLine) GetLinePositions(IReadOnlyList<DiffLine> diffLines, int endExclusive)
    {
        var oldLine = 1;
        var newLine = 1;

        for (var i = 0; i < endExclusive; i++)
        {
            switch (diffLines[i].Kind)
            {
                case DiffLineKind.Unchanged:
                    oldLine++;
                    newLine++;
                    break;
                case DiffLineKind.Removed:
                    oldLine++;
                    break;
                case DiffLineKind.Added:
                    newLine++;
                    break;
            }
        }

        return (oldLine, newLine);
    }
}

public interface IDiffPrinter
{
    string Print(FileDiff diff);
}
