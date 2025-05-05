using System.Numerics;

namespace VRChatProxy;

public readonly struct Camera(
    Vector3 position,
    float aspectRatio,
    float fov = MathF.PI / 3f
)
{
    public readonly Matrix4x4 Matrix = Matrix4x4.CreateTranslation(-position) * Matrix4x4.CreateRotationX(-0.2f);
    readonly float fovFactor = MathF.Tan(fov / 2f);

    public Vector2 Project(Vector3 point)
    {
        point = Vector3.Transform(point, Matrix);

        if (point.Z <= 0f) return default;

        return new Vector2(
            ((point.X / fovFactor * aspectRatio * point.Z) + 1f) * 0.5f,
            ((point.Y / fovFactor * point.Z) + 1f) * 0.5f
        );
    }
}
