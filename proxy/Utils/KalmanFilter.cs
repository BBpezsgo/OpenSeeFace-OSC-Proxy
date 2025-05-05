using System.Numerics;
using Maths;

namespace VRChatProxy;

struct KalmanFilter
{
    Vector3? x_k;
    Matrix3x3 P_k;
    Matrix3x3 Q;
    Matrix3x3 R;
    Matrix3x3 K;

    public readonly Vector3 Value => x_k!.Value;

    public KalmanFilter()
    {
        x_k = null;
        P_k = Matrix3x3.Identity * 0.1f;
        Q = Matrix3x3.Identity * 0.001f;
        R = Matrix3x3.Identity * 0.05f;
        K = new Matrix3x3();
    }

    public Vector3 Apply(Vector3 measurement)
    {
        if (!x_k.HasValue)
        {
            return (x_k = measurement).Value;
        }

        P_k += Q;

        K = P_k * (P_k + R).Inverse();
        x_k += K * (measurement - x_k);
        P_k = (Matrix3x3.Identity - K) * P_k;

        return x_k.Value;
    }
}
