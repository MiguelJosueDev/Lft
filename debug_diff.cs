using Lft.Diff;

var service = new LcsFileDiffService();

const string oldText = "a\nb\nc\nd";
const string newText = "a\nc\nx\nd";

var diff = service.Compute("file.txt", oldText, newText);

Console.WriteLine("Diff results:");
Console.WriteLine($"Total hunks: {diff.Hunks.Count}");

for (int i = 0; i < diff.Hunks.Count; i++)
{
    var hunk = diff.Hunks[i];
    Console.WriteLine($"\nHunk {i+1}:");
    Console.WriteLine($"  OldStartLine: {hunk.OldStartLine}, OldLineCount: {hunk.OldLineCount}");
    Console.WriteLine($"  NewStartLine: {hunk.NewStartLine}, NewLineCount: {hunk.NewLineCount}");
    Console.WriteLine($"  Lines:");
    foreach (var line in hunk.Lines)
    {
        Console.WriteLine($"    {line.Kind}: {line.Text}");
    }
}

Console.WriteLine("\n\nExpected for hunk 2:");
Console.WriteLine("  OldStartLine: 4, OldLineCount: 0");
Console.WriteLine("  NewStartLine: 4, NewLineCount: 1");
