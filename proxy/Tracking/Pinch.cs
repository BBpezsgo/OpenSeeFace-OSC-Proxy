// https://github.com/rogeraabbccdd/VRChat-MotionOSC/blob/master/src/renderer/motions/item.ts

using System.Numerics;

namespace VRChatProxy;

public enum PinchState
{
    None,
    Grab,
    Hold,
    Release,
}

public struct Pinch
{
    public readonly int Finger1;
    public readonly int Finger2;

    public bool IsThresholdMet;
    public double LastGrabTime;

    public Vector3 OriginalPinch;
    public Vector3 CurrentPinch;

    public int PinchDowned;
    public int PinchUp;

    public PinchState State;

    public Pinch(int finger1, int finger2) : this()
    {
        Finger1 = finger1;
        Finger2 = finger2;
    }

    public void Update(in Hand hand)
    {
        float a = hand.Points[Finger1].X - hand.Points[Finger2].X;
        float b = hand.Points[Finger1].Y - hand.Points[Finger2].Y;
        float c = MathF.Sqrt(a * a + b * b) * 480;
        bool threshold = IsThresholdMet = c < 30f;

        if (threshold)
        {
            CurrentPinch = hand.Points[Finger1];

            if (hand.Time - LastGrabTime > 0.7d)
            {
                OriginalPinch = hand.Points[Finger1];
            }
            LastGrabTime = hand.Time;
        }

        if (IsThresholdMet)
        {
            PinchDowned++;
        }
        else
        {
            PinchUp = PinchDowned;
            PinchDowned = 0;
        }

        if (PinchDowned > 0 && PinchDowned <= 5)
        {
            State = PinchState.Grab;
        }
        else if (PinchDowned > 5)
        {
            State = PinchState.Hold;
        }
        else if (PinchUp != default)
        {
            State = PinchState.Release;
        }
        else
        {
            State = PinchState.None;
        }
    }
}
