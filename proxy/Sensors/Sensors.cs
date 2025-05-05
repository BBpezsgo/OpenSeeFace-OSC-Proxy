using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace VRChatProxy;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GenericSensorData))]
[JsonSerializable(typeof(SensorData))]
[JsonSerializable(typeof(ImmutableArray<float>))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(int))]
public partial class SourceGenerationContext : JsonSerializerContext { }

public class SensorData
{
    [JsonPropertyName("type")] public required string Type { get; init; }
}

public class GenericSensorData
{
    [JsonPropertyName("timestamp")] public required long Timestamp { get; init; }
    [JsonPropertyName("values")] public required ImmutableArray<float> Values { get; init; }
    [JsonPropertyName("accuracy")] public required int Accuracy { get; init; }
}
