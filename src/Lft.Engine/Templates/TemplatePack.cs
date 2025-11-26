namespace Lft.Engine.Templates;

public sealed class TemplatePack
{
    public string Name { get; init; } = "";
    public List<TemplateStep> EntryPoints { get; init; } = new();
}

public sealed class TemplateStep
{
    public string Name { get; init; } = "";
    public string? CommandName { get; init; }   // for entrypoint "crud"
    public string Action { get; init; } = "";   // "group" | "create"
    public string? Def { get; init; }           // def: api-models (definition name from lft.config.json)
    public string? Source { get; init; }        // resources/api/models/model.liquid
    public string? Output { get; init; }        // {{ _ModelName }}Model.cs
    public List<TemplateStep> Steps { get; init; } = new();
}
