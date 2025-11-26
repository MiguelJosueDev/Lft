namespace Lft.Domain.Models;

public sealed class GenerationRequest
{
    public string EntityName { get; }
    public string Language { get; }
    public string? OutputDirectory { get; }
    public string CommandName { get; }
    public string TemplatePack { get; }
    public CrudSchemaDefinition? CrudSchemaDefinition { get; }

    public GenerationRequest(
        string entityName,
        string language,
        string? outputDirectory = null,
        string commandName = "crud",
        string templatePack = "main",
        CrudSchemaDefinition? crudSchemaDefinition = null)
    {
        EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        Language = language ?? throw new ArgumentNullException(nameof(language));
        OutputDirectory = outputDirectory;
        CommandName = commandName;
        TemplatePack = templatePack;
        CrudSchemaDefinition = crudSchemaDefinition;
    }
}
