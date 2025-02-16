using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VRChatProxy;

[InlineArray(468)]
public struct Buffer468<T>
{
    T _element0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FaceData
{
    public readonly Buffer468<Vector3> Points;
    public readonly Vector2 LeftGaze;
    public readonly Vector2 RightGaze;
    public readonly Vector2 LeftEyeCenter;
    public readonly float LeftEyeRadius;
    public readonly Vector2 RightEyeCenter;
    public readonly float RightEyeRadius;
    public readonly int Expression;
}

public readonly struct Face(double time, FaceData data)
{
    public readonly double Time = time;
    public readonly FaceData Data = data;
}
