using UdonSharp;
using UnityEngine;

namespace FDMi.core
{
    /// <summary>
    /// FDMiData の値変化コールバックで Animator パラメータを更新するドライバ。
    /// </summary>
    public class FDMiAnimationChangeByValue : FDMiAnimationDriver
    {
        public string valuePath;

        [FDMiDataPath(nameof(valuePath)), FDMiRegisterCallback(nameof(OnChange))]
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
