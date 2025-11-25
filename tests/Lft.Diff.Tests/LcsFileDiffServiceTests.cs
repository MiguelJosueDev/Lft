using FluentAssertions;
using Lft.Diff;
using Lft.Domain.Diff;

namespace Lft.Diff.Tests;

public class LcsFileDiffServiceTests
{
    private readonly LcsFileDiffService _service = new();

    [Fact]
    public void Compute_ShouldReturnNoHunks_WhenTextsAreIdentical()
    {
        const string text = "line1\nline2";

        var diff = _service.Compute("file.txt", text, text);

        diff.Hunks.Should().BeEmpty();
    }

    [Fact]
    public void Compute_ShouldDetectAddedLines()
    {
        const string oldText = "line1\nline3";
        const string newText = "line1\nline2\nline3";

        var diff = _service.Compute("file.txt", oldText, newText);

        diff.Hunks.Should().HaveCount(1);
        var hunk = diff.Hunks[0];
        hunk.OldStartLine.Should().Be(2);
        hunk.OldLineCount.Should().Be(0);
        hunk.NewStartLine.Should().Be(2);
        hunk.NewLineCount.Should().Be(1);
        hunk.Lines.Should().ContainSingle(l => l.Kind == DiffLineKind.Added && l.Text == "line2");
    }

    [Fact]
    public void Compute_ShouldDetectRemovedLines()
    {
        const string oldText = "line1\nline2\nline3";
        const string newText = "line1\nline3";

        var diff = _service.Compute("file.txt", oldText, newText);

        diff.Hunks.Should().HaveCount(1);
        var hunk = diff.Hunks[0];
        hunk.OldStartLine.Should().Be(2);
        hunk.OldLineCount.Should().Be(1);
        hunk.NewStartLine.Should().Be(2);
        hunk.NewLineCount.Should().Be(0);
        hunk.Lines.Should().ContainSingle(l => l.Kind == DiffLineKind.Removed && l.Text == "line2");
    }

    [Fact]
    public void Compute_ShouldDetectMixedChanges()
    {
        const string oldText = "a\nb\nc\nd";
        const string newText = "a\nc\nx\nd";

        var diff = _service.Compute("file.txt", oldText, newText);

        diff.Hunks.Should().HaveCount(2);

        diff.Hunks[0].Should().BeEquivalentTo(new DiffHunk(
            OldStartLine: 2,
            OldLineCount: 1,
            NewStartLine: 2,
            NewLineCount: 0,
            Lines: new List<DiffLine> { new(DiffLineKind.Removed, "b") }
        ));

        diff.Hunks[1].Should().BeEquivalentTo(new DiffHunk(
            OldStartLine: 4,
            OldLineCount: 0,
            NewStartLine: 3,
            NewLineCount: 1,
            Lines: new List<DiffLine> { new(DiffLineKind.Added, "x") }
        ));
    }

    [Fact]
    public void Compute_ShouldHandleEmptyOldText()
    {
        const string oldText = "";
        const string newText = "alpha\nbeta";

        var diff = _service.Compute("file.txt", oldText, newText);

        diff.Hunks.Should().ContainSingle();
        var hunk = diff.Hunks[0];
        hunk.OldStartLine.Should().Be(1);
        hunk.OldLineCount.Should().Be(0);
        hunk.NewStartLine.Should().Be(1);
        hunk.NewLineCount.Should().Be(2);
        hunk.Lines.Should().AllSatisfy(l => l.Kind.Should().Be(DiffLineKind.Added));
    }

    [Fact]
    public void Compute_ShouldHandleEmptyNewText()
    {
        const string oldText = "alpha\nbeta";
        const string newText = "";

        var diff = _service.Compute("file.txt", oldText, newText);

        diff.Hunks.Should().ContainSingle();
        var hunk = diff.Hunks[0];
        hunk.OldStartLine.Should().Be(1);
        hunk.OldLineCount.Should().Be(2);
        hunk.NewStartLine.Should().Be(1);
        hunk.NewLineCount.Should().Be(0);
        hunk.Lines.Should().AllSatisfy(l => l.Kind.Should().Be(DiffLineKind.Removed));
    }
}
