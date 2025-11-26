namespace Lft.Discovery;

/// <summary>
/// Types of injection targets in a project.
/// </summary>
public enum InjectionTarget
{
    /// <summary>
    /// Service registration in Services/Extensions/ServiceRegistrationExtensions.cs
    /// Method pattern: Add[App]Services()
    /// </summary>
    ServiceRegistration,

    /// <summary>
    /// Endpoint registration in Api/Extensions/[App]ServicesExtensions.cs
    /// Method pattern: Add[App]Services()
    /// </summary>
    EndpointRegistration,

    /// <summary>
    /// Route registration in Api/Extensions/[App]RoutesExtensions.cs
    /// Method pattern: Add[App]Routes()
    /// </summary>
    RouteRegistration,

    /// <summary>
    /// Repository registration in Repositories/Extensions/ServiceRegistrationExtensions.cs
    /// Method pattern: Add[App]Repositories()
    /// </summary>
    RepositoryRegistration,

    /// <summary>
    /// AutoMapper mapping in Repositories/Mappers/[App]MappingProfile.cs
    /// Target: Constructor
    /// </summary>
    MappingProfile,

    /// <summary>
    /// MQL query registration in Repositories/Extensions/ServiceRegistrationExtensions.cs
    /// Method pattern: AddMqlQueries() lambda
    /// </summary>
    MqlQuery
}
