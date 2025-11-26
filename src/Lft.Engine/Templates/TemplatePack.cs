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
    public string Action { get; init; } = "";   // "group" | "create" | "inject" | "ast-insert"
    public string? Source { get; init; }        // resources/api/models/model.liquid
    public string? Output { get; init; }        // {{ _ModelName }}Model.cs
    public List<TemplateStep> Steps { get; init; } = new();

    // Properties for "create" action with discovery
    public string? Layer { get; init; }         // Layer hint: "models" | "services" | "repositories" | "api"

    // Properties for "inject" action (C# AST injection)
    public string? Target { get; init; }        // Discovery target: "ServiceRegistration" | "EndpointRegistration" | etc.
    public string? TargetClass { get; init; }   // Class suffix to find (e.g., "MappingProfile")
    public string? TargetMethod { get; init; }  // Method name to inject into
    public string? Template { get; init; }      // Code snippet to inject (Liquid template)
    public string? Position { get; init; }      // "beginning" | "end" (default: "end")

    // Properties for "ast-insert" action (enhanced AST injection)
    public string? InsertionType { get; init; } // "InMethod" | "InConstructor" | "InClass"
    public bool Idempotent { get; init; }       // Check if code already exists before inserting
    public Dictionary<string, object>? Parameters { get; init; } // Query parameters for finding insertion point
    public bool CreateFile { get; init; }       // Create the target file if it doesn't exist
    public string? CreateFileSource { get; init; } // Template to use when creating the file
    public string? Def { get; init; }           // Definition context (api-module, api-repositories, etc.)
}
