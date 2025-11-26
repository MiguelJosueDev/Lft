namespace Lft.Ast.CSharp;

public interface ICSharpSyntaxValidator
{
    /// <summary>
    /// Validates the syntax of the provided C# source code.
    /// Returns a list of error messages if any syntax errors are found.
    /// </summary>
    IEnumerable<string> Validate(string sourceCode);
}
