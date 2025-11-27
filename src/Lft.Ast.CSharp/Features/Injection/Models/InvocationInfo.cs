namespace Lft.Ast.CSharp.Features.Injection.Models;

public sealed record InvocationInfo(string MethodName, List<string> TypeArguments);
