using System.IO.Pipes;

namespace VRChatProxy;

public class FaceDataReceiver : IDisposable
{
    public FaceData CurrentFace;

    readonly Thread _thread;
    bool _shouldRun;
    bool _isDisposed;

    public FaceDataReceiver()
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
        byte[] _buffer = new byte[sizeof(FaceData)];

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

                    // if (received != sizeof(FaceData))
                    // {
                    //     Console.WriteLine($"[UDP] Skipping invalid frame");
                    //     Thread.Sleep(500);
                    //     continue;
                    // }

                    fixed (void* _ptr = _buffer)
                    {
                        CurrentFace = *(FaceData*)_ptr;
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
