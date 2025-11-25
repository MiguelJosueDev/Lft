namespace Lft.Ast.CSharp;

public interface ICSharpCodebaseLoader
{
    Task<CSharpCodebase> LoadSolutionAsync(
        string solutionPath,
        CancellationToken cancellationToken = default);
    
    Task<CSharpCodebase> LoadProjectAsync(
        string projectPath,
        CancellationToken cancellationToken = default);
}

