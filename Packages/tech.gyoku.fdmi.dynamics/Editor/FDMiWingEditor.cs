using System.Collections.Generic;
using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 平面形 (前縁・後縁の点) から区間の値を焼き付け、区間ごとの設定を1画面で済ませる。
    //   - 前縁・後縁は Scene のハンドルと一覧の Vector3 の両方で置ける
    //   - コード長・面積・翼幅・coordPos・翼幅軸 (後退角) は焼き付けで決まる (FDMiWingPlanform)
    //   - 平面形は翼ローカル。翼の transform を動かす・回すと、その位置と取付角になる
    // 配線 (body / bodyAero / controlSurface) の自動解決は core の IFDMiAutoSetupUseCase がやる。
    // 生の配列・設計値・計測値は既定の Inspector に任せて折りたたむ (V1 の FDMi_wing_Editor と同じ構成)。
    [CustomEditor(typeof(FDMiWing))]
    public class FDMiWingEditor : FDMiBehaviourEditor
    {
        // Scene でハンドルを出す station。Editor のみ (保存しない)。
        int _station;
        // カーブ表の横スクロール。Editor のみ (保存しない。縦は Inspector に任せる)。
        Vector2 _curveScroll;
        // 既定の Inspector の開閉。Editor のみ。
        bool _foldRaw;

        public override void OnInspectorGUI()
        {
            FDMiWing wing = (FDMiWing)target;
            Bake(wing);

            DrawWiring(wing);
            DrawStations(wing);
            DrawCurves(wing);
            DrawTotal(wing);

            EditorGUILayout.Space();
            _foldRaw = EditorGUILayout.Foldout(_foldRaw, "配線・生の値 (既定の Inspector)");
            if (_foldRaw)
                base.OnInspectorGUI();
        }

        void OnSceneGUI()
        {
            FDMiWing wing = (FDMiWing)target;
            // transform を動かした時 (Scene のギズモ) も追従させる。
            Bake(wing);

            // 平面形は翼ローカルなので翼の transform で描く。ハンドルもこの座標で動かす。
            using (new Handles.DrawingScope(wing.transform.localToWorldMatrix))
            {
                DrawPlanform(wing);
            }
            // 焼き付いた区間は body ローカルなので機体の transform で描く。
            Transform frame = FDMiAeroGeometry.Frame(wing.body, wing.transform);
            using (new Handles.DrawingScope(frame.localToWorldMatrix))
            {
                FDMiAeroGeometry.DrawBodyFrame(wing.span * 0.2f);
                FDMiAeroGeometry.DrawCenterOfMass(wing.body != null ? wing.body.centerOfMass : Vector3.zero, wing.span * 0.02f);
                DrawSections(wing);
            }
        }

        // 焼き付け。値が変わったときだけ scene を汚す。transform の移動は Undo が持つので、
        // ここでは Undo を記録しない (次に描くときに同じ値へ焼き直る)。
        static void Bake(FDMiWing wing)
        {
            if (FDMiWingPlanform.Bake(wing))
                EditorUtility.SetDirty(wing);
        }

        void DrawWiring(FDMiWing wing)
        {
            EditorGUILayout.LabelField("平面形 (前縁・後縁)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "前縁・後縁を翼ローカル [m] で置く。コード長・面積・翼幅・coordPos・翼幅軸 (後退角) はここから焼き付く。"
                    + " station は左翼端から右翼端の順に並べる。翼の位置と取付角は transform で決める。",
                MessageType.None
            );
            int devices = wing.controlSurface != null ? wing.controlSurface.Length : 0;
            EditorGUILayout.LabelField(
                "配線",
                "body " + (wing.body != null ? wing.body.name : "なし")
                    + " / 胴体 " + (wing.bodyAero != null ? wing.bodyAero.name : "なし")
                    + " / 装置 " + devices
            );
        }

        // station の一覧。区間 i は station i と i+1 の間。行は station、行間に焼き付く区間の値を出す。
        void DrawStations(FDMiWing wing)
        {
            int stations = FDMiWingPlanform.Stations(wing);
            int sections = Mathf.Max(stations - 1, 0);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("区間 +", GUILayout.Width(64f)))
                    SetSectionCount(wing, sections + 1);
                if (GUILayout.Button("区間 −", GUILayout.Width(64f)))
                    SetSectionCount(wing, Mathf.Max(sections - 1, 0));
                EditorGUILayout.LabelField("区間 " + sections + " / station " + stations);
            }
            if (stations == 0)
                return;

            // 数値は直接書き換えず、変わったときだけまとめて反映する (Undo を1手にまとめる)。
            Vector3[] leadingEdge = (Vector3[])wing.leadingEdge.Clone();
            Vector3[] trailingEdge = (Vector3[])wing.trailingEdge.Clone();
            int removeAt = -1;
            _station = Mathf.Clamp(_station, 0, stations - 1);

            // station を選ぶ番号ボタンまで「変更」と見なさないよう、BeginChangeCheck ではなく
            // 前後の値を比べる。
            bool changed = false;
            for (int j = 0; j < stations; j++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 番号を押すと、その station のハンドルを Scene に出す。
                    if (GUILayout.Toggle(j == _station, (j + 1).ToString(), "Button", GUILayout.Width(24f)))
                        _station = j;
                    Vector3 leading = EditorGUILayout.Vector3Field("前", leadingEdge[j]);
                    if (leading != leadingEdge[j])
                        changed = true;
                    leadingEdge[j] = leading;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 番号の欄と同じ幅を空けて、前後を縦にそろえる。
                    EditorGUILayout.LabelField("", GUILayout.Width(24f));
                    Vector3 trailing = EditorGUILayout.Vector3Field("後", trailingEdge[j]);
                    if (trailing != trailingEdge[j])
                        changed = true;
                    trailingEdge[j] = trailing;
                    if (GUILayout.Button("×", GUILayout.Width(20f)))
                        removeAt = j;
                }
                if (j < sections)
                    DrawSolvedSection(leadingEdge, trailingEdge, j);
            }
            if (changed)
            {
                Undo.RecordObject(wing, "FDMiWing 平面形");
                wing.leadingEdge = leadingEdge;
                wing.trailingEdge = trailingEdge;
                Bake(wing);
            }
            if (removeAt >= 0)
                RemoveStation(wing, removeAt);
        }

        // 焼き付く区間の値。数値の確認用 (Scene のラベルと同じ値)。
        static void DrawSolvedSection(Vector3[] leadingEdge, Vector3[] trailingEdge, int i)
        {
            FDMiWingPlanform.Section section;
            string text = "  Sec" + i;
            if (FDMiWingPlanform.Solve(leadingEdge, trailingEdge, i, out section))
                text += "   コード " + section.chord.ToString("F2") + " m   幅 " + section.width.ToString("F2")
                    + " m   面積 " + section.area.ToString("F1") + " m2   後退角 " + section.sweepDeg.ToString("F1") + " deg";
            else
                text += "   前縁と後縁が同じ (置くと区間になる)";
            EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
        }

        // カーブは区間ごとに持つが、同じ翼型を使うことが多い。表で1画面に並べ、横にスクロールする。
        // 行=Sec、列=5種 (clAlpha / cdAlpha / clMach / cdMach / cmAlpha)。縦には伸ばす
        // (Inspector 自体が縦スクロールするため、二重スクロールにしない)。
        void DrawCurves(FDMiWing wing)
        {
            int sections = wing.clAlpha != null ? wing.clAlpha.Length : 0;
            EditorGUILayout.LabelField("カーブ (区間ごと)", EditorStyles.boldLabel);
            if (sections == 0)
            {
                EditorGUILayout.LabelField("区間なし (区間 + で増やす)");
                return;
            }
            _curveScroll = EditorGUILayout.BeginScrollView(_curveScroll);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", GUILayout.Width(80f));
                EditorGUILayout.LabelField("clAlpha", GUILayout.Width(140f));
                EditorGUILayout.LabelField("cdAlpha", GUILayout.Width(140f));
                EditorGUILayout.LabelField("clMach", GUILayout.Width(140f));
                EditorGUILayout.LabelField("cdMach", GUILayout.Width(140f));
                EditorGUILayout.LabelField("cmAlpha", GUILayout.Width(140f));
            }
            for (int i = 0; i < sections; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Sec" + i, GUILayout.Width(40f));
                    if (i == 0)
                        EditorGUILayout.LabelField("", GUILayout.Width(40f));
                    else if (GUILayout.Button("←0", GUILayout.Width(40f)))
                        CopySection(wing, i);
                    wing.clAlpha[i] = EditorGUILayout.CurveField(wing.clAlpha[i], GUILayout.Width(140f), GUILayout.Height(60f));
                    wing.cdAlpha[i] = EditorGUILayout.CurveField(wing.cdAlpha[i], GUILayout.Width(140f), GUILayout.Height(60f));
                    wing.clMach[i] = EditorGUILayout.CurveField(wing.clMach[i], GUILayout.Width(140f), GUILayout.Height(60f));
                    wing.cdMach[i] = EditorGUILayout.CurveField(wing.cdMach[i], GUILayout.Width(140f), GUILayout.Height(60f));
                    wing.cmAlpha[i] = EditorGUILayout.CurveField(wing.cmAlpha[i], GUILayout.Width(140f), GUILayout.Height(60f));
                }
            }
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(wing);
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Sec0 のカーブを全 section に複製"))
                CopyCurves(wing);
        }

        void DrawTotal(FDMiWing wing)
        {
            // 焼き付いた区間から合計と後退角を出す。角度は回転で変わらないので平面形から直接解く。
            int sections = wing.sectionArea != null ? wing.sectionArea.Length : 0;
            float area = 0f;
            float sweep = 0f;
            float sweepFactor = 0f;
            for (int i = 0; i < sections; i++)
            {
                area += wing.sectionArea[i];
                FDMiWingPlanform.Section section;
                if (!FDMiWingPlanform.Solve(wing.leadingEdge, wing.trailingEdge, i, out section))
                    continue;
                sweep += section.sweepDeg * section.area;
                sweepFactor += Mathf.Cos(section.sweepDeg * Mathf.Deg2Rad) * section.area;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("合計", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("翼幅 span", wing.span.ToString("F2") + " m (投影翼幅の焼き付け値)");
            EditorGUILayout.LabelField("面積", (area * wing.wingAreaCoef).ToString("F1") + " m2");
            EditorGUILayout.LabelField("アスペクト比", area > 0f ? (wing.span * wing.span / (area * wing.wingAreaCoef)).ToString("F2") : "-");
            // 後退角は翼幅軸の向きとして効く。代表値は面積加重平均 (屈折翼でも1つにまとめる)。
            EditorGUILayout.LabelField(
                "後退角",
                (area > 0f ? (sweep / area).ToString("F1") : "-") + " deg (面積加重平均) / 有効揚力傾斜 cosΛ "
                    + (area > 0f ? (sweepFactor / area).ToString("F3") : "-")
            );
        }

        // 焼き付いた区間。真の台形で描く (黄色の平面形輪郭と重なる。重ならないときは焼き付けが古い)。
        // ラベルは台形重心、軸と作用点の印は coordPos (力の作用点) に残す。未解決区間 (面積0) は描かない。
        void DrawSections(FDMiWing wing)
        {
            if (wing.coordPos == null)
                return;
            for (int i = 0; i < wing.coordPos.Length; i++)
            {
                if (wing.sectionArea == null || i >= wing.sectionArea.Length || wing.sectionArea[i] <= 0f)
                    continue;
                if (wing.sectionChord == null || i >= wing.sectionChord.Length || wing.sectionChord[i] <= 0f)
                    continue;
                FDMiAeroGeometry.DrawSection(wing, i, new Color(0.3f, 0.8f, 1f, 1f));
                FDMiAeroGeometry.DrawSectionAxes(wing, i, wing.span * 0.1f);
                Vector3 labelPos = wing.coordPos[i];
                if (FDMiAeroGeometry.TrueCorners(wing, i, out Vector3 le0, out Vector3 le1, out Vector3 te1, out Vector3 te0))
                    labelPos = FDMiAeroGeometry.Centroid(le0, le1, te1, te0);
                // 単位は書かない。区間が並ぶと文字が重なるため (コード長 [m] と面積 [m2])。
                Handles.Label(labelPos, "Sec" + i + " c=" + wing.sectionChord[i].ToString("F2") + " S=" + wing.sectionArea[i].ToString("F1"));
            }
            // 装置の舵面も一緒に出す。翼を選べば全面の絵が1枚で見える。
            if (wing.controlSurface != null)
                for (int i = 0; i < wing.controlSurface.Length; i++)
                    if (wing.controlSurface[i] != null)
                        FDMiAeroGeometry.DrawDevice(wing, wing.controlSurface[i], new Color(1f, 0.6f, 0.2f, 1f));
        }

        // 平面形の点。前縁・後縁を点で示し、番号を押した station だけドラッグできる。
        // 座標は翼ローカル (DrawingScope の行列の中で完結する)。
        void DrawPlanform(FDMiWing wing)
        {
            int stations = FDMiWingPlanform.Stations(wing);
            if (stations == 0)
                return;
            float dot = Mathf.Max(wing.span * 0.012f, 0.05f);
            for (int j = 0; j < stations; j++)
            {
                Handles.color = j == _station ? new Color(1f, 0.9f, 0.2f, 1f) : new Color(0.85f, 0.85f, 0.85f, 0.7f);
                Handles.SphereHandleCap(0, wing.leadingEdge[j], Quaternion.identity, dot, EventType.Repaint);
                Handles.SphereHandleCap(0, wing.trailingEdge[j], Quaternion.identity, dot, EventType.Repaint);
                Handles.DrawLine(wing.leadingEdge[j], wing.trailingEdge[j]);
            }
            // 前縁・後縁を結んだ平面形の輪郭。
            Handles.color = new Color(1f, 1f, 0.4f, 1f);
            for (int j = 1; j < stations; j++)
            {
                Handles.DrawLine(wing.leadingEdge[j - 1], wing.leadingEdge[j]);
                Handles.DrawLine(wing.trailingEdge[j - 1], wing.trailingEdge[j]);
            }
            if (_station < 0 || _station >= stations)
                return;

            Handles.Label(wing.leadingEdge[_station], "前縁 " + (_station + 1));
            Handles.Label(wing.trailingEdge[_station], "後縁 " + (_station + 1));
            EditorGUI.BeginChangeCheck();
            Vector3 leadingEdge = Handles.PositionHandle(wing.leadingEdge[_station], Quaternion.identity);
            Vector3 trailingEdge = Handles.PositionHandle(wing.trailingEdge[_station], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(wing, "FDMiWing 平面形");
                wing.leadingEdge[_station] = leadingEdge;
                wing.trailingEdge[_station] = trailingEdge;
                Bake(wing);
                Repaint();
            }
        }

        // 区間数の変更。翼と装置の区間配列をそろえて焼き直す。
        static void SetSectionCount(FDMiWing wing, int sections)
        {
            UnityEngine.Object[] objects = FDMiWingPlanform.Targets(wing);
            Undo.RecordObjects(objects, "FDMiWing 区間数");
            FDMiWingPlanform.SetSectionCount(wing, sections);
            for (int i = 0; i < objects.Length; i++)
                EditorUtility.SetDirty(objects[i]);
        }

        // station を1点消して前詰めする。区間数が1つ減る。
        static void RemoveStation(FDMiWing wing, int index)
        {
            UnityEngine.Object[] objects = FDMiWingPlanform.Targets(wing);
            Undo.RecordObjects(objects, "FDMiWing station 削除");
            List<Vector3> leadingEdge = new List<Vector3>(wing.leadingEdge);
            List<Vector3> trailingEdge = new List<Vector3>(wing.trailingEdge);
            leadingEdge.RemoveAt(index);
            trailingEdge.RemoveAt(index);
            wing.leadingEdge = leadingEdge.ToArray();
            wing.trailingEdge = trailingEdge.ToArray();
            FDMiWingPlanform.Bake(wing);
            for (int i = 0; i < objects.Length; i++)
                EditorUtility.SetDirty(objects[i]);
        }

        static void CopyCurves(FDMiWing wing)
        {
            Undo.RecordObject(wing, "FDMiWing カーブ複製");
            Copy(wing.clAlpha);
            Copy(wing.cdAlpha);
            Copy(wing.clMach);
            Copy(wing.cdMach);
            Copy(wing.cmAlpha);
            EditorUtility.SetDirty(wing);
        }

        // Sec0 の5種を Sec i 1つに配る (表の行頭ボタン)。
        static void CopySection(FDMiWing wing, int i)
        {
            Undo.RecordObject(wing, "FDMiWing カーブ複製");
            CopyOne(wing.clAlpha, i);
            CopyOne(wing.cdAlpha, i);
            CopyOne(wing.clMach, i);
            CopyOne(wing.cdMach, i);
            CopyOne(wing.cmAlpha, i);
            EditorUtility.SetDirty(wing);
        }

        static void CopyOne(AnimationCurve[] curves, int i)
        {
            if (curves == null || curves.Length == 0 || curves[0] == null)
                return;
            if (i <= 0 || i >= curves.Length)
                return;
            curves[i] = new AnimationCurve(curves[0].keys);
            curves[i].preWrapMode = curves[0].preWrapMode;
            curves[i].postWrapMode = curves[0].postWrapMode;
        }

        static void Copy(AnimationCurve[] curves)
        {
            if (curves == null || curves.Length == 0 || curves[0] == null)
                return;
            for (int i = 1; i < curves.Length; i++)
            {
                curves[i] = new AnimationCurve(curves[0].keys);
                curves[i].preWrapMode = curves[0].preWrapMode;
                curves[i].postWrapMode = curves[0].postWrapMode;
            }
        }
    }
}
