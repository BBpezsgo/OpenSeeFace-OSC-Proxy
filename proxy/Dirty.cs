namespace VRChatProxy;

public struct Dirty<T> where T : notnull
{
    readonly Func<T, T, bool> _threshold;
    T _value;

    public readonly T Value => _value;

    public Dirty(Func<T, T, bool> threshold, T defaultValue)
    {
        _threshold = threshold;
        _value = defaultValue;
    }

    public bool TryChange(T value)
    {
        if (_threshold.Invoke(_value, value))
        {
            _value = value;
            return true;
        }
        return false;
    }
}
