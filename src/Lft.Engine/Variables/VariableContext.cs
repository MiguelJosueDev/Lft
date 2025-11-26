namespace Lft.Engine.Variables;

public sealed class VariableContext
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public void Set(string key, object? value) => _values[key] = value;

    /// <summary>
    /// Sets a variable only if it doesn't already exist. Useful for setting defaults.
    /// </summary>
    public void SetDefault(string key, object? value)
    {
        if (!_values.ContainsKey(key))
        {
            _values[key] = value;
        }
    }

    public bool HasKey(string key) => _values.ContainsKey(key);

    public IReadOnlyDictionary<string, object?> AsReadOnly() => _values;
}
