using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[InlineArray(468)]
public struct Buffer468<T>
{
    T _element0;

    [UnscopedRef]
    public readonly ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _element0), 468);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FaceData
{
    public readonly double Time;
    public readonly int Width;
    public readonly int Height;
    public readonly Buffer468<Vector3> Points;
    public readonly Vector2 LeftGaze;
    public readonly Vector2 RightGaze;
    public readonly Vector2 LeftEyeCenter;
    public readonly float LeftEyeRadius;
    public readonly Vector2 RightEyeCenter;
    public readonly float RightEyeRadius;
}
