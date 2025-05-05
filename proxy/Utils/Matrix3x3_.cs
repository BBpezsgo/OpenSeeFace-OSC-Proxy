namespace VRChatProxy;

public struct Matrix3x3_
{
    public float _00, _01, _02;
    public float _10, _11, _12;
    public float _20, _21, _22;

    public Matrix3x3_(
        float _00, float _01, float _02,
        float _10, float _11, float _12,
        float _20, float _21, float _22
        )
    {
        this._00 = _00;
        this._01 = _01;
        this._02 = _02;
        this._10 = _10;
        this._11 = _11;
        this._12 = _12;
        this._20 = _20;
        this._21 = _21;
        this._22 = _22;
    }

    public readonly Matrix3x3_ Transpose() => new(
        _00, _10, _20,
        _01, _11, _21,
        _02, _12, _22
    );

    public static Matrix3x3_ operator -(Matrix3x3_ a, Matrix3x3_ b) => new(
        a._00 - b._00, a._01 - b._01, a._02 - b._02,
        a._10 - b._10, a._11 - b._11, a._12 - b._12,
        a._20 - b._20, a._21 - b._21, a._22 - b._22
    );

    public static Matrix3x3_ operator /(Matrix3x3_ a, float scalar) => new(
        a._00 / scalar, a._01 / scalar, a._02 / scalar,
        a._10 / scalar, a._11 / scalar, a._12 / scalar,
        a._20 / scalar, a._21 / scalar, a._22 / scalar
    );

    public static Matrix3x3_ operator *(Matrix3x3_ a, Matrix3x3_ b) => new(
        a._00 * b._00 + a._01 * b._10 + a._02 * b._20,
        a._00 * b._01 + a._01 * b._11 + a._02 * b._21,
        a._00 * b._02 + a._01 * b._12 + a._02 * b._22,

        a._10 * b._00 + a._11 * b._10 + a._12 * b._20,
        a._10 * b._01 + a._11 * b._11 + a._12 * b._21,
        a._10 * b._02 + a._11 * b._12 + a._12 * b._22,

        a._20 * b._00 + a._21 * b._10 + a._22 * b._20,
        a._20 * b._01 + a._21 * b._11 + a._22 * b._21,
        a._20 * b._02 + a._21 * b._12 + a._22 * b._22
    );
}
