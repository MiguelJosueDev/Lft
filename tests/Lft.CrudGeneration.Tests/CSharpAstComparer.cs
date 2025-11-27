using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lft.CrudGeneration.Tests;

/// <summary>
/// Compares two C# source files by parsing them into AST and comparing structural elements.
/// This allows comparing code semantically rather than textually, ignoring whitespace and formatting.
/// </summary>
public static class CSharpAstComparer
{
    /// <summary>
    /// Compares two C# source code strings and returns differences.
    /// </summary>
    public static AstComparisonResult Compare(string expected, string actual)
    {
        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var actualTree = CSharpSyntaxTree.ParseText(actual);

        var expectedRoot = expectedTree.GetCompilationUnitRoot();
        var actualRoot = actualTree.GetCompilationUnitRoot();

        var result = new AstComparisonResult();

        // Check for parse errors first
        var expectedErrors = expectedTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var actualErrors = actualTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (expectedErrors.Any())
        {
            result.AddError($"Expected code has parse errors: {string.Join(", ", expectedErrors.Select(e => e.GetMessage()))}");
        }
        if (actualErrors.Any())
        {
            result.AddError($"Actual code has parse errors: {string.Join(", ", actualErrors.Select(e => e.GetMessage()))}");
        }

        // Compare namespaces
        CompareNamespaces(expectedRoot, actualRoot, result);

        // Compare usings
        CompareUsings(expectedRoot, actualRoot, result);

        // Compare type declarations
        CompareTypes(expectedRoot, actualRoot, result);

        return result;
    }

    private static void CompareNamespaces(CompilationUnitSyntax expected, CompilationUnitSyntax actual, AstComparisonResult result)
    {
        var expectedNamespaces = GetNamespaces(expected).ToList();
        var actualNamespaces = GetNamespaces(actual).ToList();

        foreach (var ns in expectedNamespaces.Except(actualNamespaces))
        {
            result.AddMissing($"Namespace: {ns}");
        }

        foreach (var ns in actualNamespaces.Except(expectedNamespaces))
        {
            result.AddExtra($"Namespace: {ns}");
        }
    }

    private static IEnumerable<string> GetNamespaces(CompilationUnitSyntax root)
    {
        // File-scoped namespace
        var fileScopedNs = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString());

        // Block-scoped namespace
        var blockScopedNs = root.Members.OfType<NamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString());

        return fileScopedNs.Concat(blockScopedNs);
    }

    private static void CompareUsings(CompilationUnitSyntax expected, CompilationUnitSyntax actual, AstComparisonResult result)
    {
        var expectedUsings = expected.Usings.Select(u => u.Name?.ToString() ?? "").Where(u => !string.IsNullOrEmpty(u)).ToHashSet();
        var actualUsings = actual.Usings.Select(u => u.Name?.ToString() ?? "").Where(u => !string.IsNullOrEmpty(u)).ToHashSet();

        foreach (var u in expectedUsings.Except(actualUsings))
        {
            result.AddMissing($"Using: {u}");
        }

        foreach (var u in actualUsings.Except(expectedUsings))
        {
            result.AddExtra($"Using: {u}");
        }
    }

    private static void CompareTypes(CompilationUnitSyntax expected, CompilationUnitSyntax actual, AstComparisonResult result)
    {
        var expectedTypes = GetAllTypes(expected).ToList();
        var actualTypes = GetAllTypes(actual).ToList();

        var expectedTypeNames = expectedTypes.Select(t => GetTypeName(t)).ToHashSet();
        var actualTypeNames = actualTypes.Select(t => GetTypeName(t)).ToHashSet();

        // Check for missing/extra types
        foreach (var typeName in expectedTypeNames.Except(actualTypeNames))
        {
            result.AddMissing($"Type: {typeName}");
        }

        foreach (var typeName in actualTypeNames.Except(expectedTypeNames))
        {
            result.AddExtra($"Type: {typeName}");
        }

        // Compare matching types
        foreach (var expectedType in expectedTypes)
        {
            var expectedName = GetTypeName(expectedType);
            var actualType = actualTypes.FirstOrDefault(t => GetTypeName(t) == expectedName);

            if (actualType != null)
            {
                CompareTypeMembers(expectedType, actualType, expectedName, result);
            }
        }
    }

    private static IEnumerable<TypeDeclarationSyntax> GetAllTypes(CompilationUnitSyntax root)
    {
        // Get types from file-scoped namespaces
        var fromFileScopedNs = root.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .SelectMany(ns => ns.Members.OfType<TypeDeclarationSyntax>());

        // Get types from block namespaces
        var fromBlockNs = root.Members
            .OfType<NamespaceDeclarationSyntax>()
            .SelectMany(ns => ns.Members.OfType<TypeDeclarationSyntax>());

        // Get types at root level (no namespace)
        var atRoot = root.Members.OfType<TypeDeclarationSyntax>();

        return fromFileScopedNs.Concat(fromBlockNs).Concat(atRoot);
    }

    private static string GetTypeName(TypeDeclarationSyntax type)
    {
        var kind = type switch
        {
            ClassDeclarationSyntax => "class",
            InterfaceDeclarationSyntax => "interface",
            RecordDeclarationSyntax => "record",
            StructDeclarationSyntax => "struct",
            _ => "type"
        };
        return $"{kind} {type.Identifier.Text}";
    }

    private static void CompareTypeMembers(TypeDeclarationSyntax expected, TypeDeclarationSyntax actual, string typeName, AstComparisonResult result)
    {
        // Compare base types
        var expectedBases = expected.BaseList?.Types.Select(t => t.ToString()).ToHashSet() ?? new HashSet<string>();
        var actualBases = actual.BaseList?.Types.Select(t => t.ToString()).ToHashSet() ?? new HashSet<string>();

        foreach (var b in expectedBases.Except(actualBases))
        {
            result.AddMissing($"Base type '{b}' in {typeName}");
        }

        foreach (var b in actualBases.Except(expectedBases))
        {
            result.AddExtra($"Base type '{b}' in {typeName}");
        }

        // Compare properties
        var expectedProps = expected.Members.OfType<PropertyDeclarationSyntax>()
            .Select(p => $"{p.Type} {p.Identifier}").ToHashSet();
        var actualProps = actual.Members.OfType<PropertyDeclarationSyntax>()
            .Select(p => $"{p.Type} {p.Identifier}").ToHashSet();

        foreach (var p in expectedProps.Except(actualProps))
        {
            result.AddMissing($"Property '{p}' in {typeName}");
        }

        foreach (var p in actualProps.Except(expectedProps))
        {
            result.AddExtra($"Property '{p}' in {typeName}");
        }

        // Compare methods
        var expectedMethods = expected.Members.OfType<MethodDeclarationSyntax>()
            .Select(m => GetMethodSignature(m)).ToHashSet();
        var actualMethods = actual.Members.OfType<MethodDeclarationSyntax>()
            .Select(m => GetMethodSignature(m)).ToHashSet();

        foreach (var m in expectedMethods.Except(actualMethods))
        {
            result.AddMissing($"Method '{m}' in {typeName}");
        }

        foreach (var m in actualMethods.Except(expectedMethods))
        {
            result.AddExtra($"Method '{m}' in {typeName}");
        }

        // Compare constructors (primary constructors are part of the type declaration)
        CompareConstructors(expected, actual, typeName, result);
    }

    private static string GetMethodSignature(MethodDeclarationSyntax method)
    {
        var modifiers = string.Join(" ", method.Modifiers.Select(m => m.Text));
        var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
        return $"{modifiers} {method.ReturnType} {method.Identifier}({parameters})".Trim();
    }

    private static void CompareConstructors(TypeDeclarationSyntax expected, TypeDeclarationSyntax actual, string typeName, AstComparisonResult result)
    {
        // Check primary constructor parameters (C# 12+)
        var expectedParams = expected.ParameterList?.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToList() ?? new List<string>();
        var actualParams = actual.ParameterList?.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToList() ?? new List<string>();

        if (expectedParams.Count != actualParams.Count)
        {
            result.AddDifference($"Constructor parameter count differs in {typeName}: expected {expectedParams.Count}, got {actualParams.Count}");
        }
        else
        {
            for (int i = 0; i < expectedParams.Count; i++)
            {
                if (expectedParams[i] != actualParams[i])
                {
                    result.AddDifference($"Constructor parameter {i} differs in {typeName}: expected '{expectedParams[i]}', got '{actualParams[i]}'");
                }
            }
        }
    }
}

public class AstComparisonResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _missing = new();
    private readonly List<string> _extra = new();
    private readonly List<string> _differences = new();

    public bool IsEquivalent => !_errors.Any() && !_missing.Any() && !_extra.Any() && !_differences.Any();

    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<string> Missing => _missing;
    public IReadOnlyList<string> Extra => _extra;
    public IReadOnlyList<string> Differences => _differences;

    public void AddError(string error) => _errors.Add(error);
    public void AddMissing(string item) => _missing.Add(item);
    public void AddExtra(string item) => _extra.Add(item);
    public void AddDifference(string diff) => _differences.Add(diff);

    public override string ToString()
    {
        if (IsEquivalent) return "Code is structurally equivalent";

        var parts = new List<string>();

        if (_errors.Any())
            parts.Add($"Parse Errors:\n  - {string.Join("\n  - ", _errors)}");

        if (_missing.Any())
            parts.Add($"Missing Elements:\n  - {string.Join("\n  - ", _missing)}");

        if (_extra.Any())
            parts.Add($"Extra Elements:\n  - {string.Join("\n  - ", _extra)}");

        if (_differences.Any())
            parts.Add($"Differences:\n  - {string.Join("\n  - ", _differences)}");

        return string.Join("\n\n", parts);
    }
}
