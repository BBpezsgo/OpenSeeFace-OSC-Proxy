// Extracted (and refactored) from https://github.com/emilianavt/OpenSeeFace/blob/master/Examples/OpenSeeVRMExpression.cs

namespace OpenSee;

public enum SimpleExpression
{
    Neutral,
    Fun,
    Surprise,
    Angry,
}

public class SimpleExpressionDetector
{
    /// <summary>
    /// This smoothing factor is applied to the features used for simple expression detection.
    /// </summary>
    const float _simpleSmoothing = 0.6f;

    /// <summary>
    /// This smoothing factor is applied to the features used for simple expression detection.
    /// </summary>
    const float _simpleSensitivity = 1f;

    float _lastMouthCorner = 0f;
    float _lastEyebrows = 0f;
    bool _hadFun = false;
    bool _hadAngry = false;
    bool _hadSurprised = false;

    static float AdjustThreshold(bool active) => active ? 0.8f : 1f;

    public SimpleExpression Detect(in Face face)
    {
        _lastMouthCorner = _lastMouthCorner * _simpleSmoothing + (face.Features.MouthCornerUpDownLeft + face.Features.MouthCornerUpDownRight) * 0.5f * (1f - _simpleSmoothing);
        _lastEyebrows = _lastEyebrows * _simpleSmoothing + (face.Features.EyebrowUpDownLeft + face.Features.EyebrowUpDownRight) * 0.5f * (1f - _simpleSmoothing);
        if (_lastMouthCorner * _simpleSensitivity < -0.2f * AdjustThreshold(_hadFun))
        {
            _hadFun = true;
            _hadSurprised = false;
            _hadAngry = false;
            return SimpleExpression.Fun;
        }
        else if (_lastEyebrows * _simpleSensitivity > 0.2f * AdjustThreshold(_hadSurprised))
        {
            _hadFun = false;
            _hadSurprised = true;
            _hadAngry = false;
            return SimpleExpression.Surprise;
        }
        else if (_lastEyebrows * _simpleSensitivity < -0.25f * AdjustThreshold(_hadAngry) && _lastMouthCorner * _simpleSensitivity > -0.3f * (2f - AdjustThreshold(_hadAngry)))
        {
            _hadFun = false;
            _hadSurprised = false;
            _hadAngry = true;
            return SimpleExpression.Angry;
        }
        else
        {
            _hadFun = false;
            _hadSurprised = false;
            _hadAngry = false;
            return SimpleExpression.Neutral;
        }
    }
}
