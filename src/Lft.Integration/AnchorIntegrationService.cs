using System.Text;
using Lft.Domain.Models;

namespace Lft.Integration;

public class AnchorIntegrationService : IFileIntegrationService
{
    // Simple strategy: Look for "// LFT-ANCHOR: <NAME>" and insert before or after.
    // For this MVP, let's assume we just append to the end of the file if no anchor is found,
    // OR we look for a specific default anchor if provided.
    // But wait, the Step definition should probably say *where* to insert.
    // For now, let's implement a basic logic:
    // If the file exists, we look for a standard anchor like "// LFT-INSERT-MARKER".
    // If not found, we might just append or warn.
    
    // Actually, the requirement says "Integración en archivos existentes (anchors)".
    // Let's assume the template content itself might contain instructions or we just look for a generic marker.
    // Or better, let's assume the `newFragment` is intended to be inserted into `filePath`.
    
    public async Task<FileChangePlan> IntegrateAsync(string filePath, string newFragment, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            // If file doesn't exist, it's a Create operation
            return new FileChangePlan(filePath, string.Empty, newFragment, ChangeType.Create);
        }

        var oldContent = await File.ReadAllTextAsync(filePath, ct);
        
        // MVP Logic:
        // 1. Check if the newFragment is already present (idempotency check - primitive).
        if (oldContent.Contains(newFragment.Trim()))
        {
             return new FileChangePlan(filePath, oldContent, oldContent, ChangeType.Skip);
        }

        // 2. Look for anchor. Let's hardcode a convention for now or make it configurable later.
        // Convention: "// LFT-ANCHOR: METHODS"
        const string Anchor = "// LFT-ANCHOR: METHODS";
        
        if (oldContent.Contains(Anchor))
        {
            // Insert BEFORE the anchor (or after? usually inside a class, so before the closing brace or at a specific spot).
            // Let's insert BEFORE the anchor line for now.
            var sb = new StringBuilder(oldContent);
            sb.Replace(Anchor, $"{newFragment}\n\n{Anchor}");
            var newContent = sb.ToString();
            
            return new FileChangePlan(filePath, oldContent, newContent, ChangeType.Modify);
        }
        
        // Fallback: If no anchor, maybe append to end? Or just return Skip/Warning?
        // Let's append to end for now to be safe, or maybe just don't touch it.
        // User said "Integración en archivos existentes (anchors)". If no anchor, we can't integrate safely.
        // Let's return Skip with a warning in the plan (maybe we need a 'Warning' field later).
        // For now, let's just Append to end as a fallback.
        
        return new FileChangePlan(filePath, oldContent, oldContent + "\n" + newFragment, ChangeType.Modify);
    }
}
