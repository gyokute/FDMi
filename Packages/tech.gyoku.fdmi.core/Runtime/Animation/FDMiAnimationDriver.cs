using UdonSharp;
using UnityEngine;

namespace FDMi.core
{
    /// <summary>
    /// FDMiData の値を AnimationCurve で加工して Animator パラメータへ出力するドライバ群の基底クラス。
    /// valuePathHash は Editor 拡張が Animator.StringToHash(parameterName) を焼き込むため手編集しない。
    /// 出力タイミング(Update / FDMiData コールバック)は派生クラスが定義する。
    /// </summary>
    public abstract class FDMiAnimationDriver : FDMiBehaviour
    {
        [SerializeField]
        [Tooltip("出力先の Animator。通常は親から解決する")]
        private Animator animator;

        [SerializeField]
        [Tooltip("Animator のパラメータ名。変えたら焼き直す")]
        private string parameterName;

        // 焼き込み値としてScene/Prefabへの永続化が必要なため [HideInInspector] public (表示と焼き込みは FDMiAnimationDriverEditor / BakeAnimatorParameterHashUseCase)
        [HideInInspector]
        public int valuePathHash;

        [Tooltip("入力値 → Animator 出力の変換カーブ")]
        public AnimationCurve outputCurve;

        protected void Apply(float value)
        {
            animator.SetFloat(valuePathHash, outputCurve.Evaluate(value));
        }
    }
}
