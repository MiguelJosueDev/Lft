using FluentAssertions;
using Lft.Engine.Variables;

namespace Lft.Engine.Tests.Variables;

public class VariableContextTests
{
    [Fact]
    public void Set_ShouldStoreValue()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("TestKey", "TestValue");

        // Assert
        var variables = context.AsReadOnly();
        variables["TestKey"].Should().Be("TestValue");
    }

    [Fact]
    public void Set_WithNullValue_ShouldStoreNull()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("TestKey", null);

        // Assert
        var variables = context.AsReadOnly();
        variables.Should().ContainKey("TestKey");
        variables["TestKey"].Should().BeNull();
    }

    [Fact]
    public void Set_WithSameKeyCaseSensitive_ShouldOverwrite()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("TestKey", "Value1");
        context.Set("TestKey", "Value2");

        // Assert
        var variables = context.AsReadOnly();
        variables["TestKey"].Should().Be("Value2");
    }

    [Fact]
    public void Set_WithDifferentCaseKeys_ShouldStoreSeparately()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("TestKey", "Value1");
        context.Set("testKey", "Value2");
        context.Set("TESTKEY", "Value3");

        // Assert
        var variables = context.AsReadOnly();
        variables.Should().HaveCount(3);
        variables["TestKey"].Should().Be("Value1");
        variables["testKey"].Should().Be("Value2");
        variables["TESTKEY"].Should().Be("Value3");
    }

    [Fact]
    public void Set_WithComplexObject_ShouldStoreObject()
    {
        // Arrange
        var context = new VariableContext();
        var complexObject = new
        {
            Name = "Test",
            Value = 123,
            Nested = new { Property = "Nested" }
        };

        // Act
        context.Set("ComplexKey", complexObject);

        // Assert
        var variables = context.AsReadOnly();
        var result = variables["ComplexKey"] as dynamic;
        result.Should().NotBeNull();
        ((string)result.Name).Should().Be("Test");
        ((int)result.Value).Should().Be(123);
    }

    [Fact]
    public void AsReadOnly_ShouldReturnReadOnlyDictionary()
    {
        // Arrange
        var context = new VariableContext();
        context.Set("Key1", "Value1");

        // Act
        var readOnly = context.AsReadOnly();

        // Assert
        readOnly.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>();
        readOnly.Should().ContainKey("Key1");
    }

    [Fact]
    public void Set_WithSpecialCharactersInKey_ShouldWork()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("_ModelName", "Value1");
        context.Set("$specialKey", "Value2");
        context.Set("key-with-dash", "Value3");

        // Assert
        var variables = context.AsReadOnly();
        variables["_ModelName"].Should().Be("Value1");
        variables["$specialKey"].Should().Be("Value2");
        variables["key-with-dash"].Should().Be("Value3");
    }

    [Theory]
    [InlineData("string value")]
    [InlineData(123)]
    [InlineData(123.456)]
    [InlineData(true)]
    [InlineData(false)]
    public void Set_WithDifferentValueTypes_ShouldWork(object value)
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("TestKey", value);

        // Assert
        var variables = context.AsReadOnly();
        variables["TestKey"].Should().Be(value);
    }

    [Fact]
    public void Set_WithMultipleKeys_ShouldStoreAll()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.Set("Key1", "Value1");
        context.Set("Key2", "Value2");
        context.Set("Key3", "Value3");
        context.Set("Key4", "Value4");
        context.Set("Key5", "Value5");

        // Assert
        var variables = context.AsReadOnly();
        variables.Should().HaveCount(5);
    }
}