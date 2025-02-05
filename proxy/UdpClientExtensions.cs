using System.Net;
using System.Net.Sockets;

namespace VRChatProxy;

static class UdpClientExtensions
{
    public static int Receive(this UdpClient client, byte[] buffer, int size, ref EndPoint remoteEP)
        => client.Client.ReceiveFrom(buffer, size, SocketFlags.None, ref remoteEP);

    public static int Receive(this UdpClient client, byte[] buffer, ref EndPoint remoteEP)
        => client.Client.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref remoteEP);

    public static int Receive(this UdpClient client, Span<byte> buffer, ref EndPoint remoteEP)
        => client.Client.ReceiveFrom(buffer, SocketFlags.None, ref remoteEP);
}