using System;
using FDMi.core.Editor.Inspector;
using UnityEditor;
using UnityEngine;

namespace FDMi.dynamics.Editor
{
    // 翼の区間定義を Scene にワイヤーフレームで出す。区間数はここで操作する。
    // 区間配列・カーブ配列と、装置の区間配列をまとめて同じ長さにするので、長さ違いのまま実行して
    // 添字が外れることがない (FDMiWing の固定更新は coordPos の長さで全配列を引く)。
    // 配線 (body / bodyAero / controlSurface) の自動解決は core の IFDMiAutoSetupUseCase がやる。
    [CustomEditor(typeof(FDMiWing))]
    public class FDMiWingEditor : FDMiBehaviourEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FDMiWing wing = (FDMiWing)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("区間 (Editor)", EditorStyles.boldLabel);

            int current = wing.coordPos != null ? wing.coordPos.Length : 0;
            // 区間数はここで決める。配列の長さが区間の定義そのもので、データから意図した数を導けない。
            // DelayedIntField なので入力中 (Enter まで) は配列に触らない。
            EditorGUI.BeginChangeCheck();
            int count = EditorGUILayout.DelayedIntField("区間数", current);
            if (EditorGUI.EndChangeCheck() && count >= 0 && count != current)
                SetSectionCount(wing, count);

            float widthSum = 0f;
            float areaSum = 0f;
            for (int i = 0; i < current; i++)
            {
                widthSum += FDMiAeroGeometry.SectionWidth(wing, i);
                areaSum += wing.sectionArea[i];
            }
            EditorGUILayout.LabelField("区間幅の合計", widthSum.ToString("F2") + " m (span " + wing.span.ToString("F2") + " m)");
            EditorGUILayout.LabelField("面積の合計", (areaSum * wing.wingAreaCoef).ToString("F1") + " m2");
            if (GUILayout.Button("span に区間幅の合計 (" + widthSum.ToString("F2") + " m) を焼き付ける"))
            {
                Undo.RecordObject(wing, "FDMiWing span");
                wing.span = widthSum;
                EditorUtility.SetDirty(wing);
            }
        }

        void OnSceneGUI()
        {
            FDMiWing wing = (FDMiWing)target;
            Transform frame = FDMiAeroGeometry.Frame(wing.body, wing.transform);
            using (new Handles.DrawingScope(frame.localToWorldMatrix))
            {
                FDMiAeroGeometry.DrawBodyFrame(wing.span * 0.2f);
                FDMiAeroGeometry.DrawCenterOfMass(wing.body != null ? wing.body.centerOfMass : Vector3.zero, wing.span * 0.02f);
                if (wing.coordPos == null)
                    return;
                for (int i = 0; i < wing.coordPos.Length; i++)
                {
                    FDMiAeroGeometry.DrawSection(wing, i, new Color(0.3f, 0.8f, 1f, 1f));
                    FDMiAeroGeometry.DrawSectionAxes(wing, i, wing.span * 0.1f);
                    // 単位は書かない。区間が並ぶと文字が重なるため (値はコード長 [m] と面積 [m2])。
                    Handles.Label(wing.coordPos[i], "Sec" + i + " c=" + wing.sectionChord[i].ToString("F2") + " S=" + wing.sectionArea[i].ToString("F1"));
                }
                // 装置の舵面も一緒に出す。翼を選べば全面の絵が1枚で見える。
                if (wing.controlSurface != null)
                    for (int i = 0; i < wing.controlSurface.Length; i++)
                        if (wing.controlSurface[i] != null)
                            FDMiAeroGeometry.DrawDevice(wing, wing.controlSurface[i], new Color(1f, 0.6f, 0.2f, 1f));
            }
        }

        // 区間数を翼と装置に同時に反映する。値は残し、伸びた分だけ既定値 (カーブは空 = 揚力0) にする。
        // 装置を翼の子から探すのは自動解決と同じ規則 (FDMiControlSurface は自分の翼を持たない)。
        static void SetSectionCount(FDMiWing wing, int count)
        {
            FDMiControlSurface[] devices = wing.GetComponentsInChildren<FDMiControlSurface>(true);
            UnityEngine.Object[] objects = new UnityEngine.Object[devices.Length + 1];
            objects[0] = wing;
            for (int i = 0; i < devices.Length; i++)
                objects[i + 1] = devices[i];
            Undo.RecordObjects(objects, "FDMiWing 区間数");

            Resize(ref wing.coordPos, count);
            Resize(ref wing.spanNormal, count);
            Resize(ref wing.chordNormal, count);
            Resize(ref wing.sectionChord, count);
            Resize(ref wing.sectionArea, count);
            Resize(ref wing.clAlpha, count);
            Resize(ref wing.cdAlpha, count);
            Resize(ref wing.clMach, count);
            Resize(ref wing.cdMach, count);
            Resize(ref wing.cmAlpha, count);
            Resize(ref wing.alpha, count);
            FillCurves(wing.clAlpha);
            FillCurves(wing.cdAlpha);
            FillCurves(wing.clMach);
            FillCurves(wing.cdMach);
            FillCurves(wing.cmAlpha);

            for (int i = 0; i < devices.Length; i++)
            {
                Resize(ref devices[i].sectionMul, count);
                Resize(ref devices[i].alpha, count);
                Resize(ref devices[i].cl, count);
                Resize(ref devices[i].cd, count);
                Resize(ref devices[i].scale, count);
            }

            for (int i = 0; i < objects.Length; i++)
                EditorUtility.SetDirty(objects[i]);
        }

        static void Resize<T>(ref T[] array, int count)
        {
            Array.Resize(ref array, count);
        }

        // 空のカーブは 0 を返すが、null は評価できない。
        static void FillCurves(AnimationCurve[] curves)
        {
            for (int i = 0; i < curves.Length; i++)
                if (curves[i] == null)
                    curves[i] = new AnimationCurve();
        }
    }
}
