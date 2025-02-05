using System.Numerics;

namespace VRChatProxy;

public static class Utils
{
    public static float Lerp(float a, float b, float t) => (a * (1f - t)) + (b * t);
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => (a * (1f - t)) + (b * t);
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => (a * (1f - t)) + (b * t);
}
