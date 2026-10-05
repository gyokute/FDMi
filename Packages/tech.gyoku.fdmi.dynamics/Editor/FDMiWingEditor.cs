using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 翼インスペクタに焼き付けボタンを足す。設計値を変えたら必ず押し直す。
    // 焼き付け結果はシーンに保存されるため、実行時計算は掛け算だけ残る。
    [CustomEditor(typeof(FDMiWing))]
    public class FDMiWingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FDMiWing w = (FDMiWing)target;
            if (GUILayout.Button("係数を焼き付ける"))
            {
                Undo.RecordObject(w, "FDMiWing Bake");
                FDMiWingBake.Bake(w);
                EditorUtility.SetDirty(w);
            }

            float ar = w.span * w.span / Mathf.Max(w.bakedArea, 1e-6f);
            EditorGUILayout.HelpBox(
                "焼き付け結果の目安。面積 " + w.bakedArea.ToString("F2") + " m^2、"
                + "アスペクト比 " + ar.ToString("F2") + "、"
                + "揚力傾斜 " + w.bakedLiftSlope.ToString("F3") + " /rad、"
                + "最大揚力 " + w.bakedCLmax.ToString("F2") + "、"
                + "最小抗力 " + w.bakedCD0.ToString("F4") + "、"
                + "吹き下ろし利得 " + w.bakedDownwashGain.ToString("F4") + " rad/CL。",
                MessageType.None);
        }
    }
}
