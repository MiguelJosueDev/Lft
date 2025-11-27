using FluentAssertions;
using Lft.Ast.JavaScript;
using Lft.Domain.Models;
using Lft.Engine.Steps;
using Lft.Engine.Templates;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Integration;

public class JavaScriptStepExecutorIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly StepExecutor _stepExecutor;
    private readonly LiquidTemplateRenderer _renderer;

    public JavaScriptStepExecutorIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LftJsStep_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        _renderer = new LiquidTemplateRenderer();
        _stepExecutor = new StepExecutor(
            _tempDir,
            _renderer,
            pathResolver: null,
            codeInjector: null,
            jsInjectionService: new JavaScriptInjectionService());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task JsAstImport_ShouldInsertImportAndBeIdempotent()
    {
        var routerPath = Path.Combine(_tempDir, "Router.jsx");
        var original = "import React from \"react\";\nimport { Routes } from \"react-router-dom\";\n\nconst routes = [];\n";
        await File.WriteAllTextAsync(routerPath, original);

        var vars = new VariableContext();
        vars.Set("RouterFile", routerPath);
        vars.Set("_ProfileRoot", _tempDir);

        var request = new GenerationRequest("User", "javascript");
        var step = new TemplateStep
        {
            Name = "inject-import",
            Action = "js-ast-import",
            File = "{{ RouterFile }}",
            Import = "import UsersListView from \"../features/users/views/UsersListView\";"
        };

        var result = await _stepExecutor.ExecuteAsync(step, request, vars);

        result.Should().ContainSingle();
        var updatedContent = result.Single().Content;
        updatedContent.Should().Contain("import UsersListView from \"../features/users/views/UsersListView\";");

        await File.WriteAllTextAsync(routerPath, updatedContent);

        var secondRun = await _stepExecutor.ExecuteAsync(step, request, vars);
        secondRun.Should().BeEmpty();
    }

    [Fact]
    public async Task JsAstArrayInsert_ShouldAppendRouteAndBeIdempotent()
    {
        var routesPath = Path.Combine(_tempDir, "routes.js");
        var original = "const routes = [\n  { path: \"/accounts\", element: <AccountsListView /> },\n];\n\nexport default routes;\n";
        await File.WriteAllTextAsync(routesPath, original);

        var vars = new VariableContext();
        vars.Set("RoutesFile", routesPath);
        vars.Set("_ProfileRoot", _tempDir);

        var request = new GenerationRequest("User", "javascript");
        var step = new TemplateStep
        {
            Name = "inject-route",
            Action = "js-ast-array-insert",
            File = "{{ RoutesFile }}",
            Array = "routes",
            Snippet = "{ path: \"/users\", element: <UsersListView /> },"
        };

        var result = await _stepExecutor.ExecuteAsync(step, request, vars);

        result.Should().ContainSingle();
        var updatedContent = result.Single().Content;
        updatedContent.Should().Contain("{ path: \"/users\", element: <UsersListView /> }");

        await File.WriteAllTextAsync(routesPath, updatedContent);

        var secondRun = await _stepExecutor.ExecuteAsync(step, request, vars);
        secondRun.Should().BeEmpty();
    }
}
