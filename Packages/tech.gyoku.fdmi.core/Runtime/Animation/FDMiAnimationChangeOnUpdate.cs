using UdonSharp;
using UnityEngine;

namespace FDMi.core
{
    /// <summary>
    /// Update() で毎フレーム Animator パラメータを更新するドライバ。
    /// </summary>
    public class FDMiAnimationChangeOnUpdate : FDMiAnimationDriver
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string valuePath;

        [FDMiDataPath(nameof(valuePath))]
        [Tooltip("毎フレーム読む入力。valuePath から解決する")]
        public FDMiData parameterValue;

        void Update()
        {
            Apply(parameterValue.GetFloat());
        }
    }
}
