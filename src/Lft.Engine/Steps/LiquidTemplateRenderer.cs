using System.Dynamic;
using Fluid;
using Fluid.Values;
using Humanizer;

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

        RegisterHumanizerFilters();
    }

    private void RegisterHumanizerFilters()
    {
        _options.Filters.AddFilter("pascal_case", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Pascalize());
        });

        _options.Filters.AddFilter("camel_case", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Camelize());
        });

        _options.Filters.AddFilter("kebab_case", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Kebaberize());
        });

        _options.Filters.AddFilter("snake_case", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Underscore());
        });

        _options.Filters.AddFilter("pluralize", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Pluralize());
        });

        _options.Filters.AddFilter("singularize", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Singularize());
        });

        _options.Filters.AddFilter("humanize", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Humanize());
        });

        _options.Filters.AddFilter("titleize", (input, args, ctx) =>
        {
            var value = input.ToStringValue();
            return new StringValue(value.Titleize());
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
