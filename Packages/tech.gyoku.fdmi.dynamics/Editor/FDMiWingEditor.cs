using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    [CustomEditor(typeof(FDMiWing))]
    public class FDMiWingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FDMiWing wing = (FDMiWing)target;
            if (wing.SectionL == null || wing.SectionR == null)
            {
                EditorGUILayout.HelpBox("SectionL・SectionR (両端の FDMiWingSection) を指定する。", MessageType.Warning);
                return;
            }

            Rigidbody body = wing.body;
            if (body == null)
                body = wing.GetComponentInParent<Rigidbody>();
            if (body == null)
            {
                EditorGUILayout.HelpBox("親に Rigidbody がない。", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("平面形と干渉係数を焼き付ける"))
            {
                Undo.RecordObject(wing, "FDMiWing Bake");
                Undo.RecordObject(wing.transform, "FDMiWing Bake Transform");
                float rho = 1.225f;
                FDMiWingBake.Bake(wing, body, rho);
                EditorUtility.SetDirty(wing);
            }

            EditorGUILayout.HelpBox(
                "面積 " + wing.cpArea.ToString("F1") + " m2、コード " + wing.cpChordLength.ToString("F2")
                + " m、幅 " + wing.cpSpanLength.ToString("F2") + " m。",
                MessageType.None);
        }

        void OnSceneGUI()
        {
            FDMiWing wing = (FDMiWing)target;
            if (wing.SectionL == null || wing.SectionR == null)
                return;
            Rigidbody body = wing.body != null ? wing.body : wing.GetComponentInParent<Rigidbody>();
            if (body == null)
                return;
            Transform bt = body.transform;
            Vector3 spl = bt.InverseTransformPoint(wing.SectionL.transform.position);
            Vector3 spr = bt.InverseTransformPoint(wing.SectionR.transform.position);
            Vector3 toeL = spl - wing.SectionL.chordLength * bt.InverseTransformDirection(wing.SectionL.transform.forward).normalized;
            Vector3 toeR = spr - wing.SectionR.chordLength * bt.InverseTransformDirection(wing.SectionR.transform.forward).normalized;
            using (new Handles.DrawingScope(bt.localToWorldMatrix))
            {
                Handles.color = Color.magenta;
                Handles.DrawLine(spl, spr);
                Handles.DrawLine(spl, toeL);
                Handles.DrawLine(spr, toeR);
                Handles.DrawLine(toeL, toeR);
            }
        }
    }
}
