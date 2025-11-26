namespace Lft.Discovery;

/// <summary>
/// Represents a discovered injection point in the project.
/// </summary>
public sealed record InjectionPoint
{
    /// <summary>
    /// The type of injection target.
    /// </summary>
    public required InjectionTarget Target { get; init; }

    /// <summary>
    /// Full path to the file containing the injection point.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Name of the class containing the injection point.
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Name of the method where code should be injected.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Optional LFT-TOKEN marker comment for precise insertion.
    /// Example: "LFT-TOKEN - Services -"
    /// </summary>
    public string? TokenMarker { get; init; }

    /// <summary>
    /// Default position for injection (beginning or end of method).
    /// </summary>
    public InjectionPosition DefaultPosition { get; init; } = InjectionPosition.End;
}

/// <summary>
/// Position within a method body for code injection.
/// </summary>
public enum InjectionPosition
{
    /// <summary>
    /// Insert at the beginning of the method body (after variable declarations).
    /// </summary>
    Beginning,

    /// <summary>
    /// Insert at the end of the method body (before return statement).
    /// </summary>
    End
}
