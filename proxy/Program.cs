using SharpOSC;
using System.Collections.Immutable;
using System.Numerics;
using CLI;
using Maths;

namespace VRChatProxy;

static class Program
{
    const bool Render = false;
    const int ForwardCalibrationIterations = 500;

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
        using Receiver faceDataReceiver = new();
        faceDataReceiver.Start();

        ImmutableArray<string> expressionLabels = File.ReadAllLines("/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier_label.csv").ToImmutableArray();

        AnsiRendererExtended renderer = new();

        Vector3 lerpedHeadForward = new(0f, 0f, 1f);
        Dirty<Vector3> dirtyFaceForward = new((a, b) => Vector3.Distance(a, b) > 0.01f, new Vector3(0f, 0f, 1f));

        Vector3 lastHandForward = default;
        Vector3 lastHandRight = default;

        (Vector3 V, int N) average = (lerpedHeadForward, 0);

        double prevTime = 1d;

        KalmanFilter k = new();

        while (true)
        {
            Thread.Sleep(50);

            Vector2 Transform(Vector3 p) => new(Math.Clamp(p.X * faceDataReceiver.CameraResolution.X, 0f, renderer.Width - 1f), Math.Clamp(p.Y * faceDataReceiver.CameraResolution.Y, 0f, renderer.Height - 1f));

            void Connect(in Buffer21<Vector3> points, int a, int b, AnsiColor color) => renderer.LineBarille(
                Transform(points[a]),
                Transform(points[b]),
                color
            );

            void DrawPoint(Vector2 p, AnsiColor color = AnsiColor.White)
            {
                renderer[new Vector2(Math.Clamp(p.X, 0f, renderer.Width - 1f), Math.Clamp(p.Y, 0f, renderer.Height - 1f))] = new AnsiChar('.', color);
            }

            void DrawHand(in Buffer21<Vector3> points, AnsiColor color)
            {
                Connect(points, 0, 1, color);
                Connect(points, 1, 2, color);
                Connect(points, 2, 3, color);
                Connect(points, 3, 4, color);

                Connect(points, 0, 5, color);
                Connect(points, 0, 17, color);

                Connect(points, 5, 9, color);
                Connect(points, 9, 13, color);
                Connect(points, 13, 17, color);

                Connect(points, 5, 6, color);
                Connect(points, 6, 7, color);
                Connect(points, 7, 8, color);

                Connect(points, 9, 10, color);
                Connect(points, 10, 11, color);
                Connect(points, 11, 12, color);

                Connect(points, 13, 14, color);
                Connect(points, 14, 15, color);
                Connect(points, 15, 16, color);

                Connect(points, 17, 18, color);
                Connect(points, 18, 19, color);
                Connect(points, 19, 20, color);
            }

            if (false && faceDataReceiver.RightHand.Time != 0)
            {
                var right = Vector3.Normalize(faceDataReceiver.RightHand.Points[17] - faceDataReceiver.RightHand.Points[5]);
                var middle = (faceDataReceiver.RightHand.Points[17] + faceDataReceiver.RightHand.Points[5]) * 0.5f;
                var forward = Vector3.Normalize(middle - faceDataReceiver.RightHand.Points[0]);

                static Matrix3x3 ConstructRotationMatrix(Vector3 forward, Vector3 right)
                {
                    Vector3 up = Vector3.Cross(right, forward);
                    return new Matrix3x3(right.X, right.Y, right.Z, up.X, up.Y, up.Z, forward.X, forward.Y, forward.Z);
                }

                static Matrix3x3 ComputeRotationDerivative(Matrix3x3 R1, Matrix3x3 R2, float deltaTime)
                {
                    return (R2 - R1) * (1f / deltaTime);
                }

                static Vector3 ComputeAngularVelocity(Matrix3x3 dR, Matrix3x3 R)
                {
                    Matrix3x3 Omega = dR * R.Transpose();
                    return new Vector3(
                        (Omega[3, 2] - Omega[2, 3]) / 2.0f,
                        (Omega[1, 3] - Omega[3, 1]) / 2.0f,
                        (Omega[2, 1] - Omega[1, 2]) / 2.0f
                    );
                }

                const float deltaTime = 0.01f;

                Matrix3x3 R1 = ConstructRotationMatrix(lastHandForward, lastHandRight);
                Matrix3x3 R2 = ConstructRotationMatrix(forward, right);

                Matrix3x3 dR = ComputeRotationDerivative(R1, R2, deltaTime);
                Vector3 handAngularVelocity = ComputeAngularVelocity(dR, R1);
                avatarParameters["/input/SpinHoldUD"] = handAngularVelocity.X; // forward backwards
                avatarParameters["/input/SpinHoldLR"] = handAngularVelocity.Y; // left right

                if (handAngularVelocity != default) Console.WriteLine(handAngularVelocity);

                lastHandForward = forward;
                lastHandRight = right;
            }

            if (faceDataReceiver.Face.Time < prevTime) continue;
            prevTime = faceDataReceiver.Face.Time;

            var resolution = new Vector2(renderer.Width, renderer.Height);

            if (Render) renderer.Clear();

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

            var headUp = Vector3.Normalize(faceDataReceiver.Face.Data.Points[HEAD_UP] - faceDataReceiver.Face.Data.Points[HEAD_DOWN]);
            var headRight = Vector3.Normalize(faceDataReceiver.Face.Data.Points[HEAD_RIGHT] - faceDataReceiver.Face.Data.Points[HEAD_LEFT]);
            var headForward = Vector3.Normalize(Vector3.Cross(headUp, headRight));

            Vector3 correctForward = new(0f, 0f, -1f);
            Vector3 axis = Vector3.Cross(average.V, correctForward);
            axis = Vector3.Normalize(axis);
            var correctedHeadForward = float.IsNaN(axis.X) ? headForward : Utils.RotateVector(headForward, axis, -MathF.Acos(Vector3.Dot(average.V, correctForward)));

            if (MathF.Acos(Vector3.Dot(lerpedHeadForward, correctedHeadForward)) < 0.1f)
            {
                lerpedHeadForward = Utils.Slerp(lerpedHeadForward, k.Apply(correctedHeadForward), 0.1f);
            }
            else
            {
                lerpedHeadForward = Utils.Slerp(lerpedHeadForward, correctedHeadForward, 0.3f);
                k = new KalmanFilter();
            }

            if (average.N < ForwardCalibrationIterations) average.V = Utils.Slerp(average.V, headForward, 1f / ++average.N);
            else if (average.N == ForwardCalibrationIterations)
            {
                Console.WriteLine($"Forward direction calibrated");
                average.N = ForwardCalibrationIterations + 1;
            }

            if (Render)
            {
                static Vector2 Project(Vector3 v) => new Vector2(v.X, v.Y) * 1f / -v.Z;

                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + Project(headForward) * resolution,
                    (AnsiColor)Ansi.ToAnsi256(80, 80, 80));

                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + (Project(correctedHeadForward) * resolution),
                    (AnsiColor)Ansi.ToAnsi256(150, 150, 150));

                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + (Project(lerpedHeadForward) * resolution),
                    AnsiColor.Red);

                AnsiRendererExtendedExtensions.LineBarille(renderer,
                    headCenter,
                    headCenter + (Project(average.V) * resolution),
                    AnsiColor.Blue);

                renderer.FillCircle((Vector2Int)(new Vector2(faceDataReceiver.Face.Data.LeftEyeCenter.X, faceDataReceiver.Face.Data.LeftEyeCenter.Y) * resolution), (int)faceDataReceiver.Face.Data.LeftEyeRadius, AnsiColor.Green);
                renderer.FillCircle((Vector2Int)(new Vector2(faceDataReceiver.Face.Data.RightEyeCenter.X, faceDataReceiver.Face.Data.RightEyeCenter.Y) * resolution), (int)faceDataReceiver.Face.Data.RightEyeRadius, AnsiColor.Yellow);

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
