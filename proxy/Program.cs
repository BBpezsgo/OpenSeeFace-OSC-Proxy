using SharpOSC;
using System.Collections.Immutable;
using System.Numerics;
using CLI;
using Maths;

namespace VRChatProxy;

static class Program
{
    const bool Render = false;

    static unsafe void Main()
    {
        AvatarParameters avatarParameters = new();
        using UDPListener oscListener = new(9001, (IOscPacket? packet) =>
        {
            avatarParameters.HandlePacket(packet);
            // switch (packet)
            // {
            //     case OscMessage message:
            //         Console.WriteLine($"{message.Address} {string.Join(' ', message.Arguments)}");
            //         break;
            //     case OscBundle bundle:
            //         foreach (var message in bundle.Messages)
            //         {
            //             Console.WriteLine($"{message.Address} {string.Join(' ', message.Arguments)}");
            //         }
            //         break;
            // }
        });
        using UDPSender oscSender = new("127.0.0.1", 9000);
        using FaceDataReceiver faceDataReceiver = new();
        faceDataReceiver.Start();

        ImmutableArray<string> expressionLabels = File.ReadAllLines("/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier_label.csv").ToImmutableArray();

        AnsiRendererExtended renderer = new();

        Vector3 lerpedHeadForward = new(0f, 0f, 1f);
        Dirty<Vector3> dirtyFaceForward = new((a, b) => Vector3.Distance(a, b) > 0.01f, new Vector3(0f, 0f, 1f));

        double prevTime = 1d;

        KalmanFilter k = new();

        while (true)
        {
            Thread.Sleep(50);

            if (faceDataReceiver.CurrentFace.Time < prevTime) continue;
            prevTime = faceDataReceiver.CurrentFace.Time;

            var resolution = new Vector2(renderer.Width, renderer.Height);

            if (Render) renderer.Clear();

            void DrawPoint(Vector2 p, AnsiColor color = AnsiColor.White)
            {
                renderer[new Vector2(Math.Clamp(p.X, 0f, renderer.Width - 1f), Math.Clamp(p.Y, 0f, renderer.Height - 1f))] = new AnsiChar('.', color);
            }

            // if (faceDataReceiver.CurrentFace.Expression != -1) Console.WriteLine(expressionLabels[faceDataReceiver.CurrentFace.Expression]);

            // if (Render)
            // {
            //     for (int i = 0; i < 468; i++)
            //     {
            //         var p = new Vector2(faceDataReceiver.CurrentFace.Points[i].X, faceDataReceiver.CurrentFace.Points[i].Y) * resolution;
            //         DrawPoint(p);
            //     }
            // }

            var headCenter = new Vector2Int(renderer.Width / 2, renderer.Height / 2);

            const int HEAD_UP = 10;
            const int HEAD_DOWN = 152;
            const int HEAD_RIGHT = 234;
            const int HEAD_LEFT = 454;

            var headUp = Vector3.Normalize(faceDataReceiver.CurrentFace.Points[HEAD_UP] - faceDataReceiver.CurrentFace.Points[HEAD_DOWN]);
            var headRight = Vector3.Normalize(faceDataReceiver.CurrentFace.Points[HEAD_RIGHT] - faceDataReceiver.CurrentFace.Points[HEAD_LEFT]);
            var headForward = Vector3.Normalize(Vector3.Cross(headUp, headRight));

            var correctedHeadForward = Utils.RotateVector(headForward, headRight, -0.2f);

            lerpedHeadForward = k.Apply(correctedHeadForward);
            if (MathF.Acos(Vector3.Dot(lerpedHeadForward, correctedHeadForward)) > 0.2f)
            {
                lerpedHeadForward = correctedHeadForward;
                k = new KalmanFilter();
            }

            if (Render)
            {
                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + (new Vector2(headForward.X, headForward.Y) * 1f / -headForward.Z * resolution),
                    (AnsiColor)Ansi.ToAnsi256(80, 80, 80));

                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + (new Vector2(correctedHeadForward.X, correctedHeadForward.Y) * 1f / -correctedHeadForward.Z * resolution),
                    (AnsiColor)Ansi.ToAnsi256(150, 150, 150));

                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + (new Vector2(lerpedHeadForward.X, lerpedHeadForward.Y) * 1f / -lerpedHeadForward.Z * resolution),
                    AnsiColor.Red);

                renderer.FillCircle((Vector2Int)(new Vector2(faceDataReceiver.CurrentFace.LeftEyeCenter.X, faceDataReceiver.CurrentFace.LeftEyeCenter.Y) * resolution), (int)faceDataReceiver.CurrentFace.LeftEyeRadius, AnsiColor.Green);
                renderer.FillCircle((Vector2Int)(new Vector2(faceDataReceiver.CurrentFace.RightEyeCenter.X, faceDataReceiver.CurrentFace.RightEyeCenter.Y) * resolution), (int)faceDataReceiver.CurrentFace.RightEyeRadius, AnsiColor.Yellow);

                /*
                const int RIGHT_EYE_RIGHT = 33;
                const int RIGHT_EYE_LEFT = 133;
                const int RIGHT_EYE_TOP = 27;
                const int RIGHT_EYE_BOTTOM = 23;

                float top = faceDataReceiver.CurrentFace.Points[RIGHT_EYE_TOP].Y;
                float bottom = faceDataReceiver.CurrentFace.Points[RIGHT_EYE_BOTTOM].Y;
                (top, bottom) = (Math.Min(top, bottom), Math.Max(top, bottom));
                float y = Math.Clamp(faceDataReceiver.CurrentFace.RightEyeCenter.Y, top, bottom);
                y = (y - top) / (bottom - top);

                float left = faceDataReceiver.CurrentFace.Points[RIGHT_EYE_LEFT].X;
                float right = faceDataReceiver.CurrentFace.Points[RIGHT_EYE_RIGHT].X;
                (left, right) = (Math.Min(left, right), Math.Max(left, right));
                float x = Math.Clamp(faceDataReceiver.CurrentFace.RightEyeCenter.X, left, right);
                x = (x - left) / (right - left);

                CoolerLine(renderer,
                        (Coord)(faceDataReceiver.CurrentFace.RightEyeCenter * resolution),
                        (Coord)((faceDataReceiver.CurrentFace.RightEyeCenter * resolution) + (new Vector2(x, y) * resolution)),
                        AnsiColor.Yellow);
                */
            }

            avatarParameters["/tracking/eye/CenterVec"] = lerpedHeadForward * new Vector3(1f, -1f, -1f);
            avatarParameters.Sync(oscSender);

            if (Render)
            {
                Console.Clear();
                renderer.Render();
            }
        }
    }
}
