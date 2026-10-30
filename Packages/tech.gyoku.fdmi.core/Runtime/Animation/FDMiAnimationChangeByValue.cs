using UdonSharp;
using UnityEngine;

namespace FDMi.core
{
    /// <summary>
    /// FDMiData の値変化コールバックで Animator パラメータを更新するドライバ。
    /// </summary>
    public class FDMiAnimationChangeByValue : FDMiAnimationDriver
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string valuePath;

        [FDMiDataPath(nameof(valuePath)), FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("値変化で Animator を更新する入力。valuePath から解決する")]
        public FDMiData parameterValue;

        public void Initialize()
        {
            OnChange();
        }

        public void OnChange()
        {
            Apply(parameterValue.GetFloat());
        }
    }
}
