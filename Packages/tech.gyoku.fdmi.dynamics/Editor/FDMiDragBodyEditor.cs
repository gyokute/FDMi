using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    [CustomEditor(typeof(FDMiDragBody))]
    public class FDMiDragBodyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FDMiDragBody dragBody = (FDMiDragBody)target;
            if (GUILayout.Button("抗力面積を焼き付ける"))
            {
                Undo.RecordObject(dragBody, "FDMiDragBody Bake");
                FDMiDragBodyBake.Bake(dragBody);
                EditorUtility.SetDirty(dragBody);
            }

            EditorGUILayout.HelpBox(
                "等価抗力面積 " + dragBody.cdA.ToString("F3") + " m2。",
                MessageType.None);
        }

        void OnSceneGUI()
        {
            FDMiDragBody tgt = (FDMiDragBody)target;
            float rad = Mathf.Max(tgt.d, 0.01f) * 0.5f;
            Vector3 fwd = Vector3.forward * 0.5f * tgt.l;
            Vector3 aft = -Vector3.forward * 0.5f * tgt.l;
            if (Event.current.type != EventType.Repaint)
                return;
            Handles.color = Color.yellow;
            using (new Handles.DrawingScope(tgt.transform.localToWorldMatrix))
            {
                foreach (Vector3 q in new Vector3[] { Vector3.right, Vector3.up, Vector3.forward })
                {
                    Handles.CircleHandleCap(0, fwd, tgt.transform.rotation * Quaternion.LookRotation(q), rad, EventType.Repaint);
                    Handles.CircleHandleCap(0, aft, tgt.transform.rotation * Quaternion.LookRotation(q), rad, EventType.Repaint);
                    Handles.DrawLine(fwd + rad * q, aft + rad * q);
                    Handles.DrawLine(fwd - rad * q, aft - rad * q);
                }
            }
        }
    }
}
