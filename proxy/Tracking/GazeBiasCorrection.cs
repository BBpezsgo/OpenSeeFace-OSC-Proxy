using System.Numerics;

namespace VRChatProxy;

class GazeBiasCorrection
{
    List<Vector3> biasSamples = new();
    int biasSampleCount = 50;  // Number of samples to collect for bias correction
    Quaternion biasCorrection = Quaternion.Identity;  // Default: No correction
    bool biasComputed = false;

    public void CollectBiasSample(Vector3 rawRotationVector)
    {
        if (biasComputed) return;  // Bias already computed, no need to collect more samples

        biasSamples.Add(rawRotationVector);
        if (biasSamples.Count >= biasSampleCount)
        {
            ComputeBias();
        }
    }

    void ComputeBias()
    {
        // Compute the average rotation vector
        Vector3 avgRotation = new Vector3(
            biasSamples.Average(v => v.X),
            biasSamples.Average(v => v.Y),
            biasSamples.Average(v => v.Z)
        );

        // Convert to a quaternion
        float angle = avgRotation.Length();
        if (angle > 0)
        {
            Vector3 axis = Vector3.Normalize(avgRotation);
            Quaternion avgBiasQuat = Quaternion.CreateFromAxisAngle(axis, angle);

            // Store the inverse bias quaternion for correction
            biasCorrection = Quaternion.Inverse(avgBiasQuat);
        }

        biasComputed = true;
        biasSamples.Clear();
        Console.WriteLine("Bias correction quaternion computed: " + biasCorrection);
    }

    public Vector3 ApplyBiasCorrection(Vector3 rawRotationVector)
    {
        if (!biasComputed) return rawRotationVector;  // Return unchanged if bias not set

        // Convert rotation vector to quaternion
        float angle = rawRotationVector.Length();
        if (angle == 0) return rawRotationVector;  // No rotation

        Quaternion inputQuat = Quaternion.CreateFromAxisAngle(rawRotationVector, angle);

        // Apply bias correction
        Quaternion correctedQuat = biasCorrection * inputQuat;

        // Convert back to rotation vector (axis-angle representation)
        correctedQuat = Quaternion.Normalize(correctedQuat);  // Ensure normalized quaternion
        angle = 2 * (float)Math.Acos(correctedQuat.W);
        if (angle > 0)
        {
            float sinHalfAngle = (float)Math.Sin(angle / 2);
            Vector3 correctedAxis = new Vector3(correctedQuat.X, correctedQuat.Y, correctedQuat.Z) / sinHalfAngle;
            return correctedAxis * angle;
        }

        return Vector3.Zero;  // No correction needed
    }

    static Quaternion QuaternionFromFacingVector(Vector3 vector, Vector3 forward)
        => Quaternion.CreateFromAxisAngle(Vector3.Cross(vector, forward), MathF.Acos(Vector3.Dot(vector, forward)));

    public static Vector3 ApplyBias(Vector3 average, Vector3 input)
    {
        Vector3 forward = new(0f, 0f, -1f);
        Quaternion biasCorrection = Quaternion.Inverse(QuaternionFromFacingVector(average, forward));
        Quaternion inputQuaternion = QuaternionFromFacingVector(input, forward);
        Quaternion correctedQuaternion = Quaternion.Normalize(biasCorrection * inputQuaternion);
        Vector3 r = new(correctedQuaternion.X, correctedQuaternion.Y, correctedQuaternion.Z);
        return forward + 2 * Vector3.Cross(r, Vector3.Cross(r, forward) + correctedQuaternion.W * forward);
    }
}
