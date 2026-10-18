using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 胴体の形 (軸・全長・直径) と合力の作用点を Scene にワイヤーフレームで出す。
    // 座標は body ローカルなので、置いた GameObject の transform をそのまま使うとズレる。
    // 焼き付けボタンで transform から機体座標に変換して入れる (置いて押すまで既存値を壊さない)。
    [CustomEditor(typeof(FDMiBodyAero))]
    public class FDMiBodyAeroEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiBodyAero body = (FDMiBodyAero)target;
            if (body.body != null)
            {
                if (GUILayout.Button("軸と空力中心を transform から焼き付ける"))
                {
                    Undo.RecordObject(body, "FDMiBodyAero transform");
                    Transform t = body.transform;
                    // axis は向きだけ。機体の回転で表した胴体軸になる。
                    body.axis = body.body.transform.InverseTransformDirection(t.forward);
                    // 空力中心は重心まわりのモーメントの腕になる。機体座標で入れる。
                    body.centerOfPressure = body.body.transform.InverseTransformPoint(t.position);
                    EditorUtility.SetDirty(body);
                }
            }
            else
                EditorGUILayout.HelpBox("body が未配線。機体の Rigidbody を配線すると焼き付けできる。", MessageType.Info);
        }

        void OnSceneGUI()
        {
            FDMiBodyAero body = (FDMiBodyAero)target;
            Transform frame = FDMiAeroGeometry.Frame(body.body, body.transform);
            using (new Handles.DrawingScope(frame.localToWorldMatrix))
            {
                FDMiAeroGeometry.DrawBodyFrame(body.length * 0.2f);
                FDMiAeroGeometry.DrawCenterOfMass(body.body != null ? body.body.centerOfMass : Vector3.zero, body.length * 0.02f);
                FDMiAeroGeometry.DrawBodyCylinder(body.centerOfPressure, body.axis, body.length, body.diameter);
                float size = body.diameter * 0.25f;
                Handles.color = Color.yellow;
                Handles.DrawLine(body.centerOfPressure - Vector3.right * size, body.centerOfPressure + Vector3.right * size);
                Handles.DrawLine(body.centerOfPressure - Vector3.up * size, body.centerOfPressure + Vector3.up * size);
                Handles.DrawLine(body.centerOfPressure - Vector3.forward * size, body.centerOfPressure + Vector3.forward * size);
                Handles.Label(body.centerOfPressure + Vector3.up * size, "CP");
            }
        }
    }
}
