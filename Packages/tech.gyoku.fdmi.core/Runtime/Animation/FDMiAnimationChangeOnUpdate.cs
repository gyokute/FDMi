using UdonSharp;
using UnityEngine;

namespace FDMi.core
{
    /// <summary>
    /// Update() で毎フレーム Animator パラメータを更新するドライバ。
    /// </summary>
    public class FDMiAnimationChangeOnUpdate : FDMiAnimationDriver
    {
        public string valuePath;

        [FDMiDataPath(nameof(valuePath))]
        public FDMiData parameterValue;

        void Update()
        {
            Apply(parameterValue.GetFloat());
        }
    }
}
