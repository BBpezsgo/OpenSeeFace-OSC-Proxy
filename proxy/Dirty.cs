public struct Dirty<T>(Func<T, T, bool> threshold, T value) where T : notnull
{
    T _value = value;
    bool _isDirty = true;
    readonly Func<T, T, bool> threshold = threshold;

    public T Value
    {
        readonly get => _value;
        set
        {
            if (threshold.Invoke(_value, value))
            {
                _value = value;
                _isDirty = true;
            }
        }
    }

    public bool IsDirty { readonly get => _isDirty; set => _isDirty = value; }
}
