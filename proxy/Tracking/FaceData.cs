using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VRChatProxy;

[InlineArray(468)]
public struct Buffer468<T>
{
    T _element0;
}

[InlineArray(33)]
public struct Buffer33<T>
{
    T _element0;
}

static class HandLandmarks
{
    public const int WRIST = 0;
    public const int THUMB_CMC = 1;
    public const int THUMB_MCP = 2;
    public const int THUMB_IP = 3;
    public const int THUMB_TIP = 4;
    public const int INDEX_FINGER_MCP = 5;
    public const int INDEX_FINGER_PIP = 6;
    public const int INDEX_FINGER_DIP = 7;
    public const int INDEX_FINGER_TIP = 8;
    public const int MIDDLE_FINGER_MCP = 9;
    public const int MIDDLE_FINGER_PIP = 10;
    public const int MIDDLE_FINGER_DIP = 11;
    public const int MIDDLE_FINGER_TIP = 12;
    public const int RING_FINGER_MCP = 13;
    public const int RING_FINGER_PIP = 14;
    public const int RING_FINGER_DIP = 15;
    public const int RING_FINGER_TIP = 16;
    public const int PINKY_MCP = 17;
    public const int PINKY_PIP = 18;
    public const int PINKY_DIP = 19;
    public const int PINKY_TIP = 20;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HolisticPoint
{
    public readonly Vector3 Point;
    public readonly float Visibility;
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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HolisticData
{
    public readonly Buffer33<HolisticPoint> Points;
}

public readonly struct Face(double time, FaceData data)
{
    public readonly double Time = time;
    public readonly FaceData Data = data;
}

public readonly struct Holistic(double time, HolisticData data)
{
    public readonly double Time = time;
    public readonly HolisticData Data = data;
}
