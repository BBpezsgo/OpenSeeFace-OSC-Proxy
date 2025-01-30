public struct Smooth<T>(Func<T, T, float, T> lerper, T value) where T : notnull
{
    T _value = value;
    readonly Func<T, T, float, T> _lerper = lerper;

    public T Value
    {
        readonly get => _value;
        set => _value = _lerper.Invoke(_value, value, 0.5f);
    }
}
