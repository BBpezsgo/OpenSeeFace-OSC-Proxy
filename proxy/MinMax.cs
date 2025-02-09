namespace VRChatProxy;

public struct MinMax(float initialMin, float initialMax)
{
    public float Min { get; private set; } = initialMin;
    public float Max { get; private set; } = initialMax;

    public void Record(float value)
    {
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }
}
