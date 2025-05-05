using System.Numerics;
using CLI;

namespace VRChatProxy;

public static class Perspective
{
    public static Vector2 Project<T>(this BufferedRenderer<T> renderer, in Camera camera, Vector3 point)
    {
        Vector2 pointView = camera.Project(point);

        return new Vector2(
            Math.Clamp(pointView.X * renderer.Width, 0, renderer.Width - 1),
            Math.Clamp(pointView.Y * renderer.Height, 0, renderer.Height - 1)
        );
    }
}
