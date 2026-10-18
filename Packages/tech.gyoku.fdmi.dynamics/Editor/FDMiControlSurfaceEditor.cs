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

            int covered = 0;
            for (int i = 0; i < device.sectionMul.Length; i++)
                if (device.sectionMul[i] != 0f)
                    covered++;
            EditorGUILayout.LabelField("翼", wing.name + " (" + wing.coordPos.Length + " 区間)");
            EditorGUILayout.LabelField("覆う区間", covered + " / " + device.sectionMul.Length);
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
                Color dim = new Color(0.55f, 0.6f, 0.65f, 1f);
                Color covered = new Color(1f, 0.6f, 0.2f, 1f);
                for (int i = 0; i < wing.coordPos.Length; i++)
                    FDMiAeroGeometry.DrawSection(wing, i, i < device.sectionMul.Length && device.sectionMul[i] != 0f ? covered : dim);
                FDMiAeroGeometry.DrawDevice(wing, device, covered);
                FDMiAeroGeometry.DrawBodyFrame(wing.span * 0.2f);
            }
        }
    }
}
