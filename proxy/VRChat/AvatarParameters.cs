using System.Collections.Concurrent;
using SharpOSC;

namespace VRChatProxy;

class AvatarParameter<T>
{
    public T SyncedValue;
    public T DesiredValue;

    public AvatarParameter(T syncedValue, T desiredValue)
    {
        SyncedValue = syncedValue;
        DesiredValue = desiredValue;
    }
}

public struct AvatarParameters
{
    readonly ConcurrentDictionary<string, AvatarParameter<object?>> _parameters;

    public object? this[string address]
    {
        readonly get => _parameters[address].SyncedValue;
        set => _parameters[address] = _parameters.TryGetValue(address, out var present) ? new(present.SyncedValue, value) : new(default, value);
    }

    public AvatarParameters()
    {
        _parameters = new();
    }

    public void HandlePacket(IOscPacket? packet)
    {
        switch (packet)
        {
            case OscBundle v:
                HandlePacket(v);
                break;
            case OscMessage v:
                HandlePacket(v);
                break;
        }
    }

    public void HandlePacket(OscBundle packet)
    {
        for (int i = 0; i < packet.Messages.Length; i++)
        {
            HandlePacket(packet.Messages[i]);
        }
    }

    public void HandlePacket(OscMessage packet)
    {
        if (packet.Arguments.Length != 1) return;
        object? value = packet.Arguments[0];

        if (!_parameters.TryGetValue(packet.Address, out var parameter))
        {
            parameter = new AvatarParameter<object?>(value, value);
        }

        parameter.DesiredValue = value;
        parameter.SyncedValue = value;

        _parameters[packet.Address] = parameter;
    }

    public void Sync(UDPSender oscSender)
    {
        foreach ((string address, AvatarParameter<object?> value) in _parameters)
        {
            if (OscValueComparer.Instance.Equals(value.SyncedValue, value.DesiredValue)) continue;

            OscValueComparer.Instance.Equals(value.SyncedValue, value.DesiredValue);

            value.SyncedValue = value.DesiredValue;
            // Console.WriteLine($"[OSC] {address} ==> {value.DesiredValue}");
            oscSender.Send(new OscMessage(address, value.DesiredValue));
        }
    }
}
