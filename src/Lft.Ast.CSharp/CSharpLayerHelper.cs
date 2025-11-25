using Lft.Analyzer.Core;

namespace Lft.Ast.CSharp;

public static class CSharpLayerHelper
{
    public static Layer InferLayerFromNamespace(
        string @namespace,
        IReadOnlyDictionary<Layer, IReadOnlyList<string>> patterns)
    {
        if (string.IsNullOrEmpty(@namespace)) return Layer.Unknown;

        foreach (var kvp in patterns)
        {
            var layer = kvp.Key;
            var layerPatterns = kvp.Value;

            foreach (var pattern in layerPatterns)
            {
                // Simple contains check for now, can be regex later
                if (@namespace.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return layer;
                }
            }
        }

        return Layer.Unknown;
    }
}
