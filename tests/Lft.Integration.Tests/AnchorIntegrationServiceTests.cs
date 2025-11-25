using Lft.Domain.Models;
using Lft.Integration;
using Xunit;

namespace Lft.Integration.Tests;

public class AnchorIntegrationServiceTests : IDisposable
{
    private readonly AnchorIntegrationService _sut;
    private readonly string _tempFile;

    public AnchorIntegrationServiceTests()
    {
        _sut = new AnchorIntegrationService();
        _tempFile = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public async Task IntegrateAsync_FileDoesNotExist_ReturnsCreatePlan()
    {
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new IntegrationOptions();

        var result = await _sut.IntegrateAsync(nonExistentFile, "content", options);

        Assert.Equal(ChangeType.Create, result.Type);
        Assert.Equal("content", result.NewContent);
    }

    [Fact]
    public async Task IntegrateAsync_IdempotencyCheck_ReturnsSkip_IfContentExists()
    {
        await File.WriteAllTextAsync(_tempFile, "Existing content");
        var options = new IntegrationOptions { CheckIdempotency = true };

        var result = await _sut.IntegrateAsync(_tempFile, "Existing content", options);

        Assert.Equal(ChangeType.Skip, result.Type);
    }

    [Fact]
    public async Task IntegrateAsync_AnchorStrategy_InsertsBeforeAnchor()
    {
        var content = @"
Line 1
// ANCHOR
Line 3";
        await File.WriteAllTextAsync(_tempFile, content);

        var options = new IntegrationOptions
        {
            Strategy = IntegrationStrategy.Anchor,
            AnchorToken = "// ANCHOR",
            Position = InsertPosition.Before
        };

        var result = await _sut.IntegrateAsync(_tempFile, "New Line", options);

        Assert.Equal(ChangeType.Modify, result.Type);
        Assert.Contains("New Line" + Environment.NewLine + "// ANCHOR", result.NewContent);
    }

    [Fact]
    public async Task IntegrateAsync_AnchorStrategy_InsertsAfterAnchor()
    {
        var content = @"
Line 1
// ANCHOR
Line 3";
        await File.WriteAllTextAsync(_tempFile, content);

        var options = new IntegrationOptions
        {
            Strategy = IntegrationStrategy.Anchor,
            AnchorToken = "// ANCHOR",
            Position = InsertPosition.After
        };

        var result = await _sut.IntegrateAsync(_tempFile, "New Line", options);

        Assert.Equal(ChangeType.Modify, result.Type);
        Assert.Contains("// ANCHOR" + Environment.NewLine + "New Line", result.NewContent);
    }

    [Fact]
    public async Task IntegrateAsync_AnchorNotFound_ReturnsSkip()
    {
        await File.WriteAllTextAsync(_tempFile, "No anchor here");

        var options = new IntegrationOptions
        {
            Strategy = IntegrationStrategy.Anchor,
            AnchorToken = "// MISSING"
        };

        var result = await _sut.IntegrateAsync(_tempFile, "New Line", options);

        Assert.Equal(ChangeType.Skip, result.Type);
    }
}
