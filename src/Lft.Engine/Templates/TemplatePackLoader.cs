using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lft.Engine.Templates;

public sealed class TemplatePackLoader
{
    private readonly string _templatesRoot;
    private readonly IDeserializer _deserializer;

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
        var pack = _deserializer.Deserialize<TemplatePack>(content);

        // In a real implementation, we would resolve !include directives here recursively.
        // For Sprint 2, we assume a simple structure or just the entry points.

        return pack;
    }
}
