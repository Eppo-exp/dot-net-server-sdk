using System.Collections;
using System.Diagnostics.CodeAnalysis;
using eppo_sdk.exception;
using eppo_sdk.validators;

namespace eppo_sdk.dto.bandit;

/// A set of attributes for a given context `Key`.
public interface IContextAttributes : IDictionary<string, object>
{
    public string Key { get; init; }
};

/// A contextual dictionary allowing only string, bool and numeric types.
public class ContextAttributes : IContextAttributes
{

    public string Key { get; init; }

    private readonly Dictionary<string, object> _internalDictionary = new();


    public ContextAttributes(string key)
    {
        this.Key = key;
    }

    public static ContextAttributes FromDict(string key, IDictionary<string, object?> other)
    {
        var obj = new ContextAttributes(key);
        obj.AddDict(other);
        return obj;
    }

    public static ContextAttributes FromNullableAttributes(string key, IDictionary<string, string?>? categoricalAttributes, IDictionary<string, double?>? numericAttributes)
    {
        var obj = new ContextAttributes(key);
        obj.AddDict(categoricalAttributes);
        obj.AddDict(numericAttributes);
        return obj;
    }


    public ContextAttributes(string key, IDictionary<string, string>? categoricalAttributes, IDictionary<string, double>? numericAttributes)
    {
        Key = key;
        AddDict(categoricalAttributes);
        AddDict(numericAttributes);
    }

    private void AddDict<TV>(IDictionary<string, TV>? dict)
    {
        if (dict == null) return;
        foreach (var kvp in dict)
        {
            if (kvp.Value == null)
            {
                continue;
            }
            Add(kvp.Key, kvp.Value);
        }
    }

    /// Adds a value to the subject dictionary enforcing only string, bool and numeric types.
    public void Add(string key, object value)
    {
        if (value == null) return;
        // Implement your custom validation logic here
        if (IsNumeric(value) || IsCategorical(value))
        {
            _internalDictionary.Add(key, value);
        }
        else
        {
            throw new InvalidAttributeTypeException(key, value);
        }
    }

    public IDictionary<string, object> AsDict() => _internalDictionary.ToDictionary(kvp=>kvp.Key, kvp=>kvp.Value);

    /// Gets only the numeric attributes.
    public IDictionary<string, double> GetNumeric()
    {
        var nums = this.Where(kvp => IsNumeric(kvp.Value));
        return nums.ToDictionary(kvp => kvp.Key, kvp => Convert.ToDouble(kvp.Value));
    }

    /// Gets only the string attributes.
    public IDictionary<string, string> GetCategorical()
    {
        var cats = this.Where(kvp => kvp.Value is string || kvp.Value is bool);
        return cats.ToDictionary(kvp => kvp.Key, kvp => Compare.ToString(kvp.Value));
    }

    public AttributeSet AsAttributeSet() => new(GetCategorical(), GetNumeric());

    public static bool IsNumeric(object v) => v is double || v is int || v is long || v is float;

    private static bool IsCategorical(object value) => value is string || value is bool;


    // Standard Dictionary methods are "sealed" so overriding isn't possible. Thus we delegate everything here.

    public object this[string key] { get => _internalDictionary[key]; set => Add(key, value); }

    public ICollection<string> Keys => _internalDictionary.Keys;

    public ICollection<object> Values => _internalDictionary.Values;

    public int Count => _internalDictionary.Count;

    public bool IsReadOnly => false;

    public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

    public void Clear() => _internalDictionary.Clear();

    public bool Contains(KeyValuePair<string, object> item) => _internalDictionary.ContainsKey(item.Key) && _internalDictionary[item.Key].Equals(item.Value);

    public bool ContainsKey(string key) => _internalDictionary.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _internalDictionary.GetEnumerator();

    public bool Remove(string key) => _internalDictionary.Remove(key);

    public bool Remove(KeyValuePair<string, object> item) => _internalDictionary.Remove(item.Key); // Assuming removal by key

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _internalDictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_internalDictionary).CopyTo(array, arrayIndex);
}

