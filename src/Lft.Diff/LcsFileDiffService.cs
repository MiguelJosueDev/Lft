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
        var hunks = BuildHunks(diffLines);

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

    private static IReadOnlyList<DiffHunk> BuildHunks(IReadOnlyList<DiffLine> diffLines)
    {
        var hunks = new List<DiffHunk>();
        var oldLineIndex = 1;
        var newLineIndex = 1;
        var position = 0;

        while (position < diffLines.Count)
        {
            var line = diffLines[position];
            if (line.Kind == DiffLineKind.Unchanged)
            {
                oldLineIndex++;
                newLineIndex++;
                position++;
                continue;
            }

            var hunkLines = new List<DiffLine>();
            var hunkOldStart = oldLineIndex;
            var hunkNewStart = newLineIndex;
            var hunkOldCount = 0;
            var hunkNewCount = 0;
            var startPosition = position;

            // Collect all lines in this hunk and count changes
            while (position < diffLines.Count && diffLines[position].Kind != DiffLineKind.Unchanged)
            {
                var current = diffLines[position];
                hunkLines.Add(current);

                if (current.Kind == DiffLineKind.Removed)
                {
                    hunkOldCount++;
                }
                else if (current.Kind == DiffLineKind.Added)
                {
                    hunkNewCount++;
                }

                position++;
            }

            // Update indices after collecting the hunk
            oldLineIndex += hunkOldCount;
            newLineIndex += hunkNewCount;

            hunks.Add(new DiffHunk(hunkOldStart, hunkOldCount, hunkNewStart, hunkNewCount, hunkLines));
        }

        return hunks;
    }
}

public interface IDiffPrinter
{
    string Print(FileDiff diff);
}
