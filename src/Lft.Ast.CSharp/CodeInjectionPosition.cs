namespace Lft.Ast.CSharp;

public enum CodeInjectionPosition
{
    Beginning,
    End
}

/// <summary>
/// Defines semantic patterns for code injection.
/// Used to determine where to insert code within a method body.
/// </summary>
public enum CodeInjectionPattern
{
    /// <summary>Default: Use position-based insertion (Beginning/End)</summary>
    Default,

    /// <summary>Group with services.AddScoped/AddTransient/AddSingleton calls</summary>
    AddScopedBlock,

    /// <summary>Group with CreateMap calls in AutoMapper profiles</summary>
    CreateMapBlock,

    /// <summary>Group with app.Map*Routes calls</summary>
    MapRoutesBlock
}
