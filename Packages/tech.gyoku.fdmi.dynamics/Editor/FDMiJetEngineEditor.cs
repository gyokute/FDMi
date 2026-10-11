using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 設計値を変えたら自動で Bake し直し、焼き付け面積と静止最大推力を表示する。
    // 静止最大推力 = ISA SL (15℃, 101.325kPa)・速度0・全開 (離陸定格の表示条件)。
    [CustomEditor(typeof(FDMiJetEngine))]
    public class FDMiJetEngineEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            FDMiJetEngine e = (FDMiJetEngine)target;
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(e, "JetEngine design change");
                FDMiJetEngineBake.Bake(e);
                EditorUtility.SetDirty(e);
            }

            if (e._a8 <= 0f || e._afan <= 0f)
            {
                EditorGUILayout.HelpBox("未焼き付け。設計値を触ると自動で焼き付く。", MessageType.Warning);
                if (GUILayout.Button("焼き付ける"))
                {
                    Undo.RecordObject(e, "JetEngine bake");
                    FDMiJetEngineBake.Bake(e);
                    EditorUtility.SetDirty(e);
                }
            }

            FDMiJetEngineBake.StaticResult r = FDMiJetEngineBake.SolveStaticMax(e);
            EditorGUILayout.HelpBox(
                "焼き付け: A4 "
                    + e._a4.ToString("F5")
                    + " / A4p "
                    + e._a4p.ToString("F5")
                    + " / A8 "
                    + e._a8.ToString("F5")
                    + " m2、Afan "
                    + e._afan.ToString("F4")
                    + " m2。"
                    + "静止最大推力 (ISA SL・V0・全開): "
                    + r.thrustN.ToString("F0")
                    + " N、"
                    + "EPR "
                    + r.epr.ToString("F3")
                    + "、燃料 "
                    + r.fuelKgS.ToString("F3")
                    + " kg/s、"
                    + (7936.65 * r.fuelKgS).ToString("F3")
                    + " lbs/h、"
                    + "空気 "
                    + r.airKgS.ToString("F1")
                    + " kg/s。",
                MessageType.None
            );
        }
    }
}
