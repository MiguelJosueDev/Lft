using Lft.Analyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Lft.Ast.CSharp.Features.Analysis.Models;
using Lft.Ast.CSharp.Features.Analysis.Utils;

namespace Lft.Ast.CSharp.Features.Analysis.Services;

public class CSharpCodebaseLoader : ICSharpCodebaseLoader
{
    public async Task<CSharpCodebase> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
        return await ProcessSolutionAsync(solution, cancellationToken);
    }

    public async Task<CSharpCodebase> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
        return await ProcessSolutionAsync(project.Solution, cancellationToken);
    }

    private async Task<CSharpCodebase> ProcessSolutionAsync(Solution solution, CancellationToken cancellationToken)
    {
        var projects = new List<CSharpProjectInfo>();
        var allArchNodes = new List<ArchNode>();

        foreach (var project in solution.Projects)
        {
            var (projectInfo, nodes) = await ProcessProjectAsync(project, cancellationToken);
            projects.Add(projectInfo);
            allArchNodes.AddRange(nodes);
        }

        return new CSharpCodebase(projects, allArchNodes);
    }

    private async Task<(CSharpProjectInfo, List<ArchNode>)> ProcessProjectAsync(Project project, CancellationToken cancellationToken)
    {
        var documents = new List<CSharpDocumentInfo>();
        var archNodes = new List<ArchNode>();

        foreach (var document in project.Documents)
        {
            if (document.SourceCodeKind != SourceCodeKind.Regular) continue;

            var (docInfo, nodes) = await ProcessDocumentAsync(document, cancellationToken);
            documents.Add(docInfo);
            archNodes.AddRange(nodes);
        }

        var projectInfo = new CSharpProjectInfo(
            project.Name,
            project.FilePath ?? string.Empty,
            documents
        );

        return (projectInfo, archNodes);
    }

    private async Task<(CSharpDocumentInfo, List<ArchNode>)> ProcessDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        var declaredTypes = new List<string>();
        var archNodes = new List<ArchNode>();

        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

        if (syntaxTree == null || semanticModel == null)
        {
            return (new CSharpDocumentInfo(document.FilePath ?? "", document.Project.Name, declaredTypes), archNodes);
        }

        var root = await syntaxTree.GetRootAsync(cancellationToken);
        var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

        foreach (var typeDecl in typeDeclarations)
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
            if (symbol == null) continue;

            var fullTypeName = symbol.ToDisplayString();
            declaredTypes.Add(fullTypeName);

            var dependencies = ExtractDependencies(typeDecl, semanticModel, cancellationToken);

            var node = new ArchNode(
                Id: fullTypeName,
                Name: symbol.Name,
                Namespace: symbol.ContainingNamespace?.ToDisplayString() ?? "",
                Language: "csharp",
                Layer: Layer.Unknown, // To be inferred later
                DependsOnIds: dependencies,
                Metadata: new Dictionary<string, string>
                {
                    { "FilePath", document.FilePath ?? "" },
                    { "Kind", symbol.TypeKind.ToString() }
                }
            );

            archNodes.Add(node);
        }

        return (new CSharpDocumentInfo(document.FilePath ?? "", document.Project.Name, declaredTypes), archNodes);
    }

    private List<string> ExtractDependencies(TypeDeclarationSyntax typeDecl, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var dependencies = new HashSet<string>();

        // Visit all identifier names in the type declaration
        var nodes = typeDecl.DescendantNodes().OfType<IdentifierNameSyntax>();

        foreach (var node in nodes)
        {
            var symbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

            if (symbol is INamedTypeSymbol typeSymbol)
            {
                // Filter out system types or primitives if desired, but for now keep everything
                // that is not an error type.
                if (typeSymbol.TypeKind != TypeKind.Error)
                {
                    dependencies.Add(typeSymbol.ToDisplayString());
                }
            }
        }

        return dependencies.ToList();
    }
}
