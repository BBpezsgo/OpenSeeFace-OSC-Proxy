using CLI;

namespace VRChatProxy;

public static class HandRenderer
{
    public static void RenderHand(this AnsiRendererExtended renderer, in Hand hand, in Camera camera, AnsiColor color)
    {
        renderer.RenderLine(hand.Points[0], hand.Points[1], camera, color);
        renderer.RenderLine(hand.Points[1], hand.Points[2], camera, color);
        renderer.RenderLine(hand.Points[2], hand.Points[3], camera, color);
        renderer.RenderLine(hand.Points[3], hand.Points[4], camera, color);

        renderer.RenderLine(hand.Points[0], hand.Points[5], camera, color);
        renderer.RenderLine(hand.Points[0], hand.Points[17], camera, color);

        renderer.RenderLine(hand.Points[5], hand.Points[9], camera, color);
        renderer.RenderLine(hand.Points[9], hand.Points[13], camera, color);
        renderer.RenderLine(hand.Points[13], hand.Points[17], camera, color);

        renderer.RenderLine(hand.Points[5], hand.Points[6], camera, color);
        renderer.RenderLine(hand.Points[6], hand.Points[7], camera, color);
        renderer.RenderLine(hand.Points[7], hand.Points[8], camera, color);

        renderer.RenderLine(hand.Points[9], hand.Points[10], camera, color);
        renderer.RenderLine(hand.Points[10], hand.Points[11], camera, color);
        renderer.RenderLine(hand.Points[11], hand.Points[12], camera, color);

        renderer.RenderLine(hand.Points[13], hand.Points[14], camera, color);
        renderer.RenderLine(hand.Points[14], hand.Points[15], camera, color);
        renderer.RenderLine(hand.Points[15], hand.Points[16], camera, color);

        renderer.RenderLine(hand.Points[17], hand.Points[18], camera, color);
        renderer.RenderLine(hand.Points[18], hand.Points[19], camera, color);
        renderer.RenderLine(hand.Points[19], hand.Points[20], camera, color);
    }
}
