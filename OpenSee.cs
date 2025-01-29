using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Extracted (and refactored) from https://github.com/emilianavt/OpenSeeFace/blob/master/Unity/OpenSee.cs

namespace OpenSee;

file static class Deserializer
{
    public static Vector3 SwapX(Vector3 vector)
    {
        vector.X = -vector.X;
        return vector;
    }

    public static float ReadFloat(byte[] buffer, ref int offset)
    {
        float v = BitConverter.ToSingle(buffer, offset);
        offset += 4;
        return v;
    }

    public static Quaternion ReadQuaternion(byte[] buffer, ref int offset)
    {
        float x = ReadFloat(buffer, ref offset);
        float y = ReadFloat(buffer, ref offset);
        float z = ReadFloat(buffer, ref offset);
        float w = ReadFloat(buffer, ref offset);
        return new Quaternion(x, y, z, w);
    }

    public static Vector3 ReadVector3(byte[] buffer, ref int offset) => new(ReadFloat(buffer, ref offset), -ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset));

    public static Vector2 ReadVector2(byte[] buffer, ref int offset) => new(ReadFloat(buffer, ref offset), ReadFloat(buffer, ref offset));
}

[InlineArray(OpenSeeReceiver.PointCount)]
public struct Buffer1<T>
{
    T _element0;

    [UnscopedRef]
    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _element0, 10);

    [UnscopedRef]
    public readonly ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _element0), 10);
}

[InlineArray(OpenSeeReceiver.PointCount + 2)]
public struct Buffer2<T>
{
    T _element0;

    [UnscopedRef]
    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _element0, 10);

    [UnscopedRef]
    public readonly ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _element0), 10);
}

public struct FaceFeatures
{
    /// <summary>
    /// This field indicates whether the left eye is opened(0) or closed (-1). A value of 1 means open wider than normal.
    /// </summary>
    public float EyeLeft;
    /// <summary>
    /// This field indicates whether the right eye is opened(0) or closed (-1). A value of 1 means open wider than normal.
    /// </summary>
    public float EyeRight;
    /// <summary>
    /// This field indicates how steep the left eyebrow is, compared to the median steepness.
    /// </summary>
    public float EyebrowSteepnessLeft;
    /// <summary>
    /// This field indicates how far up or down the left eyebrow is, compared to its median position.
    /// </summary>
    public float EyebrowUpDownLeft;
    /// <summary>
    /// This field indicates how quirked the left eyebrow is, compared to its median quirk.
    /// </summary>
    public float EyebrowQuirkLeft;
    /// <summary>
    /// This field indicates how steep the right eyebrow is, compared to the average steepness.
    /// </summary>
    public float EyebrowSteepnessRight;
    /// <summary>
    /// This field indicates how far up or down the right eyebrow is, compared to its median position.
    /// </summary>
    public float EyebrowUpDownRight;
    /// <summary>
    /// This field indicates how quirked the right eyebrow is, compared to its median quirk.
    /// </summary>
    public float EyebrowQuirkRight;
    /// <summary>
    /// This field indicates how far up or down the left mouth corner is, compared to its median position.
    /// </summary>
    public float MouthCornerUpDownLeft;
    /// <summary>
    /// This field indicates how far in or out the left mouth corner is, compared to its median position.
    /// </summary>
    public float MouthCornerInOutLeft;
    /// <summary>
    /// This field indicates how far up or down the right mouth corner is, compared to its median position.
    /// </summary>
    public float MouthCornerUpDownRight;
    /// <summary>
    /// This field indicates how far in or out the right mouth corner is, compared to its median position.
    /// </summary>
    public float MouthCornerInOutRight;
    /// <summary>
    /// This field indicates how open or closed the mouth is, compared to its median pose.
    /// </summary>
    public float MouthOpen;
    /// <summary>
    /// This field indicates how wide the mouth is, compared to its median pose.
    /// </summary>
    public float MouthWide;

    public void FromPacket(byte[] buffer, ref int offset)
    {
        EyeLeft = Deserializer.ReadFloat(buffer, ref offset);
        EyeRight = Deserializer.ReadFloat(buffer, ref offset);
        EyebrowSteepnessLeft = Deserializer.ReadFloat(buffer, ref offset);
        EyebrowUpDownLeft = Deserializer.ReadFloat(buffer, ref offset);
        EyebrowQuirkLeft = Deserializer.ReadFloat(buffer, ref offset);
        EyebrowSteepnessRight = Deserializer.ReadFloat(buffer, ref offset);
        EyebrowUpDownRight = Deserializer.ReadFloat(buffer, ref offset);
        EyebrowQuirkRight = Deserializer.ReadFloat(buffer, ref offset);
        MouthCornerUpDownLeft = Deserializer.ReadFloat(buffer, ref offset);
        MouthCornerInOutLeft = Deserializer.ReadFloat(buffer, ref offset);
        MouthCornerUpDownRight = Deserializer.ReadFloat(buffer, ref offset);
        MouthCornerInOutRight = Deserializer.ReadFloat(buffer, ref offset);
        MouthOpen = Deserializer.ReadFloat(buffer, ref offset);
        MouthWide = Deserializer.ReadFloat(buffer, ref offset);
    }
}

public struct Face
{
    /// <summary>
    /// The time this tracking data was captured at.
    /// </summary>
    public double Time;
    /// <summary>
    /// This is the id of the tracked face. When tracking multiple faces, they might get reordered due to faces coming and going, but as long as tracking is not lost on a face, its id should stay the same. Face ids depend only on the order of first detection and locations of the faces.
    /// </summary>
    public int Id;
    /// <summary>
    /// This field gives the resolution of the camera or video being tracked.
    /// </summary>
    public Vector2 CameraResolution;
    /// <summary>
    /// This field tells you how likely it is that the right eye is open.
    /// </summary>
    public float RightEyeOpen;
    /// <summary>
    /// This field tells you how likely it is that the left eye is open.
    /// </summary>
    public float LeftEyeOpen;
    /// <summary>
    /// This field contains the rotation of the right eyeball.
    /// </summary>
    public Vector3 RightGaze;
    /// <summary>
    /// This field contains the rotation of the left eyeball.
    /// </summary>
    public Vector3 LeftGaze;
    /// <summary>
    /// This field tells you if 3D points have been successfully estimated from the 2D points. If this is false, do not rely on pose or 3D data.
    /// </summary>
    public bool Got3DPoints;
    /// <summary>
    /// This field contains the error for fitting the original 3D points. It shouldn't matter much, but it it is very high, something is probably wrong
    /// </summary>
    public float Fit3DError;
    /// <summary>
    /// This is the rotation vector for the 3D points to turn into the estimated face pose.
    /// </summary>
    public Vector3 Rotation;
    /// <summary>
    /// This is the translation vector for the 3D points to turn into the estimated face pose.
    /// </summary>
    public Vector3 Translation;
    /// <summary>
    /// This is the raw rotation quaternion calculated from the OpenCV rotation matrix. It does not match Unity's coordinate system, but it still might be useful.
    /// </summary>
    public Quaternion RawQuaternion;
    /// <summary>
    /// This is the raw rotation euler angles calculated by OpenCV from the rotation matrix. It does not match Unity's coordinate system, but it still might be useful.
    /// </summary>
    public Vector3 RawEuler;
    /// <summary>
    /// This field tells you how certain the tracker is.
    /// </summary>
    public Buffer1<float> Confidence;
    /// <summary>
    /// These are the detected face landmarks in image coordinates. There are 68 points. The last too points are pupil points from the gaze tracker.
    /// </summary>
    public Buffer1<Vector2> Points;
    /// <summary>
    /// These are 3D points estimated from the 2D points. The should be rotation and translation compensated. There are 70 points with guesses for the eyeball center positions being added at the end of the 68 2D points.
    /// </summary>
    public Buffer2<Vector3> Points3D;
    /// <summary>
    /// This field contains a number of action unit like features.
    /// </summary>
    public FaceFeatures Features;

    public void FromPacket(byte[] buffer, ref int offset)
    {
        Time = BitConverter.ToDouble(buffer, offset);
        offset += 8;
        Id = BitConverter.ToInt32(buffer, offset);
        offset += 4;

        CameraResolution = Deserializer.ReadVector2(buffer, ref offset);
        RightEyeOpen = Deserializer.ReadFloat(buffer, ref offset);
        LeftEyeOpen = Deserializer.ReadFloat(buffer, ref offset);

        byte got3D = buffer[offset];
        offset++;
        Got3DPoints = false;
        if (got3D != 0) Got3DPoints = true;

        Fit3DError = Deserializer.ReadFloat(buffer, ref offset);
        RawQuaternion = Deserializer.ReadQuaternion(buffer, ref offset);
        // Quaternion convertedQuaternion = new(-rawQuaternion.X, rawQuaternion.Y, -rawQuaternion.Z, rawQuaternion.W);
        RawEuler = Deserializer.ReadVector3(buffer, ref offset);

        Rotation = RawEuler;
        Rotation.Z = (Rotation.Z - 90) % 360;
        Rotation.X = -(Rotation.X + 180) % 360;

        float x = Deserializer.ReadFloat(buffer, ref offset);
        float y = Deserializer.ReadFloat(buffer, ref offset);
        float z = Deserializer.ReadFloat(buffer, ref offset);
        Translation = new Vector3(-y, x, -z);

        for (int i = 0; i < OpenSeeReceiver.PointCount; i++)
        {
            Confidence[i] = Deserializer.ReadFloat(buffer, ref offset);
        }

        for (int i = 0; i < OpenSeeReceiver.PointCount; i++)
        {
            Points[i] = Deserializer.ReadVector2(buffer, ref offset);
        }

        for (int i = 0; i < OpenSeeReceiver.PointCount + 2; i++)
        {
            Points3D[i] = Deserializer.ReadVector3(buffer, ref offset);
        }

        RightGaze = Vector3.Normalize(Deserializer.SwapX(Points3D[68]) - Deserializer.SwapX(Points3D[66]));
        LeftGaze = Vector3.Normalize(Deserializer.SwapX(Points3D[69]) - Deserializer.SwapX(Points3D[67]));

        Features.FromPacket(buffer, ref offset);
    }
}

public class OpenSeeReceiver : IDisposable
{
    public const int PointCount = 68;
    public const int PacketFrameSize = 8 + 4 + 2 * 4 + 2 * 4 + 1 + 4 + 3 * 4 + 3 * 4 + 4 * 4 + 4 * 68 + 4 * 2 * 68 + 4 * 3 * 70 + 4 * 14;

    public ref readonly Face CurrentFace => ref _face;

    Face _face;
    Thread _thread;
    bool _shouldRun;
    bool _isDisposed;

    public OpenSeeReceiver()
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
        using UdpClient listener = new(11573, AddressFamily.InterNetwork);
        EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        byte[] _buffer = new byte[PacketFrameSize];

        while (_shouldRun)
        {
            int received = listener.Client.ReceiveFrom(_buffer, PacketFrameSize, 0, ref ep);

            int offset = 0;
            _face.FromPacket(_buffer, ref offset);
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
