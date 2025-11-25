namespace Lft.Engine.Variables;

public sealed class VariableContext
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public void Set(string key, object? value) => _values[key] = value;

    public IReadOnlyDictionary<string, object?> AsReadOnly() => _values;
}
