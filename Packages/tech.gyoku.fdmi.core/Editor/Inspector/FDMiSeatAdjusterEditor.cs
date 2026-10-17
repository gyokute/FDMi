using UnityEditor;
using UnityEngine;

namespace FDMi.core.Editor.Inspector
{
    // Seat の initialSeatPos はシーン上の座席配置ごとに変わる設計値。手入力ではなく
    // エディタ上の実位置から焼き付ける。押すまで既存値を壊さないようボタン方式
    // (WheelCollider の「係数を焼き付ける」と同じ流儀)。
    [CustomEditor(typeof(FDMiSeatAdjuster))]
    public class FDMiSeatAdjusterEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiSeatAdjuster s = (FDMiSeatAdjuster)target;
            if (GUILayout.Button("initialSeatPos を焼き付ける"))
            {
                Undo.RecordObject(s, "FDMiSeatAdjuster initialSeatPos");
                s.initialSeatPos = s.transform.localPosition;
                EditorUtility.SetDirty(s);
            }
        }
    }
}
