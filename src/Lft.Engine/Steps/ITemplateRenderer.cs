namespace Lft.Engine.Steps;

public interface ITemplateRenderer
{
    string Render(string templateContent, IReadOnlyDictionary<string, object?> variables);
}
