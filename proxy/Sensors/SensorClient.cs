using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Zeroconf;

namespace VRChatProxy;

public readonly struct SensorRecord<T>(long timestamp, int accuracy, T value) where T : notnull
{
    public readonly long Timestamp = timestamp;
    public readonly int Accuracy = accuracy;
    public readonly T Value = value;

    public override string? ToString() => Value.ToString();
}

public struct Sensor<T>(T @default = default!) where T : notnull
{
    public readonly ConcurrentQueue<SensorRecord<T>> Records = new();
    public T Value = @default;

    public override readonly string? ToString() => Value.ToString();
}

public static class Sensors
{
    public const string Accelerometer = "android.sensor.accelerometer";
    public const string Orientation = "android.sensor.orientation";
    public const string Light = "android.sensor.light";
    public const string Proximity = "android.sensor.proximity";
    public const string Gyroscope = "android.sensor.gyroscope";
    public const string Gravity = "android.sensor.gravity";
    public const string LinearAcceleration = "android.sensor.linear_acceleration";
    public const string RotationVector = "android.sensor.rotation_vector";
    public const string Hall = "android.sensor.hall";
    public const string MagneticFieldUncalibrated = "android.sensor.magnetic_field_uncalibrated";
    public const string GameRotationVector = "android.sensor.game_rotation_vector";
    public const string GyroscopeUncalibrated = "android.sensor.gyroscope_uncalibrated";
    public const string StepCounter = "android.sensor.step_counter";
    public const string GeomagneticRotationVector = "android.sensor.geomagnetic_rotation_vector";
    public const string Gesture = "android.sensor.gesture";
    public const string Rpc = "android.sensor.rpc";
}

public class SensorClient
{
    public Sensor<Vector3> Accelerometer = new(default);
    public Sensor<Vector3> Gyroscope = new(default);
    public Sensor<Vector3> Gravity = new(default);
    public Sensor<(Vector3 Rotation, Vector3 EstimatedDrift)> UncalibratedGyroscope = new(default);
    public Sensor<Vector3> LinearAcceleration = new(default);
    public Sensor<(Vector3 Direction, float Scalar)> RotationVector = new(default);
    public Sensor<float> StepCounter = new(default);
    public Sensor<(Vector3 FieldStrength, Vector3 EstimatedIronBias)> UncalibratedMagneticField = new(default);
    public Sensor<Vector3> Orientation = new(default);

    public async Task Run(string[] sensors, CancellationToken cancellationToken = default)
    {
    again:
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"[SensorClient] Discovering servers ...");
                IReadOnlyList<IZeroconfHost> zerconfResult = await ZeroconfResolver.ResolveAsync("_websocket._tcp.local.", cancellationToken: cancellationToken);
                Console.WriteLine($"[SensorClient] Servers discovered");
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
                    Console.WriteLine($"[SensorClient] No servers found, retrying in 1 sec ...");
                    await Task.Delay(1000, cancellationToken);
                    goto again;
                }

                IZeroconfHost first = zerconfResult[0];

                if (first.Services.Count != 1)
                {
                    throw new Exception("Server has multiple services");
                }

                using ClientWebSocket ws = new();
                Uri uri;
                if (sensors.Length == 1)
                {
                    uri = new Uri($"ws://{first.IPAddress}:{first.Services.First().Value.Port}/sensor/connect?type={sensors[0]}");
                }
                else
                {
                    uri = new Uri($"ws://{first.IPAddress}:{first.Services.First().Value.Port}/sensors/connect?types=[{string.Join(',', sensors.Select(v => $"\"{v}\""))}]");
                }
                await ws.ConnectAsync(uri, cancellationToken);
                Console.WriteLine($"[SensorClient] Connected");
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"[SensorClient] Closing connection ...");
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                        Console.WriteLine($"[SensorClient] Connection closed");
                        return;
                    }

                    Memory<byte> bytes = new byte[1024];
                    ValueWebSocketReceiveResult result = await ws.ReceiveAsync(bytes, cancellationToken);
                    bytes = bytes[..result.Count];
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            string sensorType = sensors.Length == 1 ? sensors[0] : JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.SensorData)!.Type;
                            switch (sensorType)
                            {
                                case Sensors.Accelerometer: // m/s^2
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 v = new(data.Values[0], data.Values[1], data.Values[2]);
                                        Accelerometer.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        Accelerometer.Value = v;
                                        break;
                                    }
                                case Sensors.Gravity: // m/s^2
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 v = new(data.Values[0], data.Values[1], data.Values[2]);
                                        Gravity.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        Gravity.Value = v;
                                        break;
                                    }
                                case Sensors.Gyroscope: // rad/s
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 v = new(data.Values[0], data.Values[1], data.Values[2]);
                                        Gyroscope.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        Gyroscope.Value = v;
                                        break;
                                    }
                                case Sensors.GyroscopeUncalibrated: // rad/s
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 rotation = new(data.Values[0], data.Values[1], data.Values[2]);
                                        Vector3 estimatedDrift = new(data.Values[3], data.Values[4], data.Values[5]);
                                        UncalibratedGyroscope.Records.Enqueue(new(data.Timestamp, data.Accuracy, (rotation, estimatedDrift)));
                                        UncalibratedGyroscope.Value = (rotation, estimatedDrift);
                                        break;
                                    }
                                case Sensors.LinearAcceleration: // m/s^2
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 v = new(data.Values[0], data.Values[1], data.Values[2]);
                                        LinearAcceleration.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        LinearAcceleration.Value = v;
                                        break;
                                    }
                                case Sensors.RotationVector: // unitless
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 v = new(data.Values[0], data.Values[1], data.Values[2]);
                                        float scalar = data.Values[3];
                                        RotationVector.Records.Enqueue(new(data.Timestamp, data.Accuracy, (v, scalar)));
                                        RotationVector.Value = (v, scalar);
                                        break;
                                    }
                                case Sensors.StepCounter:
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        float steps = data.Values[0];
                                        StepCounter.Records.Enqueue(new(data.Timestamp, data.Accuracy, steps));
                                        StepCounter.Value = steps;
                                        break;
                                    }

                                case Sensors.GameRotationVector:
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector4 v = new(data.Values[0], data.Values[1], data.Values[2], data.Values[3]);
                                        // Gravity.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        break;
                                    }
                                case Sensors.GeomagneticRotationVector:
                                    {
                                        // 5
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        //  v = new(data.Values[0], data.Values[1], data.Values[2], data.Values[3]);
                                        // Gravity.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        break;
                                    }
                                case Sensors.MagneticFieldUncalibrated:
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 uncalibratedFieldStrength = new(data.Values[0], data.Values[1], data.Values[2]);
                                        Vector3 estimatedIronBias = new(data.Values[3], data.Values[4], data.Values[5]);
                                        UncalibratedMagneticField.Records.Enqueue(new(data.Timestamp, data.Accuracy, (uncalibratedFieldStrength, estimatedIronBias)));
                                        break;
                                    }
                                case Sensors.Orientation:
                                    {
                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
                                        Vector3 v = new(data.Values[0], data.Values[1], data.Values[2]);
                                        Orientation.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
                                        Orientation.Value = v;
                                        break;
                                    }
//                                case Sensors.Proximity:
//                                    {
//                                        // 3
//                                        GenericSensorData data = JsonSerializer.Deserialize(bytes.Span, SourceGenerationContext.Default.GenericSensorData)!;
//                                        //  v = new(data.Values[0], data.Values[1], data.Values[2], data.Values[3]);
//                                        // Gravity.Records.Enqueue(new(data.Timestamp, data.Accuracy, v));
//                                        break;
//                                    }

                                default:
                                    Console.WriteLine(sensorType);
                                    Console.WriteLine(Encoding.UTF8.GetString(bytes.Span));
                                    break;
                            }
                            break;
                        case WebSocketMessageType.Binary:
                            break;
                        case WebSocketMessageType.Close:
                            Console.WriteLine($"[SensorClient] Connection closed: {ws.CloseStatusDescription}");
                            return;
                    }
                }
            }
            catch (WebSocketException ex)
            {
                if (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    Console.WriteLine($"[SensorClient] Connection closed ...");
                    goto again;
                }
                throw;
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
