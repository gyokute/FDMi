using FDMi.core;
using FDMi.core.Editor.Preview;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Inspector
{
    [CustomEditor(typeof(FDMiTransformRotate))]
    public class FDMiTransformRotateEditor : FDMiTransformPreviewEditor
    {
        protected override bool TryCreate(out FDMiTransformPreviewBase preview, out string reason)
        {
            if (
                FDMiTransformPreview.TryCreateRotate(
                    (FDMiTransformRotate)target,
                    out FDMiRotatePreview rotate,
                    out reason
                )
            )
            {
                preview = rotate;
                return true;
            }

            preview = null;
            return false;
        }

        protected override void DrawReadout()
        {
            FDMiRotatePreview preview = (FDMiRotatePreview)Preview;
            Vector3 euler = preview.LocalEulerAt(Value);
            EditorGUILayout.LabelField(
                "角度 X / Y / Z",
                euler.x.ToString("F1") + "° / " + euler.y.ToString("F1") + "° / " + euler.z.ToString("F1") + "°"
            );
            EditorGUILayout.LabelField(
                "開始から",
                preview.Angle(StartValue, Value).ToString("F1") + "°"
            );
            EditorGUILayout.LabelField(
                "範囲端",
                preview.Angle(RangeMin, RangeMax).ToString("F1") + "°"
            );
        }

        void OnSceneGUI()
        {
            DrawScene();
        }

        void OnDisable()
        {
            StopPreview();
        }
    }
}
