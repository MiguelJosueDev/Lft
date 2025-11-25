namespace Lft.Domain.Models;

public sealed class GenerationResult
{
    public IReadOnlyList<GeneratedFile> Files { get; }

    public GenerationResult(IReadOnlyList<GeneratedFile> files)
    {
        Files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public static GenerationResult Empty { get; } = new(Array.Empty<GeneratedFile>());
}
