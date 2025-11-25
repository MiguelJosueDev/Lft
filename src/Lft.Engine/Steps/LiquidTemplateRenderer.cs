using System.Dynamic;
using Fluid;
using Fluid.Values;

namespace Lft.Engine.Steps;

public sealed class LiquidTemplateRenderer : ITemplateRenderer
{
    private readonly FluidParser _parser = new();
    private readonly TemplateOptions _options;

    public LiquidTemplateRenderer()
    {
        _options = new TemplateOptions();
        _options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.Default;

        // Register ExpandoObject to allow Liquid to access dynamic properties
        _options.MemberAccessStrategy.Register<ExpandoObject, object?>((obj, name, ctx) =>
        {
            var dict = (IDictionary<string, object?>)obj;
            return dict.TryGetValue(name, out var value) ? value : null;
        });

        // Register value converter for ExpandoObject
        _options.ValueConverters.Add(x =>
        {
            if (x is ExpandoObject expando)
            {
                return new ObjectValue(expando);
            }
            return null;
        });
    }

    public string Render(string templateContent, IReadOnlyDictionary<string, object?> variables)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
            return string.Empty;

        if (!_parser.TryParse(templateContent, out var template, out var error))
        {
            throw new InvalidOperationException($"Failed to parse Liquid template: {error}");
        }

        var context = new TemplateContext(_options);
        foreach (var (key, value) in variables)
        {
            context.SetValue(key, value);
        }

        return template.Render(context);
    }
}
