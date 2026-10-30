using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiTransformMove : FDMiBehaviour
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string dataPath;

        [FDMiDataPath(nameof(dataPath))]
        [Tooltip("移動量を決める入力。dataPath から解決する")]
        public FDMiFloat Value;

        [Tooltip("入力値 → 0..1 補間係数の変換カーブ")]
        public AnimationCurve outputCurve;

        [Tooltip("対象の解決パス。名前空間の区切りは /")]
        public string targetPath;

        [FDMiDataPath(nameof(targetPath))]
        [Tooltip("移動させる Transform。targetPath から解決する")]
        public FDMiTransformRef target;

        [SerializeField]
        [Tooltip("両端のローカル位置。この間を補間する")]
        Vector3[] position = new Vector3[2];

        public void Initialize()
        {
            OnChange();
        }

        public void OnChange()
        {
            target.data[0].localPosition = FDMiTransformMath.EvaluatePosition(
                position[0],
                position[1],
                outputCurve,
                Value.GetFloat()
            );
        }
    }
}
