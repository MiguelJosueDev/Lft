using FluentAssertions;
using Lft.Ast.JavaScript;

namespace Lft.Ast.JavaScript.Tests;

public class JavaScriptInjectionServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly JavaScriptInjectionService _service;

    public JavaScriptInjectionServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "LftJsTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        _service = new JavaScriptInjectionService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    private string CreateFile(string filename, string content)
    {
        var path = Path.Combine(_testDir, filename);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task InjectImportAsync_ShouldAddImport_WhenFileIsEmpty()
    {
        var file = CreateFile("empty.js", "");
        var import = "import { Foo } from './foo';";

        await _service.InjectImportAsync(file, import);

        var content = await File.ReadAllTextAsync(file);
        content.Trim().Should().Be(import);
    }

    [Fact]
    public async Task InjectImportAsync_ShouldAddImport_AfterLastImport()
    {
        var initial = @"
import { A } from './a';
import { B } from './b';

const x = 1;
";
        var file = CreateFile("imports.js", initial);
        var import = "import { C } from './c';";

        await _service.InjectImportAsync(file, import);

        var content = await File.ReadAllTextAsync(file);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Should be after B and before x
        content.Should().Contain("import { B } from './b';");
        content.Should().Contain("import { C } from './c';");

        var posB = content.IndexOf("import { B } from './b';");
        var posC = content.IndexOf("import { C } from './c';");
        var posX = content.IndexOf("const x = 1;");

        posC.Should().BeGreaterThan(posB);
        posC.Should().BeLessThan(posX);
    }

    [Fact]
    public async Task InjectImportAsync_ShouldNotDuplicateImport()
    {
        var initial = "import { A } from './a';";
        var file = CreateFile("duplicate.js", initial);

        await _service.InjectImportAsync(file, "import { A } from './a';");

        var content = await File.ReadAllTextAsync(file);
        // Should only appear once
        System.Text.RegularExpressions.Regex.Matches(content, "import { A } from './a';").Count.Should().Be(1);
    }

    [Fact]
    public async Task InjectIntoArrayAsync_ShouldAppendToArray_WhenArrayExists()
    {
        var initial = @"
export const routes = [
    { path: '/', component: Home }
];
";
        var file = CreateFile("routes.js", initial);
        var snippet = "{ path: '/users', component: Users }";

        await _service.InjectIntoArrayAsync(file, "routes", snippet);

        var content = await File.ReadAllTextAsync(file);
        content.Should().Contain(snippet);
        // Should be inside the array
        content.Should().MatchRegex(@"(?s)routes\s*=\s*\[.*\{ path: '/users', component: Users \}\s*\]");
    }

    [Fact]
    public async Task InjectIntoArrayAsync_ShouldHandleEmptyArray()
    {
        var initial = "const items = [];";
        var file = CreateFile("empty_array.js", initial);

        await _service.InjectIntoArrayAsync(file, "items", "'item1'");

        var content = await File.ReadAllTextAsync(file);
        content.Should().Contain("'item1'");
        content.Should().Contain("items = [");
        content.Should().Contain("]");
    }

    [Fact]
    public async Task InjectIntoArrayAsync_ShouldThrow_WhenArrayNotFound()
    {
        var file = CreateFile("no_array.js", "const x = 1;");

        Func<Task> act = async () => await _service.InjectIntoArrayAsync(file, "missing", "foo");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Array 'missing' not found*");
    }
}
