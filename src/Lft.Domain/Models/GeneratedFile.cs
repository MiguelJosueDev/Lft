namespace Lft.Domain.Models;

public sealed class GeneratedFile
{
    public string Path { get; }
    public string Content { get; }

    /// <summary>
    /// Indicates this file is a modification of an existing file (e.g., AST injection)
    /// rather than a new file creation.
    /// </summary>
    public bool IsModification { get; }

    public GeneratedFile(string path, string content, bool isModification = false)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        IsModification = isModification;
    }
}
