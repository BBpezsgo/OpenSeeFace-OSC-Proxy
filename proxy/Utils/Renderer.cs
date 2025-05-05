using System.Numerics;
using CLI;

namespace VRChatProxy;

public static class Renderer
{
    public static Vector2 Transform(this AnsiRendererExtended renderer, in Vector3 p, in Camera camera)
    {
        if (renderer.Width == -1 || renderer.Height == -1) return default;
        return new Vector2(
            Math.Clamp(p.X * renderer.Width, 0, renderer.Width - 1),
            Math.Clamp(p.Y * renderer.Height, 0, renderer.Height - 1)
        );
        return renderer.Project(camera, p);
    }

    public static void RenderLine(this AnsiRendererExtended renderer, in Vector3 a, in Vector3 b, in Camera camera, AnsiColor color)
    {
        renderer.LineBarille(
            renderer.Transform(a, camera),
            renderer.Transform(b, camera),
            color
        );
    }
}