using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Lft.Discovery;

/// <summary>
/// Locates injection points in a project using glob patterns and Roslyn parsing.
/// </summary>
public sealed partial class InjectionPointLocator : IInjectionPointLocator
{
    private readonly INamespaceResolver _namespaceResolver;

    // Regex for LFT-TOKEN comments
    [GeneratedRegex(@"//\s*LFT-TOKEN\s*-\s*(\w+)\s*-")]
    private static partial Regex LftTokenRegex();

    public InjectionPointLocator(INamespaceResolver? namespaceResolver = null)
    {
        _namespaceResolver = namespaceResolver ?? new NamespaceResolver();
    }

    public async Task<IReadOnlyList<InjectionPoint>> LocateAllAsync(
        string searchRoot,
        CancellationToken ct = default)
    {
        var allPoints = new List<InjectionPoint>();

        foreach (var target in Enum.GetValues<InjectionTarget>())
        {
            var points = await LocateAsync(searchRoot, target, ct);
            allPoints.AddRange(points);
        }

        return allPoints;
    }

    public async Task<IReadOnlyList<InjectionPoint>> LocateAsync(
        string searchRoot,
        InjectionTarget target,
        CancellationToken ct = default)
    {
        var (globPattern, methodPattern, defaultPosition) = GetSearchPatterns(target);
        var results = new List<InjectionPoint>();

        // Find matching files using glob
        var matcher = new Matcher();
        matcher.AddInclude(globPattern);

        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(searchRoot));
        var matchResult = matcher.Execute(directoryInfo);

        foreach (var match in matchResult.Files)
        {
            ct.ThrowIfCancellationRequested();

            var filePath = Path.Combine(searchRoot, match.Path);
            var points = await ParseFileForInjectionPointsAsync(
                filePath, target, methodPattern, defaultPosition, ct);
            results.AddRange(points);
        }

        return results;
    }

    private static (string GlobPattern, string MethodPattern, InjectionPosition DefaultPosition) GetSearchPatterns(
        InjectionTarget target)
    {
        return target switch
        {
            // Pattern matches: *.Services/Extensions/ServiceRegistrationExtensions.cs or .Services/Extensions/...
            InjectionTarget.ServiceRegistration => (
                "**/*.Services/Extensions/ServiceRegistrationExtensions.cs",
                @"Add\w+Services",
                InjectionPosition.End),

            // Pattern matches: *.Api/Extensions/*ServicesExtension*.cs
            InjectionTarget.EndpointRegistration => (
                "**/*.Api/Extensions/*ServicesExtension*.cs",
                @"Add\w+Services",
                InjectionPosition.End),

            // Pattern matches: *.Api/Extensions/*RoutesExtension*.cs
            InjectionTarget.RouteRegistration => (
                "**/*.Api/Extensions/*RoutesExtension*.cs",
                @"Add\w+Routes",
                InjectionPosition.Beginning),

            // Pattern matches: *.Repositories*/Extensions/ServiceRegistrationExtensions.cs
            // Method pattern allows suffixes like "Sql", "Provider", etc. (e.g., AddCellularRepositoriesSql)
            InjectionTarget.RepositoryRegistration => (
                "**/*.Repositories*/Extensions/ServiceRegistrationExtensions.cs",
                @"Add\w+Repositories\w*",
                InjectionPosition.End),

            // Pattern matches: *.Repositories*/Mappers/*MappingProfile.cs
            InjectionTarget.MappingProfile => (
                "**/*.Repositories*/Mappers/*MappingProfile.cs",
                @"\w+MappingProfile",  // Constructor name matches class name
                InjectionPosition.End),

            // Pattern matches: *.Repositories*/Extensions/ServiceRegistrationExtensions.cs
            InjectionTarget.MqlQuery => (
                "**/*.Repositories*/Extensions/ServiceRegistrationExtensions.cs",
                @"AddMqlQueries",
                InjectionPosition.End),

            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
    }

    private async Task<IReadOnlyList<InjectionPoint>> ParseFileForInjectionPointsAsync(
        string filePath,
        InjectionTarget target,
        string methodPattern,
        InjectionPosition defaultPosition,
        CancellationToken ct)
    {
        var results = new List<InjectionPoint>();

        try
        {
            var content = await File.ReadAllTextAsync(filePath, ct);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync(ct);

            // Find classes
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classes)
            {
                var className = classDecl.Identifier.Text;

                // For MappingProfile, look for constructor
                if (target == InjectionTarget.MappingProfile)
                {
                    var constructor = classDecl.Members
                        .OfType<ConstructorDeclarationSyntax>()
                        .FirstOrDefault();

                    if (constructor != null && Regex.IsMatch(className, methodPattern))
                    {
                        var tokenMarker = FindLftToken(content, constructor.SpanStart);
                        results.Add(new InjectionPoint
                        {
                            Target = target,
                            FilePath = filePath,
                            ClassName = className,
                            MethodName = className,  // Constructor name = class name
                            TokenMarker = tokenMarker,
                            DefaultPosition = defaultPosition
                        });
                    }
                    continue;
                }

                // For other targets, look for methods
                var methods = classDecl.Members.OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var methodName = method.Identifier.Text;

                    if (Regex.IsMatch(methodName, methodPattern))
                    {
                        var tokenMarker = FindLftToken(content, method.SpanStart);
                        results.Add(new InjectionPoint
                        {
                            Target = target,
                            FilePath = filePath,
                            ClassName = className,
                            MethodName = methodName,
                            TokenMarker = tokenMarker,
                            DefaultPosition = defaultPosition
                        });
                    }
                }
            }
        }
        catch (Exception)
        {
            // Skip files that can't be parsed
        }

        return results;
    }

    private static string? FindLftToken(string content, int nearPosition)
    {
        // Look for LFT-TOKEN within reasonable distance after the position
        var searchStart = Math.Max(0, nearPosition);
        var searchEnd = Math.Min(content.Length, nearPosition + 2000);
        var searchContent = content[searchStart..searchEnd];

        var match = LftTokenRegex().Match(searchContent);
        return match.Success ? match.Value : null;
    }
}
