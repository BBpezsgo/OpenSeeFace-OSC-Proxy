using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VRChatProxy;

[InlineArray(21)]
public struct Buffer21<T>
{
    T _element0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HandData
{
    public readonly int HandednessIndex;
    public readonly float HandednessScore;
    public readonly Buffer21<Vector3> Points;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Hand(double time, Buffer21<Vector3> points)
{
    public readonly double Time = time;
    public readonly Buffer21<Vector3> Points = points;
}
