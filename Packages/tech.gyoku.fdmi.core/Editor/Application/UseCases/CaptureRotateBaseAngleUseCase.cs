using FDMi.core.Editor.Preview;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Application.UseCases
{
    // FDMiTransformRotate.baseAngle は「シーン上の角度」の写し。
    // [FDMiDataPath] / [FDMiRegisterCallback] の登録と同じ Hook (選択時・インスペクタ変更時・
    // Play/ビルド/メニュー) で更新し、Transform の姿勢だけを真実にする。
    // プレビュー中は Transform がプレビュー値で書き換わっているため登録しない。
    public class CaptureRotateBaseAngleUseCase : IFDMiAutoSetupUseCase
    {
        public void Execute(Object target)
        {
            if (target is FDMiTransformRotate rotate)
                Capture(rotate);
        }

        public void ExecuteAll()
        {
            FDMiTransformRotate[] rotates = Object.FindObjectsByType<FDMiTransformRotate>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            for (int i = 0; i < rotates.Length; i++)
                Capture(rotates[i]);
        }

        public static void Capture(FDMiTransformRotate rotate)
        {
            if (rotate == null || EditorApplication.isPlaying || FDMiPreviewSession.IsActive)
                return;
            if (rotate.target == null || rotate.target.data == null || rotate.target.data.Length == 0)
                return;

            Transform target = rotate.target.data[0];
            if (target == null)
                return;

            Vector3 angle = target.localEulerAngles;
            if ((angle - rotate.baseAngle).sqrMagnitude < 1e-6f)
                return;

            Undo.RecordObject(rotate, "Register Base Angle");
            rotate.baseAngle = angle;
            EditorUtility.SetDirty(rotate);
        }
    }
}
