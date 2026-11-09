using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace FDMi.core
{
    public class FDMiTransformRotate : FDMiBehaviour
    {
        [Tooltip("入力データの解決パス。名前空間の区切りは /")]
        public string dataPath;

        [FDMiDataPath(nameof(dataPath))]
        [FDMiRegisterCallback(nameof(OnChange))]
        [Tooltip("回転角を決める入力。dataPath から解決する")]
        public FDMiData Value;

        [Tooltip("入力値 → 回転角 [deg] の変換カーブ")]
        public AnimationCurve outputCurve;

        [Tooltip("対象の解決パス。名前空間の区切りは /")]
        public string targetPath;

        [FDMiDataPath(nameof(targetPath))]
        [Tooltip("回転させる Transform。targetPath から解決する")]
        public FDMiTransformRef target;

        [SerializeField]
        [Tooltip("回転軸。角度はこの軸のオイラー角に配る")]
        Vector3 axis;

        // シーン上の角度の写し。エディタ (CaptureRotateBaseAngleUseCase) が FDMiData の
        // [FDMiRegisterCallback] 登録と同じ Hook で更新するので、手で編集しても次の Hook で戻る。
        // quaternion 合成ではなくオイラー加算なのは、プレビューと実行時で同一の式を使うため。
        // 軸が単一成分という前提で、その成分だけをカーブ出力で動かす。
        [HideInInspector]
        public Vector3 baseAngle;

        public void Initialize()
        {
            OnChange();
        }

        public void OnChange()
        {
            Vector3 angle = FDMiTransformMath.EvaluateEuler(
                axis,
                outputCurve,
                Value.GetFloat()
            );
            target.data[0].localEulerAngles = baseAngle + angle;
        }
    }
}
