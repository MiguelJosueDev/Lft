namespace Lft.Integration;

public enum IntegrationStrategy
{
    Anchor,
    Append,
    Prepend,
    Regex
}

public enum InsertPosition
{
    Before,
    After
}

public class IntegrationOptions
{
    public IntegrationStrategy Strategy { get; set; } = IntegrationStrategy.Anchor;

    /// <summary>
    /// The token to look for when using Anchor strategy.
    /// Example: "// LFT-ANCHOR: METHODS"
    /// </summary>
    public string? AnchorToken { get; set; }

    public InsertPosition Position { get; set; } = InsertPosition.Before;

    /// <summary>
    /// If true, will not insert if the content (or a hash of it) is already present.
    /// </summary>
    public bool CheckIdempotency { get; set; } = true;
}
