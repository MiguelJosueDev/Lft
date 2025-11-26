using Lft.Engine.Variables;
using Lft.Domain.Models;

namespace Lft.App.Services;

public class SmartContextVariableProvider : IVariableProvider
{
    private readonly ISmartPathResolver _pathResolver;
    private readonly string _rootDirectory;
    private readonly string? _profileName;

    public SmartContextVariableProvider(ISmartPathResolver pathResolver, string rootDirectory, string? profileName)
    {
        _pathResolver = pathResolver;
        _rootDirectory = rootDirectory;
        _profileName = profileName;
    }

    public void Populate(VariableContext ctx, GenerationRequest request)
    {
        // Define the suffixes we want to look for
        var suffixes = new Dictionary<string, string>
        {
            { "models_namespace", "Model.cs" },
            { "entities_namespace", "Entity.cs" },
            { "repositories_namespace", "Repository.cs" },
            { "services_namespace", "Service.cs" },
            { "controllers_namespace", "Controller.cs" },
            { "endpoints_namespace", "Endpoint.cs" },
            { "mapping_profiles_namespace", "MappingProfile.cs" }
        };

        foreach (var (varName, suffix) in suffixes)
        {
            var resolution = _pathResolver.Resolve(suffix, _rootDirectory, _profileName);
            if (resolution?.Namespace != null)
            {
                ctx.Set(varName, resolution.Namespace);
                // Also infer a base namespace if possible (e.g. from models)
                if (varName == "models_namespace")
                {
                    // If models is "MyApp.Domain.Models", base might be "MyApp.Domain" or "MyApp"
                    // This is heuristic, but useful.
                    // Let's just provide the explicit ones for now.
                }
            }
        }
    }
}
