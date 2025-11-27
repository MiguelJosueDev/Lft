using Lft.Analyzer.Core;
using Lft.Ast.CSharp.Features.Analysis.Utils;
using Xunit;

namespace Lft.Ast.CSharp.Tests;

public class CSharpLayerHelperTests
{
    [Fact]
    public void InferLayerFromNamespace_MatchesPatterns()
    {
        var patterns = new Dictionary<Layer, IReadOnlyList<string>>
        {
            { Layer.Domain, new[] { ".Domain" } },
            { Layer.Infrastructure, new[] { ".Infrastructure" } }
        };

        Assert.Equal(Layer.Domain, CSharpLayerHelper.InferLayerFromNamespace("MyApp.Domain.Models", patterns));
        Assert.Equal(Layer.Infrastructure, CSharpLayerHelper.InferLayerFromNamespace("MyApp.Infrastructure.Data", patterns));
        Assert.Equal(Layer.Unknown, CSharpLayerHelper.InferLayerFromNamespace("MyApp.Api", patterns));
    }
}
