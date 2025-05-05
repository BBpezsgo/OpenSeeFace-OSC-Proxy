namespace VRChatProxy;

public ref struct Reader
{
    readonly Span<byte> _buffer;
    int _pointer;

    public Reader(Span<byte> buffer)
    {
        _buffer = buffer;
        _pointer = 0;
    }

    /// <exception cref="EndOfStreamException"/>
    public unsafe T* Read<T>() where T : unmanaged
    {
        if (_pointer + sizeof(T) > _buffer.Length) throw new EndOfStreamException();
        fixed (byte* ptr = _buffer)
        {
            T* res = (T*)(ptr + _pointer);
            _pointer += sizeof(T);
            return res;
        }
    }
}