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
        float _machEnv = 0.8f; // 包絡線表示用の Mach (Editor のみ、保存しない)
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

            if (e._a8 <= 0f || e.coreAreaM2 <= 0f)
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
                "焼き付け: A8 "
                    + e._a8.ToString("F5")
                    + " m2、Core A "
                    + e.coreAreaM2.ToString("F4")
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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("全開エンベロープ (Editor 推算・ISA)", EditorStyles.boldLabel);
            _machEnv = Mathf.Clamp(EditorGUILayout.FloatField("Mach", _machEnv), 0f, 2f);
            if (e._a8 > 0f && e._dHcD > 0f)
            {
                const int n = 25;
                const float altMax = 15000f;
                float maxT = 1f;
                float[] th = new float[n];
                for (int i = 0; i < n; i++)
                {
                    th[i] = FDMiJetEngineEnvelope.SolveFullPower(e, altMax * i / (n - 1), _machEnv).thrustN;
                    if (th[i] > maxT)
                        maxT = th[i];
                }
                Rect gr = EditorGUILayout.GetControlRect(GUILayout.Height(130f));
                float padL = 44f, padR = 8f, padT = 6f, padB = 16f;
                EditorGUI.DrawRect(gr, new Color(0.14f, 0.14f, 0.14f));
                Handles.BeginGUI();
                Handles.color = new Color(1f, 1f, 1f, 0.25f);
                float midY = gr.yMax - padB - (gr.height - padT - padB) * 0.5f;
                Handles.DrawLine(new Vector3(gr.x + padL, midY, 0f), new Vector3(gr.xMax - padR, midY, 0f));
                Handles.color = Color.cyan;
                Vector3[] pts = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    float x = gr.x + padL + (gr.width - padL - padR) * i / (n - 1);
                    float y = gr.yMax - padB - (gr.height - padT - padB) * (th[i] / maxT);
                    pts[i] = new Vector3(x, y, 0f);
                }
                Handles.DrawAAPolyLine(2f, pts);
                Handles.EndGUI();
                GUI.Label(new Rect(gr.x, gr.y, padL, 14f), (maxT / 1000f).ToString("F0") + "kN", EditorStyles.miniLabel);
                GUI.Label(new Rect(gr.x, gr.yMax - padB, padL, 14f), "0", EditorStyles.miniLabel);
                GUI.Label(new Rect(gr.x + padL, gr.yMax - padB + 2f, 60f, 14f), "0m", EditorStyles.miniLabel);
                GUI.Label(new Rect(gr.xMax - padR - 60f, gr.yMax - padB + 2f, 60f, 14f), "15km", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    "0m "
                        + (th[0] / 1000f).ToString("F1")
                        + " / 5km "
                        + (th[8] / 1000f).ToString("F1")
                        + " / 10km "
                        + (th[16] / 1000f).ToString("F1")
                        + " / 15km "
                        + (th[n - 1] / 1000f).ToString("F1")
                        + " kN"
                );
            }
        }
    }
}
