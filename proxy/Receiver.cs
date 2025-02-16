using System.IO.Pipes;
using System.Runtime.InteropServices;
using Maths;

namespace VRChatProxy;

public class Receiver : IDisposable
{
    public enum PacketType : int
    {
        Face = 1,
        Hands = 2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Header
    {
        public readonly double Time;
        public readonly int Width;
        public readonly int Height;
        public readonly PacketType Type;
    }

    public Vector2Int CameraResolution;
    public Face Face;
    public Hand LeftHand;
    public Hand RightHand;

    readonly Thread _thread;
    bool _shouldRun;
    bool _isDisposed;

    unsafe static readonly int MaxPacketSize = sizeof(Header) + Math.Max(sizeof(FaceData), sizeof(int) + sizeof(HandData) * 2);

    public Receiver()
    {
        _shouldRun = true;
        _thread = new(Worker);
    }

    public void Start()
    {
        _thread.Start();
    }

    unsafe void Worker()
    {
        NamedPipeClientStream? listener = null;
        byte[] _buffer = new byte[MaxPacketSize];

        try
        {
            while (_shouldRun)
            {
                Console.WriteLine($"[UDP] Connecting ...");
                listener = new NamedPipeClientStream(".", "mySocket", PipeDirection.In);
                listener.Connect();
                Console.WriteLine($"[UDP] Connected");

                while (_shouldRun)
                {
                    int received = listener.Read(_buffer, 0, _buffer.Length);

                    if (received == 0)
                    {
                        listener.Close();
                        listener.Dispose();
                        listener = null;
                        Console.WriteLine($"[UDP] Disconnected");
                        break;
                    }

                    Reader reader = new(_buffer.AsSpan(0, received));

                    fixed (void* _header = _buffer)
                    {
                        Header* header = reader.Read<Header>();
                        CameraResolution = new Vector2Int(header->Width, header->Height);

                        switch (header->Type)
                        {
                            case PacketType.Face:
                                FaceData* face = reader.Read<FaceData>();
                                Face = new Face(header->Time, *face);
                                break;
                            case PacketType.Hands:
                                int handCount = *reader.Read<int>();
                                if (handCount == 2)
                                {
                                    HandData* handA = reader.Read<HandData>();
                                    HandData* handB = reader.Read<HandData>();
                                    if (handA->HandednessIndex == 1 &&
                                        handB->HandednessIndex == 1)
                                    {
                                        if (handA->HandednessScore > handB->HandednessScore)
                                        {
                                            RightHand = new Hand(header->Time, handA->Points);
                                            LeftHand = new Hand(header->Time, handB->Points);
                                        }
                                        else
                                        {
                                            RightHand = new Hand(header->Time, handB->Points);
                                            LeftHand = new Hand(header->Time, handA->Points);
                                        }
                                    }
                                    else if (handA->HandednessIndex == 0 &&
                                             handB->HandednessIndex == 0)
                                    {
                                        if (handA->HandednessScore > handB->HandednessScore)
                                        {
                                            LeftHand = new Hand(header->Time, handA->Points);
                                            RightHand = new Hand(header->Time, handB->Points);
                                        }
                                        else
                                        {
                                            LeftHand = new Hand(header->Time, handB->Points);
                                            RightHand = new Hand(header->Time, handA->Points);
                                        }
                                    }
                                    else
                                    {
                                        if (handA->HandednessIndex == 0)
                                        {
                                            LeftHand = new Hand(header->Time, handA->Points);
                                            RightHand = new Hand(header->Time, handB->Points);
                                        }
                                        else
                                        {
                                            RightHand = new Hand(header->Time, handA->Points);
                                            LeftHand = new Hand(header->Time, handB->Points);
                                        }
                                    }
                                }
                                else if (handCount == 1)
                                {
                                    HandData* hand = reader.Read<HandData>();
                                    if (hand->HandednessIndex == 0)
                                    {
                                        LeftHand = new Hand(header->Time, hand->Points);
                                    }
                                    else
                                    {
                                        RightHand = new Hand(header->Time, hand->Points);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
        finally
        {
            listener?.Dispose();
            _shouldRun = false;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _shouldRun = false;
        _isDisposed = true;
        GC.SuppressFinalize(this);

        _thread.Join();
    }
}
