using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Win32.Console;
using Win32;
using SharpOSC;
using OpenSee;

static class Program
{
    const bool Render = true;

    static Vector2 Transform(this Vector2 v, in Face face, IRenderer renderer)
    {
        return v / face.CameraResolution * new Vector2(renderer.Width, renderer.Height);
    }

    static Vector2 Lerp(Vector2 a, Vector2 b, float t) => (a * (1f - t)) + (b * t);
    static Vector3 Lerp(Vector3 a, Vector3 b, float t) => (a * (1f - t)) + (b * t);

    static unsafe void Main()
    {
        using UDPListener oscListener = new(9001, (OscPacket? packet) =>
        {
            switch (packet)
            {
                case OscMessage message:
                    Console.WriteLine(message.Address);
                    Console.WriteLine(string.Join(' ', message.Arguments));
                    break;
                case OscBundle bundle:
                    Console.WriteLine(bundle.Messages);
                    break;
            }
        });
        using UDPSender oscSender = new("127.0.0.1", 9000);
        AnsiRenderer renderer = new();
        SimpleExpressionDetector expression = new();
        Face prevFace = default;
        using UdpClient listener = new(11573, AddressFamily.InterNetwork);
        EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        byte[] buffer = new byte[OpenSeeReceiver.PacketFrameSize];

        while (true)
        {
            listener.Receive(buffer, ref ep);
            Face face = new();
            int offset = 0;
            face.FromPacket(buffer, ref offset);

            // Console.WriteLine(expression.Detect(face));

            for (int i = 0; i < OpenSeeReceiver.PointCount; i++)
            {
                face.Points[i] = Lerp(prevFace.Points[i], face.Points[i], 0.7f);
            }

            for (int i = 0; i < OpenSeeReceiver.PointCount + 2; i++)
            {
                face.Points3D[i] = Lerp(prevFace.Points3D[i], face.Points3D[i], 0.7f);
            }

            prevFace = face;

            void DrawPoint(Vector2 p, AnsiColor color = AnsiColor.White)
            {
                renderer[new Vector2(Math.Clamp(p.X, 0f, renderer.Width - 1f), Math.Clamp(p.Y, 0f, renderer.Height - 1f))] = new AnsiChar('.', (byte)color);
            }

            if (Render)
            {
                renderer.Clear();
                for (int i = 0; i < OpenSeeReceiver.PointCount; i++)
                {
                    Vector2 p = face.Points[i].Transform(face, renderer);

                    DrawPoint(p);
                    renderer.Text(p, $"{i}");
                }
            }

            var pFrom = (face.Points[0] + face.Points[16]) / 2f;
            var pTo = face.Points[27];
            var headForward = new Vector3(Vector2.Normalize(pTo - pFrom), Vector2.Distance(pFrom, pTo));
            if (Render)
            {
                RendererExtensions.Line(renderer,
                    (Coord)pTo.Transform(face, renderer),
                    (Coord)(pTo.Transform(face, renderer) + (new Vector2(headForward.X, headForward.Y) * Vector3.Distance(default, headForward))),
                    new AnsiChar('X', (byte)AnsiColor.Red));
            }

            oscSender.Send(new OscMessage("/tracking/eye/CenterVec", Vector3.Normalize(headForward)));

            if (Render)
            {
                Console.Clear();
                renderer.Render();
            }
        }
    }
}
