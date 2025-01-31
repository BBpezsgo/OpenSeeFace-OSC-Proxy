using System.Net;
using System.Numerics;
using Win32.Console;
using Win32;
using SharpOSC;
using OpenSee;

static class Program
{
    const bool Render = false;

    static Vector2 Transform(this Vector2 v, in Face face, IRenderer renderer)
    {
        return v / face.CameraResolution * new Vector2(renderer.Width, renderer.Height);
    }

    static Vector2 Transform(this Vector2 v, in FaceData face, IRenderer renderer)
    {
        return v * new Vector2(renderer.Width, renderer.Height);
    }

    static Vector2 Transform(this Vector3 v, in FaceData face, IRenderer renderer)
    {
        return new Vector2(v.X * renderer.Width, v.Y * renderer.Height);
    }

    static Vector2 Lerp(Vector2 a, Vector2 b, float t) => (a * (1f - t)) + (b * t);
    static Vector3 Lerp(Vector3 a, Vector3 b, float t) => (a * (1f - t)) + (b * t);

    static void CoolerLine(this AnsiRenderer renderer, Coord a, Coord b, AnsiColor color)
    {
        a.Y *= 2;
        b.Y *= 2;

        int dx = b.X - a.X;
        int dy = b.Y - a.Y;

        int sx = Math.Sign(dx);
        int sy = Math.Sign(dy);

        dx = Math.Abs(dx);
        dy = Math.Abs(dy);
        int d = Math.Max(dx, dy);

        float r = d / 2f;

        int x = a.X;
        int y = a.Y;

        if (dx > dy)
        {
            for (int i = 0; i < d; i++)
            {
                if (x < 0 || x >= renderer.Width || y < 0 || y / 2 >= renderer.Height) continue;

                char c;
                if ((y & 1) == 0)
                {
                    c = '▀';
                }
                else
                {
                    c = '▄';
                }

                renderer.Set(x, y / 2, new AnsiChar(c, (byte)color));

                x += sx;
                r += dy;

                if (r >= dx)
                {
                    y += sy;
                    r -= dx;
                }
            }
        }
        else
        {
            int _y = -1;
            int _x = -1;
            for (int i = 0; i < d; i++)
            {
                if (x < 0 || x >= renderer.Width || y < 0 || y / 2 >= renderer.Height) continue;

                char c;
                if (_y == y && _x == x)
                {
                    c = '█';
                }
                else if ((y & 1) == 0)
                {
                    c = '▀';
                    if (sy > 0) _y = y + sy;
                    else _y = -1;
                }
                else
                {
                    c = '▄';
                    if (sy < 0) _y = y + sy;
                    else _y = -1;
                }

                renderer.Set(x, y / 2, new AnsiChar(c, (byte)color));
                _x = x;

                y += sy;
                r += dx;

                if (r >= dy)
                {
                    x += sx;
                    r -= dy;
                }
            }
        }
    }
    static void Sphere(this AnsiRenderer renderer, Coord c, int r, AnsiColor color)
    {
        int hr = r / 2 + 1;

        Coord min = new(Math.Max(0, c.X - hr), Math.Max(0, c.Y - hr));
        Coord max = new(Math.Min(renderer.Width - 1, c.X + hr), Math.Min(renderer.Height - 1, c.Y + hr));

        min *= 2;
        max *= 4;

        c.X *= 2;
        c.Y *= 4;

        const string Barille = "⠀⠁⠂⠃⠄⠅⠆⠇⠈⠉⠊⠋⠌⠍⠎⠏⠐⠑⠒⠓⠔⠕⠖⠗⠘⠙⠚⠛⠜⠝⠞⠟⠠⠡⠢⠣⠤⠥⠦⠧⠨⠩⠪⠫⠬⠭⠮⠯⠰⠱⠲⠳⠴⠵⠶⠷⠸⠹⠺⠻⠼⠽⠾⠿⡀⡁⡂⡃⡄⡅⡆⡇⡈⡉⡊⡋⡌⡍⡎⡏⡐⡑⡒⡓⡔⡕⡖⡗⡘⡙⡚⡛⡜⡝⡞⡟⡠⡡⡢⡣⡤⡥⡦⡧⡨⡩⡪⡫⡬⡭⡮⡯⡰⡱⡲⡳⡴⡵⡶⡷⡸⡹⡺⡻⡼⡽⡾⡿⢀⢁⢂⢃⢄⢅⢆⢇⢈⢉⢊⢋⢌⢍⢎⢏⢐⢑⢒⢓⢔⢕⢖⢗⢘⢙⢚⢛⢜⢝⢞⢟⢠⢡⢢⢣⢤⢥⢦⢧⢨⢩⢪⢫⢬⢭⢮⢯⢰⢱⢲⢳⢴⢵⢶⢷⢸⢹⢺⢻⢼⢽⢾⢿⣀⣁⣂⣃⣄⣅⣆⣇⣈⣉⣊⣋⣌⣍⣎⣏⣐⣑⣒⣓⣔⣕⣖⣗⣘⣙⣚⣛⣜⣝⣞⣟⣠⣡⣢⣣⣤⣥⣦⣧⣨⣩⣪⣫⣬⣭⣮⣯⣰⣱⣲⣳⣴⣵⣶⣷⣸⣹⣺⣻⣼⣽⣾⣿";

        for (int y = min.Y; y <= max.Y; y++)
        {
            for (int x = min.X; x <= max.X; x++)
            {
                int dx = x - c.X;
                int dy = y - c.Y;
                if (dx * dx + dy * dy < hr * hr)
                {
                    char chr = (x % 2, y % 4) switch
                    {
                        (0, 0) =>  '⠁',
                        (0, 1) =>  '⠂',
                        (0, 2) =>  '⠄',
                        (0, 3) =>  '⡀',
                        (1, 0) =>  '⠈',
                        (1, 1) =>  '⠐',
                        (1, 2) =>  '⠠',
                        (1, 3) =>  '⢀',
                        _ =>       '⠀',
                    };
                    if (renderer[x / 2, y / 4].Char >= Barille[0] &&
                        renderer[x / 2, y / 4].Char <= Barille[^1])
                    {
                        renderer[x / 2, y / 4].Char |= chr;
                    }
                    else
                    {
                        renderer[x / 2, y / 4] = new AnsiChar(chr, (byte)color);
                    }
                }
            }
        }
    }

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
        SimpleExpressionDetector expressionDetector = new();
        EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        byte[] buffer = new byte[OpenSeeReceiver.PacketFrameSize];

        Smooth<Vector3> lerpedHeadForward = new(Lerp, new Vector3(0f, 0f, 1f));
        Dirty<Vector3> dirtyFaceForward = new((a, b) => Vector3.Distance(a, b) > 0.01f, new Vector3(0f, 0f, 1f));

        using FaceDataReceiver faceDataReceiver = new();
        faceDataReceiver.Start();

        while (true)
        {
            Thread.Sleep(50);

            if (faceDataReceiver.CurrentFace.Time == default) continue;

            if (Render) renderer.Clear();

            void DrawPoint(Vector2 p, AnsiColor color = AnsiColor.White)
            {
                renderer[new Vector2(Math.Clamp(p.X, 0f, renderer.Width - 1f), Math.Clamp(p.Y, 0f, renderer.Height - 1f))] = new AnsiChar('.', (byte)color);
            }

            // if (Render)
            // {
            //     for (int i = 0; i < 468; i++)
            //     {
            //         DrawPoint(new Vector2(bruhReceiver.CurrentFace.Points[i].X, bruhReceiver.CurrentFace.Points[i].Y).Transform(bruhReceiver.CurrentFace, renderer));
            //     }
            // }

            var pFrom = (faceDataReceiver.CurrentFace.Points[162] + faceDataReceiver.CurrentFace.Points[389]) / 2f;
            var pTo = faceDataReceiver.CurrentFace.Points[9];
            var headForward = Vector3.Normalize(pFrom - pTo);
            // var headCenter = pTo.Transform(faceDataReceiver.CurrentFace, renderer);
            var headCenter = new Coord(renderer.Width / 2, renderer.Height / 2);

            headForward.Z = headForward.Z / 2f; // MathF.Sqrt(MathF.Abs(headForward.Z)) * MathF.Sign(headForward.Z);
            headForward.X = MathF.Pow(MathF.Abs(headForward.X), 1.5f) * MathF.Sign(headForward.X);
            headForward.Y = MathF.Pow(MathF.Abs(headForward.Y), 1.5f) * MathF.Sign(headForward.Y);
            headForward = Vector3.Normalize(headForward);

            lerpedHeadForward.Value = headForward;
            dirtyFaceForward.Value = lerpedHeadForward.Value;

            if (Render)
            {
                DrawPoint(headCenter + (new Vector2(-lerpedHeadForward.Value.X, -lerpedHeadForward.Value.Y) * lerpedHeadForward.Value.Z * new Vector2(renderer.Width, renderer.Height)), AnsiColor.Red);
                // CoolerLine(renderer,
                //     (Coord)headCenter,
                //     (Coord)(headCenter + (new Vector2(-lerpedHeadForward.Value.X, -lerpedHeadForward.Value.Y) * lerpedHeadForward.Value.Z * new Vector2(renderer.Width, renderer.Height))),
                //     AnsiColor.Red);

                // renderer.Sphere((Coord)(new Vector2(bruhReceiver.CurrentFace.LeftEyeCenter.X, bruhReceiver.CurrentFace.LeftEyeCenter.Y) * new Vector2(renderer.Width, renderer.Height)), (int)bruhReceiver.CurrentFace.LeftEyeRadius, AnsiColor.Green);
                // renderer.Sphere((Coord)(new Vector2(bruhReceiver.CurrentFace.RightEyeCenter.X, bruhReceiver.CurrentFace.RightEyeCenter.Y) * new Vector2(renderer.Width, renderer.Height)), (int)bruhReceiver.CurrentFace.RightEyeRadius, AnsiColor.Yellow);
            }

            if (dirtyFaceForward.IsDirty)
            {
                dirtyFaceForward.IsDirty = false;
                oscSender.Send(new OscMessage("/tracking/eye/CenterVec", dirtyFaceForward.Value));
                // Console.WriteLine("Message sent");
            }

            // RendererExtensions.Line(renderer,
            //     new Coord(renderer.Width / 2, renderer.Height / 2),
            //     new Coord(renderer.Width / 2, renderer.Height / 2) + (Coord)(new Vector2(bruhReceiver.CurrentFace.LeftGaze.X, bruhReceiver.CurrentFace.LeftGaze.Y) * 10f),
            //     new AnsiChar('X')
            // );

            if (Render)
            {
                Console.Clear();
                renderer.Render();
            }
        }
    }
}
