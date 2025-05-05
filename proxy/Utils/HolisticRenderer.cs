using CLI;

namespace VRChatProxy;

public static class HolisticRenderer
{
    public static void RenderHolistic(this AnsiRendererExtended renderer, in HolisticData holistic, in Camera camera)
    {
        renderer.RenderLine(holistic.Points[0].Point, holistic.Points[2].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[2].Point, holistic.Points[7].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[8].Point, holistic.Points[5].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[5].Point, holistic.Points[0].Point, camera, AnsiColor.White);

        renderer.RenderLine(holistic.Points[10].Point, holistic.Points[9].Point, camera, AnsiColor.White);

        renderer.RenderLine(holistic.Points[11].Point, holistic.Points[12].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[12].Point, holistic.Points[14].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[12].Point, holistic.Points[14].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[14].Point, holistic.Points[16].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[16].Point, holistic.Points[22].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[16].Point, holistic.Points[18].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[16].Point, holistic.Points[20].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[18].Point, holistic.Points[20].Point, camera, AnsiColor.White);

        renderer.RenderLine(holistic.Points[11].Point, holistic.Points[13].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[13].Point, holistic.Points[15].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[15].Point, holistic.Points[21].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[15].Point, holistic.Points[19].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[15].Point, holistic.Points[17].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[17].Point, holistic.Points[19].Point, camera, AnsiColor.White);

        renderer.RenderLine(holistic.Points[12].Point, holistic.Points[24].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[11].Point, holistic.Points[23].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[23].Point, holistic.Points[24].Point, camera, AnsiColor.White);

        renderer.RenderLine(holistic.Points[24].Point, holistic.Points[26].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[26].Point, holistic.Points[28].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[28].Point, holistic.Points[32].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[28].Point, holistic.Points[30].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[30].Point, holistic.Points[32].Point, camera, AnsiColor.White);

        renderer.RenderLine(holistic.Points[23].Point, holistic.Points[25].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[25].Point, holistic.Points[27].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[27].Point, holistic.Points[31].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[27].Point, holistic.Points[29].Point, camera, AnsiColor.White);
        renderer.RenderLine(holistic.Points[29].Point, holistic.Points[31].Point, camera, AnsiColor.White);
    }
}
