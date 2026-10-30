using FDMi.core;
using FDMi.core.Editor.Preview;
using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Inspector
{
    [CustomEditor(typeof(FDMiTransformMove))]
    public class FDMiTransformMoveEditor : FDMiTransformPreviewEditor
    {
        protected override bool TryCreate(out FDMiTransformPreviewBase preview, out string reason)
        {
            if (
                FDMiTransformPreview.TryCreateMove(
                    (FDMiTransformMove)target,
                    out FDMiMovePreview move,
                    out reason
                )
            )
            {
                preview = move;
                return true;
            }

            preview = null;
            return false;
        }

        protected override void DrawReadout()
        {
            FDMiMovePreview preview = (FDMiMovePreview)Preview;
            EditorGUILayout.LabelField("localPosition", preview.Target.localPosition.ToString("F3"));
            EditorGUILayout.LabelField(
                "開始から",
                preview.Distance(StartValue, Value).ToString("F3") + " m"
            );
            EditorGUILayout.LabelField(
                "範囲端",
                preview.Distance(RangeMin, RangeMax).ToString("F3") + " m"
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
