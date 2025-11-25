using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lft.Engine.Templates;

public sealed class TemplatePackLoader
{
    private readonly string _templatesRoot;
    private readonly IDeserializer _deserializer;
    private static readonly Regex IncludePattern = new(@"^\s*-\s*!include\s+(.+\.yml)\s*$", RegexOptions.Multiline);

    public TemplatePackLoader(string templatesRoot)
    {
        _templatesRoot = templatesRoot;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public async Task<TemplatePack> LoadAsync(string packName, CancellationToken ct = default)
    {
        var packPath = Path.Combine(_templatesRoot, packName, "_index.yml");
        if (!File.Exists(packPath))
        {
            throw new FileNotFoundException($"Template pack '{packName}' not found at '{packPath}'");
        }

        var content = await File.ReadAllTextAsync(packPath, ct);

        // Resolve !include directives recursively
        var resolvedContent = await ResolveIncludesAsync(content, Path.GetDirectoryName(packPath)!, ct);

        var pack = _deserializer.Deserialize<TemplatePack>(resolvedContent);

        return pack;
    }

    private async Task<string> ResolveIncludesAsync(string yamlContent, string baseDirectory, CancellationToken ct)
    {
        var matches = IncludePattern.Matches(yamlContent);

        if (matches.Count == 0)
        {
            return yamlContent;
        }

        var result = yamlContent;

        // Process includes in reverse order to maintain correct string positions
        foreach (Match match in matches.Reverse())
        {
            var includePath = match.Groups[1].Value.Trim();
            var fullPath = Path.Combine(baseDirectory, includePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Included file not found: {includePath} (resolved to {fullPath})");
            }

            var includeContent = await File.ReadAllTextAsync(fullPath, ct);

            // Recursively resolve nested includes
            includeContent = await ResolveIncludesAsync(includeContent, Path.GetDirectoryName(fullPath)!, ct);

            // Preserve indentation from the !include line
            var indentation = GetIndentation(match.Value);
            var indentedContent = IndentLines(includeContent, indentation);

            // Replace the !include directive with the file content
            result = result.Remove(match.Index, match.Length).Insert(match.Index, indentedContent);
        }

        return result;
    }

    private static string GetIndentation(string line)
    {
        var match = Regex.Match(line, @"^(\s*)");
        return match.Success ? match.Groups[1].Value : "";
    }

    private static string IndentLines(string content, string indentation)
    {
        if (string.IsNullOrEmpty(indentation))
        {
            return content;
        }

        var lines = content.Split('\n');
        var indentedLines = lines.Select((line, index) =>
        {
            // Don't indent the first line (it replaces the !include line)
            // Don't indent empty lines
            if (index == 0 || string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
            return indentation + line;
        });

        return string.Join('\n', indentedLines);
    }
}
