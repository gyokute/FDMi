using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 焼き付いた質量特性と MAC を読み取り専用で出す。配線 (body / データパス) と designMass は
    // 既定の Inspector で編集する。値の変更は FDMiBehaviourEditor が検出して
    // FDMiBodyMassAutoSetupUseCase を再実行し、FDMiDataPath の解決と同じタイミングで焼き直す。
    //
    // Scene view には MAC 線と 2 つの重心を出す。
    //   白   : 焼き付け時の合成重心 (テンソルの基準)
    //   黄色 : 現在の ~/Physics/CG が指す重心 (ランタイムが使う点)
    // 2 つがずれているときは Inspector に再焼き付けの警告を出す。設計点と入力点は別物なので、
    // 片方だけ描くと「ノードを変えたのにシーンの点が動かない」と誤解する。
    [CustomEditor(typeof(FDMiBodyMass))]
    public class FDMiBodyMassEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiBodyMass mass = (FDMiBodyMass)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("質量特性 (Editor 焼き付け)", EditorStyles.boldLabel);

            if (mass.body == null)
            {
                EditorGUILayout.HelpBox("body が未配線。機体の Rigidbody を配線すると焼き付けできる。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("設計質量", mass.designMass.ToString("F0") + " kg");
            EditorGUILayout.LabelField(
                "配分",
                "翼 " + mass.wingMass.ToString("F0") + " / 胴体 " + mass.fuselageMass.ToString("F0")
                    + " / エンジン " + mass.engineMass.ToString("F0") + " kg"
            );
            EditorGUILayout.LabelField("主翼", mass.mainWing != null ? mass.mainWing.name : "-");
            EditorGUILayout.LabelField(
                "MAC",
                mass.macChord.ToString("F2") + " m / LE " + mass.macLeadingEdge.ToString("F3")
                    + " / 方向 " + mass.macDirection.ToString("F3")
            );
            EditorGUILayout.LabelField("設計MAC重心率", mass.bakedMacFraction.ToString("F3"));
            float inputFraction = CurrentMacFraction(mass);
            EditorGUILayout.LabelField("入力MAC重心率", inputFraction.ToString("F3"));
            EditorGUILayout.LabelField("合成重心 (body ローカル)", mass.centerOfMass.ToString("F3") + " m");
            EditorGUILayout.LabelField("慣性テンソル", mass.inertiaTensor.ToString("F0") + " kg m2");
            EditorGUILayout.LabelField("運用入力", mass.MassPath + " [kg] / " + mass.MacCGPath + " [MAC率]");

            if (!Mathf.Approximately(inputFraction, mass.bakedMacFraction))
                EditorGUILayout.HelpBox(
                    "~/Physics/CG が焼き付け時と違う。テンソルの基準点を更新するには再焼き付けが必要。",
                    MessageType.Warning
                );
        }

        void OnSceneGUI()
        {
            FDMiBodyMass mass = (FDMiBodyMass)target;
            if (mass.body == null || mass.macChord <= 0f)
                return;

            Transform frame = FDMiAeroGeometry.Frame(mass.body, mass.transform);
            using (new Handles.DrawingScope(frame.localToWorldMatrix))
            {
                // 重心は胴体・翼の中に入る。深度テストを切らないとモデルに隠れて見えない。
                // zTest は DrawingScope の対象外なので、他のハンドルへ漏れないよう戻す。
                UnityEngine.Rendering.CompareFunction oldZTest = Handles.zTest;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

                Vector3 leadingEdge = mass.macLeadingEdge;
                Vector3 trailingEdge = leadingEdge + mass.macDirection * mass.macChord;
                Handles.color = new Color(0.35f, 0.75f, 1f, 1f);
                Handles.DrawLine(leadingEdge, trailingEdge);
                // ラベルは線の外側へ逃がす。MAC 線と重心が重なっても文字が読めるようにする。
                Handles.Label(leadingEdge - mass.macDirection * 4f + Vector3.up * 2f, "MAC LE");
                Handles.Label(trailingEdge + mass.macDirection * 4f + Vector3.up * 2f, "MAC TE");

                float size = Mathf.Max(mass.macChord * 0.05f, 0.1f);
                float inputFraction = CurrentMacFraction(mass);
                Vector3 inputCG = new Vector3(
                    mass.centerOfMass.x,
                    mass.centerOfMass.y,
                    mass.macZ0 + mass.macSlope * inputFraction
                );
                Color inputColor = new Color(1f, 0.85f, 0.2f, 1f);
                if (Mathf.Approximately(inputFraction, mass.bakedMacFraction))
                {
                    // 設計点と入力点が一致しているときは 1 つだけ描く (ラベルの重なりを避ける)。
                    DrawCross(inputCG, size, inputColor, "CG f=" + inputFraction.ToString("F3"));
                }
                else
                {
                    DrawCross(mass.centerOfMass, size, Color.white, "CG bake f=" + mass.bakedMacFraction.ToString("F3"));
                    DrawCross(inputCG, size, inputColor, "CG input f=" + inputFraction.ToString("F3"));
                }

                Handles.zTest = oldZTest;
            }
        }

        // 現在の ~/Physics/CG。シーンに保存されているプロキシの編集値を読む。
        static float CurrentMacFraction(FDMiBodyMass mass)
        {
            return mass.MacCG != null ? mass.MacCG.data[0] : 0f;
        }

        static void DrawCross(Vector3 point, float size, Color color, string label)
        {
            Handles.color = color;
            Handles.DrawLine(point - Vector3.right * size, point + Vector3.right * size);
            Handles.DrawLine(point - Vector3.up * size, point + Vector3.up * size);
            Handles.DrawLine(point - Vector3.forward * size, point + Vector3.forward * size);
            Handles.Label(point + Vector3.up * (size * 6f), label);
        }
    }
}
