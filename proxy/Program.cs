using SharpOSC;
using System.Collections.Immutable;
using System.Numerics;
using CLI;
using Maths;

namespace VRChatProxy;

static class Program
{
    const bool Render = true;
    const int ForwardCalibrationIterations = 50;

    static bool SignalInterrupt = false;

    static int Main(string[] args)
    {
        AnsiRendererExtended renderer = new();

        if (false)
        {
            using CancellationTokenSource source = new();
            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                SignalInterrupt = true;
                source.Cancel();
            };

            SensorClient sensorClient = new();
            Task sensorClientTask = sensorClient.Run(
            [
                Sensors.Orientation,
            ], source.Token);

            Vector3 position = default;
            Vector3 velocity = default;
            Vector3 gravity = default;
            Vector3 acceleration = default;
            long lastAccelerometer = 0;

            while (!SignalInterrupt)
            {
                Thread.Sleep(100);
                Console.WriteLine($"{sensorClient.Orientation.Value}");
                continue;

                while (sensorClient.Accelerometer.Records.TryDequeue(out SensorRecord<Vector3> _accelerometer))
                {
                    while (sensorClient.Gravity.Records.TryDequeue(out SensorRecord<Vector3> _gravity))
                    {
                        gravity = _gravity.Value;
                        if (gravity == default || _gravity.Timestamp >= _accelerometer.Timestamp) break;
                    }
                    acceleration = _accelerometer.Value - gravity;

                    if (lastAccelerometer == 0)
                    {
                        lastAccelerometer = _accelerometer.Timestamp;
                    }
                    else
                    {
                        float deltaTime = (_accelerometer.Timestamp - lastAccelerometer) * 1E-9f;
                        if (deltaTime < 0f) continue;
                        lastAccelerometer = _accelerometer.Timestamp;
                        if (Math.Abs(acceleration.X) < 0.1f) acceleration.X = 0f;
                        if (Math.Abs(acceleration.Y) < 0.1f) acceleration.Y = 0f;
                        if (Math.Abs(acceleration.Z) < 0.1f) acceleration.Z = 0f;

                        if (acceleration == default)
                        {
                            velocity = default;
                        }
                        else
                        {
                            velocity += (acceleration - gravity) * deltaTime;
                        }
                        position += velocity * deltaTime;
                    }
                }
                Console.WriteLine($"{position.X:F1} {position.Y:F1} {position.Z:F1}");

                Vector2 center = new Vector2(renderer.Width, renderer.Height) / 2f;
                renderer.LineBarille(
                    center,
                    center + (sensorClient.Gyroscope.Value * 10f).To2D(),
                    AnsiColor.White
                );

                renderer.Render();
                renderer.Clear();
            }

            sensorClientTask.Wait();
            return 0;
        }

        /*
        {
            Console.WriteLine($"[ZeroConf] Discovering servers ...");
            IReadOnlyList<IZeroconfHost> zerconfResult = await ZeroconfResolver.ResolveAsync("_websocket._tcp.local.", cancellationToken: source.Token);
            Console.WriteLine($"[ZeroConf] Discovered servers:");
            foreach (var item in zerconfResult)
            {
                Console.WriteLine($"  {item.Id} - {string.Join(" ", item.IPAddresses)} with name {item.DisplayName}");
                Console.WriteLine($"  Services:");
                foreach (var service in item.Services)
                {
                    Console.WriteLine($"    {service.Key} {service.Value.ServiceName} ({service.Value.Name}) on port {service.Value.Port} with ttl {service.Value.Ttl}");
                    foreach (var _properties in service.Value.Properties)
                    {
                        foreach (var _property in _properties)
                        {
                            Console.WriteLine($"      {_property.Key} {_property.Value}");
                        }
                    }
                }
            }

            if (zerconfResult.Count == 0)
            {
                Console.WriteLine("No sensor servers found");
                return 1;
            }

            IZeroconfHost first = zerconfResult[0];

            if (first.Services.Count != 1)
            {
                Console.WriteLine("Server has multiple services");
                return 1;
            }

            using ClientWebSocket ws = new();
            await ws.ConnectAsync(new Uri($"ws://{first.IPAddress}:{first.Services.First().Value.Port}/sensor/connect?type=android.sensor.accelerometer"), source.Token);
            while (true)
            {
                await Task.Delay(100, source.Token);
                if (SignalInterrupt)
                {
                    Console.WriteLine($"[WS] Closing connection ...");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "SIGINT", source.Token);
                    Console.WriteLine($"[WS] Connection closed");
                }

                Memory<byte> bytes = new byte[1024];
                ValueWebSocketReceiveResult result = await ws.ReceiveAsync(bytes, source.Token);
                bytes = bytes[..result.Count];
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        Accelerometer acc = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.Accelerometer)!;

                        break;
                    case WebSocketMessageType.Binary:
                        break;
                    case WebSocketMessageType.Close:
                        goto close;
                }
            }
        close:
            return 0;
        }
        */

        AvatarParameters avatarParameters = new();
        using UDPListener oscListener = new(9001, packet => avatarParameters.HandlePacket(packet));
        using UDPSender oscSender = new("127.0.0.1", 9000);
        using Receiver faceDataReceiver = new();
        faceDataReceiver.Start();

        ImmutableArray<string> expressionLabels = File.ReadAllLines("/home/BB/Projects/OpenSeeFace-OSC-Proxy/server/model/keypoint_classifier/keypoint_classifier_label.csv").ToImmutableArray();

        Vector3 lerpedHeadForward = new(0f, 0f, 1f);
        Dirty<Vector3> dirtyFaceForward = new((a, b) => Vector3.Distance(a, b) > 0.01f, new Vector3(0f, 0f, 1f));

        Vector3 lastHandForward = default;
        Vector3 lastHandRight = default;
        Vector3 lastHandRotation = default;

        (Vector3 V, int N) average = (lerpedHeadForward, 0);

        double prevFaceTime = 1d;
        double prevRightHandTime = 1d;
        double prevLeftHandTime = 1d;
        double prevHolisticTime = 1d;

        KalmanFilter k = new();
        KalmanFilter k1 = new();
        KalmanFilter k2 = new();
        KalmanFilter k3 = new();

        Camera camera = new(
            new Vector3(0.5f, 0.5f, -0.7f),
            (float)(renderer.Width * 2f) / (float)(renderer.Height * 6f),
            MathF.PI / 3f
        );

        Pinch leftPinchIndex = new(HandLandmarks.THUMB_TIP, HandLandmarks.INDEX_FINGER_TIP);

        Pinch rightPinchIndex = new(HandLandmarks.THUMB_TIP, HandLandmarks.INDEX_FINGER_TIP);
        Pinch rightPinchMiddle = new(HandLandmarks.THUMB_TIP, HandLandmarks.MIDDLE_FINGER_TIP);
        Pinch rightPinchRing = new(HandLandmarks.THUMB_TIP, HandLandmarks.RING_FINGER_TIP);
        Pinch rightPinchPinky = new(HandLandmarks.THUMB_TIP, HandLandmarks.PINKY_TIP);

        while (true)
        {
            Thread.Sleep(50);
            double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Vector2 resolution = new(renderer.Width, renderer.Height);
            if (Render) renderer.Clear();

            const double MaxLife = 1d;

            if (faceDataReceiver.Holistic.Time >= prevHolisticTime)
            {
                if (Render  && now - faceDataReceiver.Holistic.Time < MaxLife)
                {
                    renderer.RenderHolistic(faceDataReceiver.Holistic.Data, camera);
                }
                prevHolisticTime = faceDataReceiver.Holistic.Time;
            }

            if (faceDataReceiver.RightHand.Time >= prevRightHandTime)
            {
                if (Render && now - faceDataReceiver.RightHand.Time < MaxLife)
                {
                    renderer.RenderHand(faceDataReceiver.RightHand, camera, prevRightHandTime == faceDataReceiver.RightHand.Time ? AnsiColor.Gray : AnsiColor.White);
                }
                prevRightHandTime = faceDataReceiver.RightHand.Time;

                rightPinchIndex.Update(faceDataReceiver.RightHand);
                rightPinchMiddle.Update(faceDataReceiver.RightHand);
                rightPinchRing.Update(faceDataReceiver.RightHand);
                rightPinchPinky.Update(faceDataReceiver.RightHand);

                k1.Apply(faceDataReceiver.RightHand.Points[0]);
                k2.Apply(faceDataReceiver.RightHand.Points[5]);
                k3.Apply(faceDataReceiver.RightHand.Points[17]);

                Vector3 right = Vector3.Normalize(k3.Value - k2.Value);
                Vector3 middle = (k3.Value + k2.Value) * 0.5f;
                Vector3 forward = Vector3.Normalize(middle - k1.Value);

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
                        (Omega[2, 1] - Omega[1, 2]) / 2.0f,
                        (Omega[0, 2] - Omega[2, 0]) / 2.0f,
                        (Omega[1, 0] - Omega[0, 1]) / 2.0f
                    );
                }

                static Vector3 DirectionToEuler(Vector3 v)
                {
                    float yaw = MathF.Atan2(v.X, v.Z); // * (180f / MathF.PI);
                    float pitch = MathF.Asin(-v.Y); // * (180f / MathF.PI);
                    return new(yaw, pitch, 0f);
                }

                Vector3 handRotation = DirectionToEuler(forward);
                if (lastHandRotation != default)
                {
                    //avatarParameters["/input/SpinHoldUD"] = lastHandRotation.X - handRotation.X;
                    //avatarParameters["/input/SpinHoldCwCcw"] = lastHandRotation.Y - handRotation.Y;
                }
                lastHandRotation = handRotation;

                //const float deltaTime = 0.01f;
                //
                //Matrix3x3 R1 = ConstructRotationMatrix(lastHandForward, lastHandRight);
                //Matrix3x3 R2 = ConstructRotationMatrix(forward, right);
                //
                //Matrix3x3 dR = ComputeRotationDerivative(R1, R2, deltaTime);
                //Vector3 handAngularVelocity = ComputeAngularVelocity(dR, R1);
                //avatarParameters["/input/SpinHoldUD"] = -handAngularVelocity.X * 0.2f; // forward backwards
                //avatarParameters["/input/SpinHoldCwCcw"] = -handAngularVelocity.Y * 0.2f;
                //avatarParameters["/input/SpinHoldLR"] = -handAngularVelocity.Z * 0.2f; // left right
                //
                //if (handAngularVelocity != default) Console.WriteLine(handAngularVelocity);

                lastHandForward = forward;
                lastHandRight = right;
            }

            if (faceDataReceiver.LeftHand.Time >= prevLeftHandTime)
            {
                if (Render && now - faceDataReceiver.LeftHand.Time < MaxLife)
                {
                    renderer.RenderHand(faceDataReceiver.LeftHand, camera, prevLeftHandTime == faceDataReceiver.LeftHand.Time ? AnsiColor.Gray : AnsiColor.White);
                }
                prevLeftHandTime = faceDataReceiver.LeftHand.Time;

                leftPinchIndex.Update(faceDataReceiver.LeftHand);
            }

            if (faceDataReceiver.Face.Time >= prevFaceTime)
            {
                if (Render && now - faceDataReceiver.Face.Time < MaxLife)
                {
                    renderer.RenderFace(faceDataReceiver.Face.Data, camera);
                }
                prevFaceTime = faceDataReceiver.Face.Time;

                Vector2Int headCenter = new(renderer.Width / 2, renderer.Height / 2);

                const int HEAD_UP = 10;
                const int HEAD_DOWN = 152;
                const int HEAD_RIGHT = 234;
                const int HEAD_LEFT = 454;

                Vector3 headUp = Vector3.Normalize(faceDataReceiver.Face.Data.Points[HEAD_UP] - faceDataReceiver.Face.Data.Points[HEAD_DOWN]);
                Vector3 headRight = Vector3.Normalize(faceDataReceiver.Face.Data.Points[HEAD_RIGHT] - faceDataReceiver.Face.Data.Points[HEAD_LEFT]);
                Vector3 headForward = Vector3.Normalize(Vector3.Cross(headUp, headRight));

                Vector3 correctForward = new(0f, 0f, -1f);
                Vector3 axis = Vector3.Cross(average.V, correctForward);
                axis = Vector3.Normalize(axis);
                Vector3 correctedHeadForward = float.IsNaN(axis.X) ? headForward : Utils.RotateVector(headForward, axis, -MathF.Acos(Vector3.Dot(average.V, correctForward)));

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

                if (false)
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

                avatarParameters["/avatar/parameters/VF135_Expressions/Angry"] = faceDataReceiver.Face.Data.Expression == 0;
                avatarParameters["/avatar/parameters/VF132_Expressions/Sad"] = faceDataReceiver.Face.Data.Expression == 3;
                avatarParameters["/avatar/parameters/VF129_Expressions/Happy"] = faceDataReceiver.Face.Data.Expression == 1;

                //avatarParameters["/tracking/eye/CenterVec"] = lerpedHeadForward * new Vector3(1f, -1f, -1f);
                //avatarParameters["/tracking/trackers/8/position"] = new Vector3(1f, 1f, 1f) + System.Random.Shared.Direction3() * 0.01f;
                //avatarParameters["/tracking/trackers/8/rotation"] = Utils.Slerp(default, System.Random.Shared.Direction3(), 0.1f);

                //avatarParameters["/avatar/parameters/Viseme"] = DateTime.UtcNow.TimeOfDay.Seconds % 10;
            }

            {
                if (rightPinchIndex.State == PinchState.None)
                {
                    if (now - rightPinchIndex.LastGrabTime < 0.7d)
                    {
                        Vector3 delta = rightPinchIndex.CurrentPinch - rightPinchIndex.OriginalPinch;
                        renderer.RenderLine(rightPinchIndex.CurrentPinch, rightPinchIndex.OriginalPinch, camera, AnsiColor.Gray);
                    }
                }
                else
                {
                    Vector3 delta = rightPinchIndex.CurrentPinch - rightPinchIndex.OriginalPinch;
                    renderer.RenderLine(rightPinchIndex.CurrentPinch, rightPinchIndex.OriginalPinch, camera, rightPinchIndex.State switch
                    {
                        PinchState.None => AnsiColor.Gray,
                        PinchState.Grab => AnsiColor.BrightGreen,
                        PinchState.Hold => AnsiColor.BrightYellow,
                        PinchState.Release => AnsiColor.BrightRed,
                        _ => AnsiColor.Gray,
                    });
                    //avatarParameters["/input/SpinHoldLR"] = Math.Sign((int)(delta.X * 10f));
                }
            }

            //avatarParameters.Sync(oscSender);

            if (Render)
            {
                Console.Clear();
                renderer.Render();
            }
        }
    }
}
