using System.Text;
using Lft.Domain.Models;

namespace Lft.Integration;

public class AnchorIntegrationService : IFileIntegrationService
{
    public async Task<FileChangePlan> IntegrateAsync(
        string filePath,
        string newFragment,
        IntegrationOptions options,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return new FileChangePlan(filePath, string.Empty, newFragment, ChangeType.Create);
        }

        var oldContent = await File.ReadAllTextAsync(filePath, ct);

        // 1. Idempotency Check
        if (options.CheckIdempotency)
        {
            // For full file replacements (like AST injections), compare entire content
            if (NormalizeContent(oldContent) == NormalizeContent(newFragment))
            {
                return new FileChangePlan(filePath, oldContent, oldContent, ChangeType.Skip);
            }
        }

        // 2. Strategy Execution
        return options.Strategy switch
        {
            IntegrationStrategy.Anchor => ApplyAnchorStrategy(filePath, oldContent, newFragment, options),
            IntegrationStrategy.Append => new FileChangePlan(filePath, oldContent, oldContent + Environment.NewLine + newFragment, ChangeType.Modify),
            IntegrationStrategy.Prepend => new FileChangePlan(filePath, oldContent, newFragment + Environment.NewLine + oldContent, ChangeType.Modify),
            IntegrationStrategy.Replace => new FileChangePlan(filePath, oldContent, newFragment, ChangeType.Modify),
            _ => throw new NotImplementedException($"Strategy {options.Strategy} not implemented yet.")
        };
    }

    private FileChangePlan ApplyAnchorStrategy(string filePath, string oldContent, string newFragment, IntegrationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AnchorToken))
        {
            // Fallback if no anchor token provided but strategy is Anchor.
            // We could throw, but let's default to Append with a warning, or just return Skip.
            // For safety, let's return Skip and maybe we need a way to signal warnings.
            // For now, let's treat it as "Anchor not found" behavior.
            return new FileChangePlan(filePath, oldContent, oldContent, ChangeType.Skip);
        }

        if (!oldContent.Contains(options.AnchorToken))
        {
            // Anchor not found.
            // Future: Options.OnAnchorMissing (Throw, Append, Skip)
            return new FileChangePlan(filePath, oldContent, oldContent, ChangeType.Skip);
        }

        var sb = new StringBuilder(oldContent);
        var replacement = options.Position == InsertPosition.Before
            ? $"{newFragment}{Environment.NewLine}{options.AnchorToken}"
            : $"{options.AnchorToken}{Environment.NewLine}{newFragment}";

        sb.Replace(options.AnchorToken, replacement);

        return new FileChangePlan(filePath, oldContent, sb.ToString(), ChangeType.Modify);
    }

    private static string NormalizeContent(string content)
    {
        // Normalize line endings and trim for comparison
        return content.Replace("\r\n", "\n").Trim();
    }
}
