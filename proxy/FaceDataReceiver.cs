using System.IO.Pipes;

public class FaceDataReceiver : IDisposable
{
    public FaceData CurrentFace;

    Thread _thread;
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
        using NamedPipeClientStream listener = new(".", "mySocket", PipeDirection.In);
        byte[] _buffer = new byte[sizeof(FaceData)];

        listener.Connect();

        while (_shouldRun)
        {
            int received = listener.Read(_buffer, 0, _buffer.Length);
            if (_buffer.Length < sizeof(FaceData))
            {
                Thread.Sleep(500);
                continue;
            }
            fixed (void* _ptr = _buffer)
            {
                CurrentFace = *(FaceData*)_ptr;
            }
        }

        _shouldRun = false;
        _isDisposed = true;
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
