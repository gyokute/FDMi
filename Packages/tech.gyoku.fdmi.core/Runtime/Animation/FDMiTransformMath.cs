using UnityEngine;

namespace FDMi.core
{
    // Move / Rotate の計算を Runtime (UdonSharp) と Editor で共有する。
    // U# は同一アセンブリ内の plain な static class の static メソッドを呼べる。
    // FDMiSyncMath のように UdonSharpBehaviour を基底にすると program asset が要るため、
    // 純粋な計算だけを持つここは plain static class にする。
    // 制約: U# の plain 型は static フィールド・プロパティ・ネスト型を持てない。
    public static class FDMiTransformMath
    {
        public static Vector3 EvaluatePosition(
            Vector3 from,
            Vector3 to,
            AnimationCurve curve,
            float input
        )
        {
            return Vector3.LerpUnclamped(from, to, curve.Evaluate(input));
        }

        public static Vector3 EvaluateEuler(Vector3 axis, AnimationCurve curve, float input)
        {
            return axis * curve.Evaluate(input);
        }
    }
}
