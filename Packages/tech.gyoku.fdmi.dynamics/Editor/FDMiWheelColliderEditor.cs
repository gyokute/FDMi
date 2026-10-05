using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // ホイールインスペクタに焼き付けボタンを足す。設計値・body.mass を変えたら必ず押し直す。
    // 焼き付け結果はシーンに保存されるため、実行時の Start は読み出しと旧シーン救済だけ残る。
    // ジオメトリのビジュアライズは OnSceneGUI で行い、選択中のみワイヤーフレーム表示する
    // (Unity 標準の WheelCollider と同じ流儀。シーン全体を汚さない)。
    [CustomEditor(typeof(FDMiWheelCollider))]
    public class FDMiWheelColliderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FDMiWheelCollider w = (FDMiWheelCollider)target;
            if (GUILayout.Button("係数を焼き付ける"))
            {
                Undo.RecordObject(w, "FDMiWheelCollider Bake");
                FDMiWheelBake.Bake(w);
                EditorUtility.SetDirty(w);
            }

            float mass = w.body != null ? w.body.mass : 200000f;
            float targetLoad = mass * 9.81f * Mathf.Max(w.loadShare, 0.001f);
            EditorGUILayout.HelpBox(
                "焼き付け結果の目安。静荷重分担 " + targetLoad.ToString("F0") + " N、"
                + "基本レート " + (w.bakedK0 / 1000f).ToString("F0") + " kN/m、"
                + "折点 " + w.bakedKneeX.ToString("F2") + " m、"
                + "末端レート " + (w.bakedKEnd / 1000f).ToString("F0") + " kN/m、"
                + "減衰 圧側 " + (w.bakedCCompress / 1000f).ToString("F0") + " / 伸側 " + (w.bakedCRebound / 1000f).ToString("F0") + " kNs/m、"
                + "脚上限 " + w.bakedFzCap.ToString("F0") + " N、"
                + "焼き付け時質量 " + w.bakedBodyMass.ToString("F0") + " kg。",
                MessageType.None);

            if (w.bakedBodyMass <= 0f)
                EditorGUILayout.HelpBox("未焼き付け。実行時はフォールバック計算で動く。", MessageType.Warning);
            else if (w.body != null && !Mathf.Approximately(w.body.mass, w.bakedBodyMass))
                EditorGUILayout.HelpBox("body.mass が焼き付け時と違う。再焼き付けが必要。", MessageType.Warning);

            if (w.body != null)
            {
                Vector3 it = w.body.inertiaTensor;
                float iMax = Mathf.Max(it.x, Mathf.Max(it.y, it.z));
                if (iMax < w.body.mass * 0.25f)
                    EditorGUILayout.HelpBox("Rigidbody の慣性テンソルが質量に対して小さすぎる (回転半径 < 0.5 m)。脚のオフセンター荷重で発散する。Implicit Tensor を切って m*L^2/12 程度の値を入れる。", MessageType.Error);
            }

            if (w.suspensionOutput == null || w.spinOutput == null)
                EditorGUILayout.HelpBox("可視化は外部の責務。suspensionOutput (0..1)・spinOutput (rad 0..2pi) を FDMiFloat につなぐとタイヤ・脚・SteerPivot を駆動できる。未接続でも outStroke 等のテレメトリは出る。", MessageType.Info);
        }

        void OnSceneGUI()
        {
            FDMiWheelCollider w = (FDMiWheelCollider)target;
            Transform t = w.transform;
            Vector3 o = t.position;
            Vector3 a = t.up;
            if (a.sqrMagnitude < 1e-8f)
                a = Vector3.up;
            a = a.normalized;
            Vector3 f0 = t.forward;
            if (f0.sqrMagnitude < 1e-8f)
                f0 = Vector3.forward;
            f0 = f0.normalized;
            Vector3 axleDir = t.right;
            if (axleDir.sqrMagnitude < 1e-8f)
                axleDir = Vector3.right;
            axleDir = axleDir.normalized;

            float stroke = Mathf.Clamp(w.maxStroke, 0.01f, 10f);
            float radius = Mathf.Clamp(w.tireRadius, 0.01f, 10f);
            float rayLen = stroke + radius;

            Vector3 qExt = o - a * stroke;
            Vector3 qBot = o;
            Vector3 groundProbe = o - a * rayLen;

            Handles.color = Color.gray;
            Handles.DrawLine(o, groundProbe, 2f);

            Handles.color = Color.yellow;
            Handles.DrawWireDisc(qExt, axleDir, radius, 2f);
            Handles.DrawLine(qExt - axleDir * radius, qExt + axleDir * radius, 1f);

            Handles.color = new Color(1f, 0.4f, 0.4f, 1f);
            Handles.DrawWireDisc(qBot, axleDir, radius, 1f);

            if (Application.isPlaying)
            {
                float x = Mathf.Clamp(w.outStroke, 0f, stroke);
                Vector3 qCur = o - a * (stroke - x);
                Handles.color = Color.green;
                Handles.DrawWireDisc(qCur, axleDir, radius, 3f);
            }

            float tick = radius * 1.5f;
            Handles.color = Color.white;
            Handles.DrawLine(qExt, qExt + f0 * tick, 1f);
            float lim = Mathf.Abs(w.steerLimitDeg);
            if (lim > 0.5f)
            {
                Handles.color = new Color(0.4f, 0.8f, 1f, 1f);
                Vector3 fL = Quaternion.AngleAxis(lim, a) * f0;
                Vector3 fR = Quaternion.AngleAxis(-lim, a) * f0;
                Handles.DrawLine(qExt, qExt + fL * tick, 1f);
                Handles.DrawLine(qExt, qExt + fR * tick, 1f);
            }

            Handles.color = Color.gray;
            Vector3 g1 = groundProbe - axleDir * radius;
            Vector3 g2 = groundProbe + axleDir * radius;
            Handles.DrawLine(g1, g2, 1f);
        }
    }
}
