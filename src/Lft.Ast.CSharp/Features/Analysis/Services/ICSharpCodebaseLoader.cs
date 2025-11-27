using Lft.Ast.CSharp.Features.Analysis.Models;

namespace Lft.Ast.CSharp.Features.Analysis.Services;

public interface ICSharpCodebaseLoader
{
    Task<CSharpCodebase> LoadSolutionAsync(
        string solutionPath,
        CancellationToken cancellationToken = default);

    Task<CSharpCodebase> LoadProjectAsync(
        string projectPath,
        CancellationToken cancellationToken = default);
}

