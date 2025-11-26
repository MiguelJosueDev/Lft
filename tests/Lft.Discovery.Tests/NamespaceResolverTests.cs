using Xunit;

namespace Lft.Discovery.Tests;

public class NamespaceResolverTests : IDisposable
{
    private readonly NamespaceResolver _sut = new();
    private readonly string _tempDir;

    public NamespaceResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LftNamespaceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { }
    }

    #region ResolveFromFile Tests

    [Fact]
    public void ResolveFromFile_WithFileScopedNamespace_ReturnsNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            namespace LiveFree.Accounts.Api;

            public class Test { }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Accounts.Api", result);
    }

    [Fact]
    public void ResolveFromFile_WithBlockNamespace_ReturnsNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            namespace LiveFree.Accounts.Api
            {
                public class Test { }
            }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Accounts.Api", result);
    }

    [Fact]
    public void ResolveFromFile_WithNestedNamespace_ReturnsFirstNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            namespace LiveFree.Accounts
            {
                namespace Api
                {
                    public class Test { }
                }
            }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Accounts", result);
    }

    [Fact]
    public void ResolveFromFile_WithNoNamespace_ReturnsNull()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            public class Test { }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromFile_WithEmptyFile_ReturnsNull()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", "");

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromFile_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "NonExistent.cs");

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromFile_WithUsingStatementsBeforeNamespace_ReturnsNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            using System;
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Builder;

            namespace LiveFree.Transactions.Services;

            public class TransactionService { }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Transactions.Services", result);
    }

    [Fact]
    public void ResolveFromFile_WithCommentsBeforeNamespace_ReturnsNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            // This is a comment
            /* Multi-line
               comment */

            namespace LiveFree.Cellular.Api;

            public class Test { }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Cellular.Api", result);
    }

    [Theory]
    [InlineData("LiveFree.Accounts")]
    [InlineData("LiveFree.Accounts.Api")]
    [InlineData("LiveFree.Accounts.Api.Extensions")]
    [InlineData("LiveFree.Artemis.Ticketing.Services")]
    [InlineData("Company.Product.Module")]
    public void ResolveFromFile_WithVariousNamespaces_ParsesCorrectly(string expectedNamespace)
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", $"namespace {expectedNamespace};\npublic class Test {{ }}");

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal(expectedNamespace, result);
    }

    #endregion

    #region ResolveFromDirectory Tests

    [Fact]
    public void ResolveFromDirectory_WithCsFiles_ReturnsNamespace()
    {
        // Arrange
        CreateCsFile("Test.cs", """
            namespace LiveFree.Accounts.Api;
            public class Test { }
            """);

        // Act
        var result = _sut.ResolveFromDirectory(_tempDir);

        // Assert
        Assert.Equal("LiveFree.Accounts.Api", result);
    }

    [Fact]
    public void ResolveFromDirectory_WithExtensionsSubfolder_PrefersExtensions()
    {
        // Arrange
        // File at root
        CreateCsFile("Test.cs", """
            namespace LiveFree.Accounts.Api;
            public class Test { }
            """);

        // File in Extensions folder (should be preferred)
        Directory.CreateDirectory(Path.Combine(_tempDir, "Extensions"));
        CreateCsFile("Extensions/ServiceExtensions.cs", """
            namespace LiveFree.Accounts.Api.Extensions;
            public static class ServiceExtensions { }
            """);

        // Act
        var result = _sut.ResolveFromDirectory(_tempDir);

        // Assert
        // Should return base namespace (without .Extensions suffix)
        Assert.Equal("LiveFree.Accounts.Api", result);
    }

    [Fact]
    public void ResolveFromDirectory_WithNoCsFiles_ReturnsNull()
    {
        // Act
        var result = _sut.ResolveFromDirectory(_tempDir);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromDirectory_WithNonExistentDirectory_ReturnsNull()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_tempDir, "NonExistent");

        // Act
        var result = _sut.ResolveFromDirectory(nonExistentDir);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromDirectory_WithMultipleCsFiles_ReturnsFirst()
    {
        // Arrange
        CreateCsFile("A_First.cs", """
            namespace LiveFree.First;
            public class First { }
            """);
        CreateCsFile("B_Second.cs", """
            namespace LiveFree.Second;
            public class Second { }
            """);

        // Act
        var result = _sut.ResolveFromDirectory(_tempDir);

        // Assert
        // Should return something (order depends on filesystem)
        Assert.NotNull(result);
        Assert.StartsWith("LiveFree.", result);
    }

    #endregion

    #region InferFromProjectName Tests

    [Theory]
    [InlineData("LiveFree.Accounts.Api", "LiveFree.Accounts.Api")]
    [InlineData("LiveFree.Transactions.Services", "LiveFree.Transactions.Services")]
    [InlineData("LiveFree.Artemis.Ticketing.Models", "LiveFree.Artemis.Ticketing.Models")]
    [InlineData("MyProject", "MyProject")]
    public void InferFromProjectName_ReturnsProjectNameAsNamespace(string projectName, string expected)
    {
        // Act
        var result = _sut.InferFromProjectName(projectName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ResolveFromFile_WithMalformedCSharp_HandlesGracefully()
    {
        // Arrange
        var filePath = CreateCsFile("Malformed.cs", """
            namespace Incomplete
            // Missing closing brace and semicolon
            public class {
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        // Should handle gracefully - either return partial result or null
        // The regex should still match the namespace declaration
        Assert.Equal("Incomplete", result);
    }

    [Fact]
    public void ResolveFromFile_WithGlobalUsings_StillFindsNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            global using System;
            global using System.Linq;

            namespace LiveFree.Accounts.Api;

            public class Test { }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Accounts.Api", result);
    }

    [Fact]
    public void ResolveFromFile_WithNullableDirective_StillFindsNamespace()
    {
        // Arrange
        var filePath = CreateCsFile("Test.cs", """
            #nullable enable

            namespace LiveFree.Accounts.Api;

            public class Test { }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Accounts.Api", result);
    }

    [Fact]
    public void ResolveFromFile_WithImplicitUsings_StillFindsNamespace()
    {
        // Arrange - File without usings due to ImplicitUsings
        var filePath = CreateCsFile("Test.cs", """
            namespace LiveFree.Modern.Api;

            public class Test
            {
                public List<string> Items { get; } = new();
            }
            """);

        // Act
        var result = _sut.ResolveFromFile(filePath);

        // Assert
        Assert.Equal("LiveFree.Modern.Api", result);
    }

    #endregion

    #region Helper Methods

    private string CreateCsFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    #endregion
}
