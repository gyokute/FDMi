using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 装置が覆う区間を、翼のワイヤーフレームに重ねて出す。
    // 翼は親 (GetComponentInParent) から取る。装置は自分の翼を持たない (区間の並びと翼の配線は翼が持つ)。
    [CustomEditor(typeof(FDMiControlSurface))]
    public class FDMiControlSurfaceEditor : FDMiBehaviourEditor
    {
        // 区間表の横スクロール。Editor のみ (保存しない。縦は Inspector に任せる)。
        Vector2 _tableScroll;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiControlSurface device = (FDMiControlSurface)target;
            FDMiWing wing = device.GetComponentInParent<FDMiWing>(true);
            if (wing == null)
            {
                // 自動解決も同じ規則。置き場所が違うと翼に配線されない。
                EditorGUILayout.HelpBox("親に FDMiWing がない。装置は翼の子に置く。", MessageType.Info);
                return;
            }

            DrawSectionTable(device, wing);
        }

        // 区間表。行=Sec、列=sectionMul(編集) / alpha / cl / cd / scale(出力・読取専用)。
        // 出力は FixedUpdate で上書きされるため編集しない。横にスクロールし、縦には伸ばす
        // (Inspector 自体が縦スクロールするため、二重スクロールにしない。FDMiWingEditor と同じ)。
        void DrawSectionTable(FDMiControlSurface device, FDMiWing wing)
        {
            int sections = device.sectionMul != null ? device.sectionMul.Length : 0;
            int covered = 0;
            for (int i = 0; i < sections; i++)
                if (device.sectionMul[i] != 0f)
                    covered++;
            EditorGUILayout.LabelField("区間 (装置の効き)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("翼", wing.name + " (" + wing.coordPos.Length + " 区間)");
            EditorGUILayout.LabelField("覆う区間", covered + " / " + sections);
            if (sections == 0)
                return;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全て0", GUILayout.Width(64f)))
                    SetAll(device, 0f);
                if (GUILayout.Button("全て1", GUILayout.Width(64f)))
                    SetAll(device, 1f);
            }
            _tableScroll = EditorGUILayout.BeginScrollView(_tableScroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Sec", GUILayout.Width(40f));
                EditorGUILayout.LabelField("sectionMul", GUILayout.Width(140f));
                EditorGUILayout.LabelField("alpha", GUILayout.Width(140f));
                EditorGUILayout.LabelField("cl", GUILayout.Width(140f));
                EditorGUILayout.LabelField("cd", GUILayout.Width(140f));
                EditorGUILayout.LabelField("scale", GUILayout.Width(140f));
            }
            for (int i = 0; i < sections; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(40f));
                    float mul = EditorGUILayout.Slider(device.sectionMul[i], 0f, 1f, GUILayout.Width(140f));
                    if (mul != device.sectionMul[i])
                    {
                        Undo.RecordObject(device, "FDMiControlSurface 区間配分");
                        device.sectionMul[i] = mul;
                        EditorUtility.SetDirty(device);
                    }
                    EditorGUILayout.LabelField(Output(device.alpha, i, "F2"), GUILayout.Width(140f));
                    EditorGUILayout.LabelField(Output(device.cl, i, "F3"), GUILayout.Width(140f));
                    EditorGUILayout.LabelField(Output(device.cd, i, "F3"), GUILayout.Width(140f));
                    EditorGUILayout.LabelField(Output(device.scale, i, "F3"), GUILayout.Width(140f));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        static string Output(float[] values, int i, string format)
        {
            if (values == null || i < 0 || i >= values.Length)
                return "-";
            return values[i].ToString(format);
        }

        static void SetAll(FDMiControlSurface device, float value)
        {
            if (device.sectionMul == null)
                return;
            Undo.RecordObject(device, "FDMiControlSurface 区間配分");
            for (int i = 0; i < device.sectionMul.Length; i++)
                device.sectionMul[i] = value;
            EditorUtility.SetDirty(device);
        }
        void OnSceneGUI()
        {
            FDMiControlSurface device = (FDMiControlSurface)target;
            FDMiWing wing = device.GetComponentInParent<FDMiWing>(true);
            if (wing == null || wing.coordPos == null)
                return;

            Transform frame = FDMiAeroGeometry.Frame(wing.body, wing.transform);
            using (new Handles.DrawingScope(frame.localToWorldMatrix))
            {
                // 翼の区間は薄く、装置が覆う区間は濃く。どこに効くかが1枚で分かる。
                // 覆わない区間も消さない: 装置の位置は翼の区間の並びの中で決まる。
                // 未解決区間 (面積0) は翼側と同様に描かない。枠は真の台形 (DrawSection) で描く。
                Color dim = new Color(0.55f, 0.6f, 0.65f, 1f);
                Color covered = new Color(1f, 0.6f, 0.2f, 1f);
                for (int i = 0; i < wing.coordPos.Length; i++)
                {
                    if (wing.sectionArea == null || i >= wing.sectionArea.Length || wing.sectionArea[i] <= 0f)
                        continue;
                    if (wing.sectionChord == null || i >= wing.sectionChord.Length || wing.sectionChord[i] <= 0f)
                        continue;
                    FDMiAeroGeometry.DrawSection(wing, i, i < device.sectionMul.Length && device.sectionMul[i] != 0f ? covered : dim);
                }
                FDMiAeroGeometry.DrawDevice(wing, device, covered);
                FDMiAeroGeometry.DrawBodyFrame(wing.span * 0.2f);
            }
        }
    }
}
