namespace Lft.Domain.Models;

public sealed class GeneratedFile
{
    public string Path { get; }
    public string Content { get; }

    public GeneratedFile(string path, string content)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}
