using System.Numerics;

namespace VRChatProxy;

public static class Utils
{
    public static Vector3 RotateVector(Vector3 v, Vector3 k, float angle)
    {
        return v * MathF.Cos(angle) + Vector3.Cross(v, k) * MathF.Sin(angle) + k * Vector3.Dot(k, v) * (1f - MathF.Cos(angle));
    }

    public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
    {
        float theta = MathF.Acos(Vector3.Dot(a, b));
        float sinTheta = MathF.Sin(theta);
        return (MathF.Sin((1f - t) * theta) / sinTheta * a) +
               (MathF.Sin(t * theta) / sinTheta * b);
    }

    public static float Lerp(float a, float b, float t) => (a * (1f - t)) + (b * t);
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => (a * (1f - t)) + (b * t);
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => (a * (1f - t)) + (b * t);
}
